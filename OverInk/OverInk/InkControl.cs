using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OverInk.AppData;
using InkLibrary;
using System.IO;
using XDMTECH.WaferMap;
using static OverInk.InkModel;
using OverInk.AppCode;
using System.Threading;

namespace OverInk
{
    public class InkControl
    {
        private DataTable map = new DataTable();
        public InkInfo ink = new InkInfo();

        private List<DieList> totalDies = new List<DieList>();
        private List<Point> badDies = new List<Point>();
        private List<Point> InkDieList = new List<Point>();
        private List<Point> inkPositionDies = new List<Point>();
        Dictionary<string, List<Point>> InkZoneIdList = new Dictionary<string, List<Point>>();

        BinMapControl bmc = new BinMapControl();
        BinMapSingleViewPane singleViewPane = new BinMapSingleViewPane();
        List<InkMapHeaderInfo> HeaderInfoList = new List<InkMapHeaderInfo>();
        WaferDrawingContext drawingContext = new WaferDrawingContext();
        ZoneCollection zoneCollection = new ZoneCollection();
        public InkControl()
        {

        }
        public InkControl(InkInfo ink)
        {
            this.ink = ink;
        }
        public InkControl(InkInfo ink,string isdebug)
        {
            this.ink = ink;
            this.ink.isDebug = isdebug;
        }
        //public InkControl(string product,string isdebug,string version)
        //{
        //    this.ink = getInkSetting(product);
        //    this.ink.OverInkVersion = version;
        //    this.ink.isDebug = isdebug;

        //}
        public string runInk()
        {
            try
            {
                SysItem.writeLog("Start ink map control");
                SysItem.writeLog("Get cp map data...");
                if (ink.OVERINKFLAG=="N")
                {
                    SysItem.writeLog("the value of OVERINKFLAG is N, no need trigger over ink");
                    return "Success";
                }

                getMapData();
    
                if (map.Rows.Count > 0)
                {
                    SysItem.writeLog("cp map count：" + map.Rows.Count);
                    //获取map die
                    initDieInfo();
                    //初始化bmc
                    initbmc();
                    //获取position die
                    initPositoinDies();
                    //获取ink die
                    initInkDieList();
                    //Judge Yield
                    bool judgeyield = JudgeYield();
                    SysItem.writeLog("Trigger Die Count:" + InkDieList.Count.ToString());
     

                    if (InkDieList.Count>0 || judgeyield)
                    {
                        SysItem.writeLog("need send notice mail");
                        if (ink.SEND_ALARM_MAIL=="1")
                        {
                            ink.MAIL_MSG = "the wafer: "+ ink.lotid +"#"+ink.wafer +" meet ink trigger condition:";
                            if (InkDieList.Count>0)
                            {
                                ink.MAIL_MSG += " Cluster ";
                            }
                            if (judgeyield)
                            {
                                ink.MAIL_MSG += " Yield";
                            }
                            sendnoticemail();

                        }
                        if (!judgeyield)
                        {
                            SysItem.writeLog("no meet ink trigger condition");
                            InkDieList = new List<Point>();
                        }
                    }
                    if (ink.OverInkVersion == "Auto" && (InkDieList.Count == 0 || !judgeyield))
                    {
                        //SysItem.writeLog("no meet ink trigger condition");
                        return "Success";
                    }


                    //初始化ink rule
                    initInkRule();

                    int oriPassCount = 0;
                    int afterPassCount = 0;
                    ExportInkSetting setting = new ExportInkSetting
                    {
                        ExportOrientation = "Right", //bmc.BinMaplayer.Shapes[0].notch,
                        ExportOrientationColumnName = "Notch:",
                        DefaultFileName = "ExportInk"
                    };
                    exportheadsetting();
                    string inkExportText = bmc.GetInkTextMap(setting, HeaderInfoList, out afterPassCount, out oriPassCount);
                    inkExportText = inkExportText.Replace("Pass: 0", "Pass: " + afterPassCount.ToString());
                    SysItem.writeLog("oriPassCount:" + oriPassCount.ToString() + ", afterPassCount: " + afterPassCount.ToString());
                    if (!Directory.Exists(ink.exportpath))
                    {
                        Directory.CreateDirectory(ink.exportpath);
                    }
                    // delete same lot+wafer map
                    DirectoryInfo di = new DirectoryInfo(ink.exportpath);
                    FileInfo[] fis = di.GetFiles(ink.parentlotid +"*_"+ ink.wafer+"_CP_*.txt");
                    foreach (FileInfo fi in fis)
                    {
                        SysItem.writeLog("delete old file: " + fi.Name);
                        fi.Delete();
                    }


                    string inkFilePath = string.Format("{3}\\{0}_{1}_CP_{2}.txt", ink.lotid, ink.wafer, afterPassCount.ToString(), ink.exportpath);
                    SysItem.writeLog("export to overinkpath " + inkFilePath);
                    System.Text.Encoding encodingUTF8 = new System.Text.UTF8Encoding(false);
                    using (StreamWriter sw = new StreamWriter(inkFilePath, false, encodingUTF8)) //M0.39
                    {
                        sw.Write(inkExportText);
                    }
                    SysItem.writeLog("End ink map control");
                    return "Success"+ inkExportText;

                }
                else
                {
                    return "can not find cp data, please check lotid & wafer is correct.";
                }



            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return ex.Message;
            }
        }

        private void getMapData()
        {
            switch (ink.OverInkVersion)
            {
                case "Manual":
                    map = SqlData.getcpmapdata(ink.isDebug, ink.lotid + "#" + ink.wafer, ink.egdeCount);
                    break;
                case "Auto":
                    map = SqlData.getcpmapdata(ink);
                    break;
                default:
                    break;
            }
        }

        public void Clear()
        {
            totalDies.Clear();
            badDies.Clear();
            InkDieList.Clear();
            inkPositionDies.Clear();
            InkZoneIdList.Clear();
            bmc = new BinMapControl();
            singleViewPane = new BinMapSingleViewPane();
            HeaderInfoList = new List<InkMapHeaderInfo>();
            drawingContext = new WaferDrawingContext();
            zoneCollection = new ZoneCollection();
            ink.stepname.Clear();
            ink.jobintime.Clear();
            ink.jobouttime.Clear();
            ink.ppid.Clear();
        }

        private void sendnoticemail()
        {
            if (string.IsNullOrEmpty(ink.MAIL_SENDTO))
            {
                ink.MAIL_SENDTO = "luoxiangrong@silanic.cn";
            }
           // SysItem.sendmail(ink);
          Thread t = new Thread(new ParameterizedThreadStart(SysItem.sendmail));
          t.Start(ink);

        }

        private void initbmc()
        {
            singleViewPane.SetInkMapArrayMap(bmc, map);
            bmc.GoodBinLookUp = new Dictionary<string, bool>();
            bmc.GoodBinLookUp["1"] = true;
        }

        private void initInkRule()
        {
            List<Point> inkzonedies = getZoneDie(InkDieList);
            switch (ink.inkrule)
            {
                case InkModel.InkRule.InkAllDie:

                    if (ink.inkpositioin ==InkPosition.Shot && ink.shotforcedink=="Y")
                    {
                        foreach (Point p in inkPositionDies)
                        {
                            if (inkzonedies.Contains(p))
                            {
                                bmc.CPTempInkMapMark[p] = 0;
                            }
                            else
                            {
                                bmc.CPTempInkMapMark[p] = 1;
                            }
                        }
                    }else
                    {
                        if (inkzonedies.Count > 0)
                        {
                            foreach (Point p in inkzonedies)
                            {
                                if (!bmc.CPTempInkMapMark.ContainsKey(p))
                                {
                                    foreach (string i in InkZoneIdList.Keys)
                                    {
                                        if (InkZoneIdList[i].Contains(p))
                                        {
                                            MergeDictionary(InkZoneIdList[i].ToDictionary(x => new Point(x.X, x.Y), x => 1));
                                            break;
                                        }
                                    }
                                }
                                bmc.CPTempInkMapMark[p] = 0;

                            }
                        }
                    }
                    break;
                case InkModel.InkRule.InkType:
                    switch (ink.inkType)
                    {
                        case "Rectangle":
                            bmc.InkMapType = InkType.Rectangle;
                            bmc.InkRangeHoriAxis = ink.range;
                            break;
                        case "Cross":
                            bmc.InkMapType = InkType.Cross;
                            bmc.InkRangeHoriAxis = ink.range;
                            break;
                        case "Ellipse":
                            bmc.InkMapType = InkType.Ellipse;
                            bmc.InkRangeVertAxis = ink.range;
                            bmc.InkRangeHoriAxis = ink.range_y;
                            break;
                        case "Level":
                            bmc.InkMapType = InkType.Level;
                            bmc.InkRangeHoriAxis = ink.range;
                            break;
                        case "Vertical":
                            bmc.InkMapType = InkType.Vertical;
                            bmc.InkRangeHoriAxis = ink.range;
                            break;
                    }
                    bmc.CPInkWithAlgorithm = true;
                    bmc.startInk(inkzonedies);
                    if (ink.renewflag == "N")
                    {
                        JudgeRenew();
                    }
                    List<Point> p1 = bmc.CPTempInkMapMark.Keys.ToList();
                    foreach (Point p in p1)
                    {
                        if (inkPositionDies.FindIndex(a => (a.X == p.X && a.Y == p.Y)) < 0)
                        {
                            bmc.CPTempInkMapMark.Remove(p);
                        }
                    }
                    break;
                default:
                    break;
            }



        }

        private void MergeDictionary(Dictionary<Point, int> d2)
        {
            foreach (var l in d2.Keys)
            {
                bmc.CPTempInkMapMark[l] = d2[l];
            }
        }

        private void initDieInfo()
        {
            bmc.BinMaplayer = new InkLibrary.DieGridDecorator();
            for (int i = 0; i < map.Rows.Count; i++)
            {
                DieList dl = new DieList();
                DataRow dr = map.Rows[i];
                dl.die = new Point(int.Parse(dr["DIE_X"].ToString()), int.Parse(dr["DIE_Y"].ToString()));
                dl.wafer = new Point(int.Parse(dr["wafer_x"].ToString()), int.Parse(dr["wafer_y"].ToString()));
                dl.bin_code = dr["BIN_CODE"].ToString();
                dl.location = dr["position"].ToString();
                dl.shotid = dr["shot_id"].ToString();
                InkLibrary.DieShapeObject dieObj = new InkLibrary.DieShapeObject();
                dieObj.xIndex = dl.die.X;
                dieObj.yIndex = dl.die.Y;
                dieObj.CenterX = dl.wafer.X;
                dieObj.CenterY = dl.wafer.Y;
                dieObj.Bounds.Width = (float)Convert.ToDouble(dr["DIE_PITCH_X"].ToString());
                dieObj.Bounds.Height = (float)Convert.ToDouble(dr["DIE_PITCH_Y"].ToString());

                dieObj.Text = dl.bin_code;
                //     dieObj.notch = dr["notch"].ToString();
                dieObj.notch = "Right";
                bmc.BinMaplayer.Shapes.Add(dieObj);
                if (dl.bin_code == "X")
                {
                    badDies.Add(dl.die);
                }
                totalDies.Add(dl);
            }

        }

        private void initInkDieList()
        {
         //   List<Point> JudgeDieList = new List<Point>();
            foreach (Point pf in badDies)
            {
                List<Point> CheckListDie = new List<Point>();

                if (!InkDieList.Contains(pf))
                {
                    CheckListDie = getnearbaddie(pf, ink.clusterDistance, CheckListDie);
                    if (CheckListDie.Count >= ink.clusterMinCount)
                    {
                        InkDieList.AddRange(CheckListDie);
                    }
                }

            }
        }

        private bool JudgeYield()
        {
            List<DieList> egdetotaldie = totalDies.FindAll(t => t.location == "Edge");
            List<DieList> egdefaildie = egdetotaldie.FindAll(t => t.bin_code == "X");
            List<DieList> centertotaldie = totalDies.FindAll(t => t.location == "Center");
            List<DieList> centerfaildie = centertotaldie.FindAll(t => t.bin_code == "X");

            double y1 = egdetotaldie.Count == 0 ? 1 : (Double)(egdetotaldie.Count - egdefaildie.Count) / (Double)egdetotaldie.Count;
            double y2 = centertotaldie.Count == 0 ? 1 : (Double)(centertotaldie.Count - centerfaildie.Count) / (Double)centertotaldie.Count;
            SysItem.writeLog(string.Format("edgeYield setting:{0}, actual:{1}", ink.edgeYield, y1));
            SysItem.writeLog(string.Format("centerYield setting:{0}, actual:{1}", ink.centerYield, y2));

            if (y1 < ink.edgeYield || y2 < ink.centerYield)
            {
                return true;
            }
            else
            {
                return false;
            }


        }

        private List<Point> getnearbaddie(Point pf, int distinct, List<Point> ListDie)
        {

            for (int i = -1 - distinct; i <= 1 + distinct; i++)
            {
                for (int j = -1 - distinct; j <= 1 + distinct; j++)
                {
                    int x = pf.X + i;
                    int y = pf.Y + j;
                    Point newpf = new Point(x, y);
                    if (ListDie.IndexOf(newpf) >= 0)
                    {
                        continue;
                    }
                    if (badDies.IndexOf(newpf) >= 0)
                    {
                        ListDie.Add(newpf);

                        ListDie = getnearbaddie(newpf, distinct, ListDie);
                    }

                }
            }

            return ListDie;
        }

        private void initPositoinDies()
        {
            switch (ink.inkpositioin)
            {
                case InkPosition.FullMap:
                    inkPositionDies = totalDies.Select(t => t.die).ToList();
                    InkZoneIdList["0"] = inkPositionDies;
                    break;
                case InkPosition.Sector:
                    getzonemap();
                    break;
                case InkPosition.Shot:
                    getshotmap();
                    break;
                default:
                    break;
            }
        }

        private void getshotmap()
        {
            string[] shots = ink.shotid.Split(',');
            foreach (var shotid in shots)
            {
                inkPositionDies.AddRange(totalDies.Where(t => t.shotid == shotid).Select(a => a.die).ToList());
                if (!InkZoneIdList.Keys.Contains(shotid))
                {
                    InkZoneIdList[shotid] = new List<Point>();
                }
                InkZoneIdList[shotid].AddRange(inkPositionDies);
            }
        }

        private List<Point> getZoneDie(List<Point> ps)
        {

            List<Point> newinkmap = inkPositionDies.Intersect(ps).ToList();
            return newinkmap;
        }

        private void JudgeRenew()
        {
            List<Point> ps = new List<Point>();
            foreach (Point p in bmc.CPTempInkMapMark.Keys)
            {
                int a = bmc.BinMaplayer.Shapes.FindIndex(s => s.xIndex == p.X && s.yIndex == p.Y);
                if (a >= 0)
                {
                    DieList d = totalDies[a];
                    if (d.bin_code == "1")
                    {
                        ps.Add(d.die);
                    }
                }

            }
            for (int i = 0; i < ps.Count; i++)
            {
                bmc.CPTempInkMapMark.Remove(ps[i]);
            }
        }

        private void setzonedefine()
        {
            //MODE: Rule
            //ZONE_ID_LIST:1; 2; 3; 4; 5; 6; 7; 8
            //ZONE_TEXT_LIST: 1; 2; 3; 4; 5; 6; 7; 8
            //R0: R50; A0: A90
            //R50:R100; A0: A90
            //R0:R50; A90: A180
            //R50:R100; A90: A180
            //R0:R50; A180: A270
            //R50:R100; A180: A270
            //R0:R50; A270: A360
            //R50:R100; A270: A360
            string zondf = "";
            int id = 1;
            string id_list = "";
            double radius = 360 / ink.sector.radius;
            if (ink.sector.type == "Same Radius")
            {

                double circle = 100 / ink.sector.circle;
                for (double i = 0; i < ink.sector.radius; i++)
                {
                    for (double j = 0; j < ink.sector.circle; j++)
                    {
                        id_list += id.ToString() + ";";
                        id++;
                        zondf += string.Format("R{0}:R{1};A{2}:A{3}\r\n", (j * circle).ToString(),
                                                        ((j + 1) * circle > 100 ? 100 : (j + 1) * circle).ToString(),
                                                        (ink.sector.angle + (i) * radius).ToString(),
                                                        (ink.sector.angle + (i + 1) * radius).ToString()
                                                );
                    }
                }
            }
            else if (ink.sector.type == "Same Area")
            {

                for (double i = 0; i < ink.sector.radius; i++)
                {
                    for (double j = 0; j < ink.sector.circle; j++)
                    {
                        id_list += id.ToString() + ";";
                        id++;
                        zondf += string.Format("R{0}:R{1};A{2}:A{3}\r\n", (Math.Sqrt(j / ink.sector.circle) * 100).ToString(),
                                                        ((Math.Sqrt((j + 1) / ink.sector.circle) > 1 ? 1 : Math.Sqrt((j + 1) / ink.sector.circle)) * 100).ToString(),
                                                        (ink.sector.angle + (i) * radius).ToString(),
                                                        (ink.sector.angle + (i + 1) * radius).ToString()
                                                );
                    }
                }

            }

            zondf = string.Format("MODE: Rule\r\nZONE_ID_LIST:{0}\r\nZONE_TEXT_LIST:{0}\r\n{1}", id_list, zondf);

            // Set Wafer Object with wafer size
            // WaferDrawingContext drawingContext = new WaferDrawingContext();
            drawingContext.WaferSizeUM = 300 * 1000;

            // Load Zone Def to Zone Collection
            zoneCollection = ZoneCollection.LoadFromString(zondf);
            //Console.Write(zondf);
            //  }

        }

        private void exportheadsetting()
        {
            InkMapHeaderInfo headerInfo = new InkMapHeaderInfo
            {
                Header = "LotId",
                Value = ink.lotid
            };
            HeaderInfoList.Add(headerInfo);
            headerInfo = new InkMapHeaderInfo
            {
                Header = "DeviceName",
                Value = ""
            };
            HeaderInfoList.Add(headerInfo);

            headerInfo = new InkMapHeaderInfo
            {
                Header = "PieceID",
                Value = ink.wafer
            };
            HeaderInfoList.Add(headerInfo);
            headerInfo = new InkMapHeaderInfo
            {
                Header = "Pass",
                Value = "0"
            };
            HeaderInfoList.Add(headerInfo);
            headerInfo = new InkMapHeaderInfo
            {
                Header = "notch",
                Value = bmc.BinMaplayer.Shapes[0].notch
            };
            HeaderInfoList.Add(headerInfo);

        }

        private string getzoneindex(double v1, double v2)
        {
            //// Load Zone Def File
            //// string zoneDefContent = File.ReadAllText(@"..\..\ZoneSampleFile\4 pies.zone");
            //string zoneDefContent = File.ReadAllText(@"..\..\ZoneSampleFile\8 pies.zone");
            //// Set Wafer Object with wafer size
            //WaferDrawingContext drawingContext = new WaferDrawingContext();
            //drawingContext.WaferSizeUM = 300 * 1000;

            //// Load Zone Def to Zone Collection
            //ZoneCollection zoneCollection = ZoneCollection.LoadFromString(zoneDefContent);
            string RealZoneID = "";

            //Check point in which zone
            double waferXUM = v1;
            double waferYUM = v2;
            int zoneIndex = zoneCollection.CartesianPointInZone(waferXUM, waferYUM, drawingContext); // Get Zone Object Index in the List
            if (zoneIndex >= 0)
            {
                ZoneObject zoneObj = zoneCollection[zoneIndex];

                RealZoneID = zoneObj.ID;  // Get Real Zone ID
            }
            else
            {
                RealZoneID = "e";
            }
            return RealZoneID;
        }

        private void getzonemap()
        {
            setzonedefine();
            int max_x = int.Parse(map.Compute("max(die_x)", "").ToString());
            int min_x = int.Parse(map.Compute("min(die_x)", "").ToString());
            int max_y = int.Parse(map.Compute("max(die_y)", "").ToString());
            int min_y = int.Parse(map.Compute("min(die_y)", "").ToString());

            List<string> zoneid = ink.sector.location.Replace(" ", "").Split(',').ToList();

            for (double i = max_x; i >=min_x; i--)
            {
                string line = "";
                for (double j = max_y; j >=min_y; j--)
                {

                    // PointF pf = new PointF((float)j, (float)i);
                    int a = bmc.BinMaplayer.Shapes.FindIndex(p => p.xIndex == i && p.yIndex == j);
                    if (a >= 0)
                    {
                        DieList die = totalDies[a];
                        string id = getzoneindex(-die.wafer.Y, die.wafer.X);
                        if (zoneid.FindIndex(z => z.ToString() == id) >= 0)
                        {
                            inkPositionDies.Add(die.die);

                            if (!InkZoneIdList.ContainsKey(id))
                            {
                                InkZoneIdList[id] = new List<Point>();
                            }
                            InkZoneIdList[id].Add(die.die);
                        }
                        line += ((char)(64 + int.Parse(id))).ToString();
                    }
                    else
                    {
                        line += ".";
                    }
                }
               //Console.WriteLine(line);
            }
        }

        public InkInfo initGetInkSetting(string lotid,string waferid)
        {
            DataTable dt = SqlData.getlotinfo(ink.isDebug, lotid);
            if (dt.Rows.Count > 0)
            {
                string product = dt.Rows[0]["productname"].ToString();
                ink.lotid = lotid;
                ink.wafer = waferid;
                return getInkSetting(product);

            }
            else
            {
                SysItem.writeLog("can not find product info");
            }
            return ink;

        }

        private InkInfo getInkSetting(string product)
        {

            double dnum = 0;
            int inum = 0;
            DataTable dt = SqlData.getInkSettingInfo(ink.isDebug, product);
            if (dt.Rows.Count > 0)
            {
                DataRow dr = dt.Rows[0];
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    SysItem.writeLog("ColumnName: " + dt.Columns[0].ToString() + ", Value:" + dr[i].ToString());
                }

                ink.currentproduct = product;
                ink.OVERINKFLAG = dr["OVERINKFLAG"].ToString();
                ink.SEND_ALARM_MAIL = dr["SEND_ALARM_MAIL"].ToString();
                ink.MAIL_SENDTO = dr["MAIL_SENDTO"].ToString();
                if (double.TryParse(dr["CENTER_MIN_YIELD"].ToString(), out dnum))
                {
                    ink.centerYield = dnum / 100;

                }
                if (double.TryParse(dr["EDAGE_MIN_YIELD"].ToString(), out dnum))
                {
                    ink.edgeYield = dnum / 100;

                }

                if (int.TryParse(dr["EDAGE_COUNT"].ToString(), out inum))
                {
                    ink.egdeCount = inum;

                }

                if (int.TryParse(dr["DISTANCE"].ToString(), out inum))
                {
                    ink.clusterDistance = inum;

                }
                if (int.TryParse(dr["MINCOUNT"].ToString(), out inum))
                {
                    ink.clusterMinCount = inum;

                }

                switch (dr["INKRULE"].ToString())
                {
                    case "InkAllDie":
                        ink.inkrule = InkModel.InkRule.InkAllDie;
                        break;
                    case "InkType":
                        ink.inkrule = InkModel.InkRule.InkType;
                        ink.inkType = dr["INKTYPE"].ToString();
                        if (int.TryParse(dr["RANGE"].ToString(), out inum))
                        {
                            ink.range = inum;
                        }
                        if (int.TryParse(dr["RANGE_Y"].ToString(), out inum))
                        {
                            ink.range_y = inum;
                        }
                        ink.renewflag = dr["RENEW_FLAG"].ToString();
                        break;
                    default:
                        break;
                }

                switch (dr["INKPOSSITION"].ToString())
                {
                    case "FullMap":
                        ink.inkpositioin = InkPosition.FullMap;
                        break;
                    case "Sector":
                        ink.inkpositioin = InkPosition.Sector;
                        ink.sector = new Sector();
                        ink.sector.type = dr["SECTOR_TYPE"].ToString();
                        if (int.TryParse(dr["SECTOR_RADIAN"].ToString(), out inum))
                        {
                            ink.sector.radius = inum;
                        }
                        if (int.TryParse(dr["SECTOR_CIRCLE"].ToString(), out inum))
                        {
                            ink.sector.circle = inum;
                        }
                        if (int.TryParse(dr["SECTOR_ANGLE"].ToString(), out inum))
                        {
                            ink.sector.angle = inum;
                        }
                        ink.sector.location = dr["SECTOR_LOCATION"].ToString();
                        break;
                    case "Shot":
                        ink.inkpositioin = InkPosition.Shot;
                        ink.shotid = dr["SHOT_ID"].ToString();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                SysItem.writeLog("can not find product ink setting");
            }
            return ink;
        }

        public void initInkSetting(string product)
        {

            double dnum = 0;
            int inum = 0;
            SysItem.writeLog("Start to get Ink Setting, productname: " + product);
            DataTable dt = SqlData.getInkSettingInfo(ink.isDebug, product);
            if (dt.Rows.Count > 0)
            {
                SysItem.writeLog("Get Ink Setting Success");
                ink.currentproduct = product;
                DataRow dr = dt.Rows[0];
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    SysItem.writeLog("ColumnName: " + dt.Columns[i].ColumnName+ ", Value: " + dr[i].ToString());
                }
                ink.OVERINKFLAG = dr["OVERINKFLAG"].ToString();
                ink.SEND_ALARM_MAIL = dr["SEND_ALARM_MAIL"].ToString();
                ink.MAIL_SENDTO = dr["MAIL_SENDTO"].ToString();
                if (double.TryParse(dr["CENTER_MIN_YIELD"].ToString(), out dnum))
                {
                    ink.centerYield = dnum / 100;
                  //  SysItem.writeLog("centerYield: " + ink.centerYield.ToString());
                }
                if (double.TryParse(dr["EDAGE_MIN_YIELD"].ToString(), out dnum))
                {
                    ink.edgeYield = dnum / 100;
                 //   SysItem.writeLog("edgeYield: " + ink.edgeYield.ToString());

                }

                if (int.TryParse(dr["EDAGE_COUNT"].ToString(), out inum))
                {
                    ink.egdeCount = inum;
                 //   SysItem.writeLog("egdeCount: " + ink.egdeCount.ToString());

                }


                if (int.TryParse(dr["DISTANCE"].ToString(), out inum))
                {
                    ink.clusterDistance = inum;
                 //   SysItem.writeLog("clusterDistance: " + ink.clusterDistance.ToString());

                }
                if (int.TryParse(dr["MINCOUNT"].ToString(), out inum))
                {
                    ink.clusterMinCount = inum;
                //    SysItem.writeLog("clusterMinCount: " + ink.clusterMinCount.ToString());

                }
              //  SysItem.writeLog("INKRULE: " + dr["INKRULE"].ToString());
                switch (dr["INKRULE"].ToString())
                {
                    case "InkAllDie":
                        ink.inkrule = InkModel.InkRule.InkAllDie;
                        break;
                    case "InkType":
                        ink.inkrule = InkModel.InkRule.InkType;
                        ink.inkType = dr["INKTYPE"].ToString();
                      //  SysItem.writeLog("inkType: " + ink.inkType);
                        if (int.TryParse(dr["RANGE"].ToString(), out inum))
                        {
                            ink.range = inum;
                            //SysItem.writeLog("RANGE: " + ink.range.ToString());

                        }
                        if (int.TryParse(dr["RANGE_Y"].ToString(), out inum))
                        {
                            ink.range_y = inum;
                       //     SysItem.writeLog("range_y: " + ink.range_y.ToString());

                        }
                        ink.renewflag = dr["RENEW_FLAG"].ToString();
                     //   SysItem.writeLog("range_y: " + ink.renewflag);

                        break;
                    default:
                        break;
                }
              //  SysItem.writeLog("INKRULE: " + dr["INKRULE"].ToString());
                switch (dr["INKPOSSITION"].ToString())
                {
                    case "FullMap":
                        ink.inkpositioin = InkPosition.FullMap;
                        break;
                    case "Sector":
                        ink.inkpositioin = InkPosition.Sector;
                        ink.sector = new Sector();
                        ink.sector.type = dr["SECTOR_TYPE"].ToString();
                        if (int.TryParse(dr["SECTOR_RADIAN"].ToString(), out inum))
                        {
                            ink.sector.radius = inum;
                        }
                        if (int.TryParse(dr["SECTOR_CIRCLE"].ToString(), out inum))
                        {
                            ink.sector.circle = inum;
                        }
                        if (int.TryParse(dr["SECTOR_ANGLE"].ToString(), out inum))
                        {
                            ink.sector.angle = inum;
                        }
                        ink.sector.location = dr["SECTOR_LOCATION"].ToString();
                        break;
                    case "Shot":
                        ink.inkpositioin = InkPosition.Shot;
                        ink.shotid = dr["SHOT_ID"].ToString();
                        break;
                    default:
                        break;
                }
            }
            else
            {
                ink.currentproduct = "";
                SysItem.writeLog("can not find product ink setting");
            }
           // return ink;
        }
    }
}

