using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OverInk
{
    public class InkModel
    {
        public class DieList
        {
            public Point die { get; set; }
            public Point wafer { get; set; }
            public string bin_code { get; set; }
            public string location { get; set; }
            public string shotid { get; set; }

        }

        public class InkInfo
        {
            public string OverInkVersion = "Manual";
            public string isDebug = "1";

            public string exportpath = "D:\\Over_INK_Manual";
            public string cpfilepath = "";
            public string mergedpathbk = "";
            public string mergedpathAOI = "";

            public string lotid { get; set; }
            public string parentlotid { get; set; }
            public string wafer { get; set; }
            public string currentproduct { get; set; }
            public string cpproduct { get; set; }

            public List<string> stepname = new List<string>();
            public List<string> jobintime = new List<string>();
            public List<string> jobouttime = new List<string>();
            public List<string> ppid = new List<string>();



            // Y N
            public string OVERINKFLAG { get; set; }
            // 1 0
            public string SEND_ALARM_MAIL { get; set; }
            public string MAIL_SENDTO { get; set; }
            public string MAIL_CC { get; set; }
            public string MAIL_MSG { get; set; }

            //  public class InkTrigger {
            public Double centerYield { get; set; }
            public Double edgeYield { get; set; }
            public int egdeCount { get; set; }

            public int clusterDistance { get; set; }
            public int clusterMinCount { get; set; }

            public InkRule inkrule = InkRule.InkType;
            public string inkType { get; set; }
            public int range { get; set; }
            public int range_y { get; set; }
            public string renewflag { get; set; }

            public Sector sector { get; set; }

            public InkPosition inkpositioin = InkPosition.FullMap;

            public string shotid { get; set; }
            public string shotforcedink { get; set; }
        }

        public class Sector
        {

            public double radius { get; set; }
            public double circle { get; set; }
            public string type { get; set; }
            public double angle { get; set; }
            public string location { get; set; }

        }

        public enum InkPosition
        {
            FullMap,
            Sector,
            Shot
        }
        public enum InkRule
        {
            InkAllDie,
            InkType
        }
    }
  
}
