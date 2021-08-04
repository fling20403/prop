using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using System.Configuration;
using OverInk.AppCode;

namespace OverInk.AppData
{
    public class SqlData
    {
        protected static string _constr_rpt = "Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST =172.21.10.6)(PORT=1521))(CONNECT_DATA=(FAILOVER=ON)(LOAD_BALANCE=ON)(SERVER=DEDICATED)(SERVICE_NAME=jkprpt)(FAILOVER_MODE=(TYPE=session)(METHOD=basic)(RETRIES=180)(DELAY=5))));User Id=rpt;Password=wsxedc0120;Connection Timeout=300;Min Pool Size=16;Max Pool Size=100;";
        protected static string _constr_yms = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = 172.21.13.200)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)(SERVICE_NAME = edadb)));"+
                                            "User Id=EDA;Password=EDA;Connection Timeout=300;Min Pool Size=16;Max Pool Size=100;";
        protected static string _constr_rpt_t = "Data Source = (DESCRIPTION = (ADDRESS = (PROTOCOL = TCP)(HOST = 172.21.25.1)(PORT = 1521))(CONNECT_DATA = (FAILOVER = ON)" +
                 "(LOAD_BALANCE = ON)(SERVER = DEDICATED)(SERVICE_NAME = jktrptdb)(FAILOVER_MODE = (TYPE = session)(METHOD = basic)(RETRIES = 180)(DELAY = 5)))); " +
                 "User Id = rpt; Password=rpt;Connection Timeout = 300; Min Pool Size=16;Max Pool Size=100;";
        protected static string _constr_yms_t = "Data Source=(DESCRIPTION =(ADDRESS = (PROTOCOL = TCP)(HOST = 192.168.6.149)(PORT = 1521))(CONNECT_DATA =(SERVER = DEDICATED)" +
                " (SERVICE_NAME = jktedadb)));User Id=eda;Password=eda;Connection Timeout=300;Min Pool Size=16;Max Pool Size=100;";

        internal static DataTable getcpmapdata(InkModel.InkInfo ink)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "with shotmap as (select a2.die_x_from_bl,a2.die_y_from_bl,a1.die_pitch_x * 1000 die_pitch_x,a1.die_pitch_y*1000 die_pitch_y," +
                        " max(die_x_from_bl) over(partition by a2.shotmap_info_key) max_x," +
                        " min(die_x_from_bl) over(partition by a2.shotmap_info_key) min_x," +
                        " max(die_y_from_bl) over(partition by a2.shotmap_info_key) max_y," +
                        " min(die_y_from_bl) over(partition by a2.shotmap_info_key) min_y," +
                        " a2.die_bl_loc_x,a2.die_bl_loc_y,a2.shot_id,a1.shotmap_info_key,a1.mainproduct product " +
                        "  from shotmap_info a1 " +
                        " inner join shotmap_die a2 on a2.IS_EFFECTIVE_DIE = 'Y' and a2.shotmap_info_key = a1.shotmap_info_key " +
                        " where a1.mainproduct = '{0}') ";
               for (int i = 0; i < ink.stepname.Count; i++)
                {
                    sql += ",cp"+i.ToString()+" as ( select f1.product,to_number(f2.die_x) + case when f1.center_die_x is null then 0 " +
                        " else nvl(f4.positioning_die_x, 0) - nvl(f1.center_die_x, 0) end as die_x，" +
                        " to_number(f2.die_y) + case when f1.center_die_y is null then 0 else nvl(f4.positioning_die_y, 0) - nvl(f1.center_die_y, 0) " +
                        " end as die_y,f3.bin_name,f2.initial_die_x,f2.initial_die_y from (select product, WAFER, MEAS_TIME, case when " +
                        " ORIGINAL_WAFER_NOTCH = 'R' then 'Right' when ORIGINAL_WAFER_NOTCH = 'L' then 'left' when ORIGINAL_WAFER_NOTCH = 'U' then  'Up' " +
                        " when ORIGINAL_WAFER_NOTCH = 'D' then 'Down' end as notch, cp_type, center_die_x, center_die_y from ( select c.*, " +
                        " row_number() over(order by MEAS_TIME desc) rn from cp_wafer_sum c where c.wafer = '{1}' " +
                        " and MEAS_TIME >= to_date('"+ink.jobintime[i] + "', 'yyyymmdd hh24miss') " +
                        " and MEAS_TIME <= to_date('" + ink.jobouttime[i] + "', 'yyyymmdd hh24miss') " +
                        " and CP_TEST_PROG = '" +ink.ppid[i] +"'  ) where rn = 1) f1 " +
                        " left join v_cp_map_data f2 on f2.wafer = '{1}' and f1.MEAS_TIME = f2.meas_time and f1.cp_type = f2.cp_type " +
                        " left join (select distinct product, cp_type, bin_name from cp_bin_def where BIN_TYPE = 'G') f3 on f2.bin = " +
                        " f3.bin_name and f1.cp_type = f3.cp_type left join cp_prod_info f4 on f1.product = f4.product)";
                }

                sql += " select a.*, case when(die_x > max_x - {2} or die_x < min_x + {2}) or " +
                        " (die_y < min_y + {2} or die_y > max_y - {2}) then 'Edge' else 'Center' end as position  from( " +
                        " select a.*, max(die_x) over(partition by die_y) max_x, min(die_x) over(partition by die_y) min_x, " +
                        " max(die_y) over(partition by die_x) max_y, min(die_y) over(partition by die_x) min_y   from( " +
                        " select DIE_PITCH_X, DIE_PITCH_Y, DIE_X - 1 die_x, DIE_Y - 1 die_y, WAFER_X, WAFER_Y, SHOT_ID, " +
                        " case when sum(BIN_CODE) = 0 then 'N' when sum(BIN_CODE) < 0 then 'X' else '1' end as bin_code from( " +
                        " select s1.die_pitch_x, s1.die_pitch_y, s1.die_x_from_bl die_x, s2.initial_die_x, s2.initial_die_y, s1.die_y_from_bl die_y, " +
                        " s1.DIE_BL_LOC_X wafer_x, s1.DIE_BL_LOC_Y wafer_y, s1.shot_id, " +
                        " case when s2.die_x is null then 0 when s2.bin_name is null then - 999 else 1 end as bin_code " +
                        " from shotmap s1 left join cp0 s2 on s1.die_x_from_bl = s2.die_x and s1.die_y_from_bl = s2.die_y /*where s1.product = s2.product*/ ";
                    for (int i = 1; i < ink.stepname.Count; i++)
                {
                    sql += " union select s1.die_pitch_x, s1.die_pitch_y, s1.die_x_from_bl die_x, s2.initial_die_x, s2.initial_die_y," +
                       " s1.die_y_from_bl die_y, s1.DIE_BL_LOC_X wafer_x, s1.DIE_BL_LOC_Y wafer_y,s1.shot_id,case when s2.die_x is " +
                       " null then 0 when s2.bin_name is null then - 999 else 1 end as bin_code from shotmap s1 left " +
                       " join cp" +i.ToString()+ " s2 on s1.die_x_from_bl = s2.die_x and s1.die_y_from_bl = s2.die_y /* where s1.product = s2.product*/ ";
                }

                sql += " ) group by DIE_PITCH_X, DIE_PITCH_Y, DIE_X, DIE_Y, WAFER_X, WAFER_Y, SHOT_ID) a) a ";
                sql = string.Format(sql, ink.cpproduct, ink.lotid + "#" + ink.wafer, ink.egdeCount.ToString());
             //   SysItem.writeLog(sql);
                dt = SqlHelper.DataAdapter(ink.isDebug == "1" ? _constr_yms_t : _constr_yms, sql);
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return dt;
            }
        }

        internal static DataTable getcpmapdata(string isdebug,string wafer, int edgecount)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = "with cp0 as (select f1.product,to_number(f2.die_x) + case when f1.center_die_x is null then 0 " +
                        " else nvl(f4.positioning_die_x, 0) - nvl(f1.center_die_x, 0) end as die_x，" +
                        " to_number(f2.die_y) + case when f1.center_die_y is null then 0 else nvl(f4.positioning_die_y, 0) - nvl(f1.center_die_y, 0) " +
                        " end as die_y,f3.bin_name,f2.initial_die_x,f2.initial_die_y from (select product, WAFER, MEAS_TIME, case when " +
                        " ORIGINAL_WAFER_NOTCH = 'R' then 'Right' when ORIGINAL_WAFER_NOTCH = 'L' then 'left' when ORIGINAL_WAFER_NOTCH = 'U' then  'Up' " +
                        " when ORIGINAL_WAFER_NOTCH = 'D' then 'Down' end as notch, cp_type, center_die_x, center_die_y from ( select c.*, " +
                        " row_number() over(order by MEAS_TIME desc) rn from cp_wafer_sum c where c.wafer = '{1}') where rn = 1) f1 " +
                        " left join v_cp_map_data f2 on f2.wafer = '{1}' and f1.MEAS_TIME = f2.meas_time and f1.cp_type = f2.cp_type " +
                        " left join (select distinct product, cp_type, bin_name from cp_bin_def where BIN_TYPE = 'G') f3 on f2.bin = " +
                        " f3.bin_name and f1.cp_type = f3.cp_type left join cp_prod_info f4 on f1.product = f4.product)," +
                        " shotmap as (select a2.die_x_from_bl,a2.die_y_from_bl,a1.die_pitch_x * 1000 die_pitch_x,a1.die_pitch_y *1000 die_pitch_y," +
                        " max(die_x_from_bl) over(partition by a2.shotmap_info_key) max_x," +
                        " min(die_x_from_bl) over(partition by a2.shotmap_info_key) min_x," +
                        " max(die_y_from_bl) over(partition by a2.shotmap_info_key) max_y," +
                        " min(die_y_from_bl) over(partition by a2.shotmap_info_key) min_y," +
                        " a2.die_bl_loc_x,a2.die_bl_loc_y,a2.shot_id,a1.shotmap_info_key " +
                        " from shotmap_info a1 " +
                        " inner join shotmap_die a2 on a2.IS_EFFECTIVE_DIE = 'Y' and a2.shotmap_info_key = a1.shotmap_info_key " +
                        " where a1.mainproduct in (select product from cp0)) " +
                        " select a.*, case when(die_x > max_x - {2} or die_x < min_x + {2}) or " +
                        " (die_y < min_y + {2} or die_y > max_y - {2}) then 'Edge' else 'Center' end as position  from( " +
                        " select a.*, max(die_x) over(partition by die_y) max_x, min(die_x) over(partition by die_y) min_x, " +
                        " max(die_y) over(partition by die_x) max_y, min(die_y) over(partition by die_x) min_y   from( " +
                        " select DIE_PITCH_X, DIE_PITCH_Y, DIE_X - 1 die_x, DIE_Y - 1 die_y, WAFER_X, WAFER_Y, SHOT_ID, " +
                        " case when sum(BIN_CODE) = 0 then 'N' when sum(BIN_CODE) < 0 then 'X' else '1' end as bin_code from( " +
                        " select s1.die_pitch_x, s1.die_pitch_y, s1.die_x_from_bl die_x, s2.initial_die_x, s2.initial_die_y, s1.die_y_from_bl die_y, " +
                        " s1.DIE_BL_LOC_X wafer_x, s1.DIE_BL_LOC_Y wafer_y, s1.shot_id, " +
                        " case when s2.die_x is null then 0 when s2.bin_name is null then - 999 else 1 end as bin_code " +
                        " from shotmap s1 left join cp0 s2 on s1.die_x_from_bl = s2.die_x and s1.die_y_from_bl = s2.die_y " +
                        " ) group by DIE_PITCH_X, DIE_PITCH_Y, DIE_X, DIE_Y, WAFER_X, WAFER_Y, SHOT_ID) a) a ";
                sql = string.Format(sql,"", wafer, edgecount.ToString());

                dt = SqlHelper.DataAdapter(isdebug == "1" ? _constr_yms_t : _constr_yms, sql);
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return dt;
            }
        }

        internal static DataTable getlotinfo(string isdebug, string lotid)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = string.Format("select substr(PRODUCTNAME,1,6) productname from fwlot where appid='{0}'", lotid);

                dt = SqlHelper.DataAdapter(isdebug == "1" ? _constr_rpt_t : _constr_rpt, sql);
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return dt;
            }
        }

        internal static DataTable getInkSettingInfo(string isdebug,string product)
        {
            DataTable dt = new DataTable();
            try
            {
                string sql = string.Format("select * from autoink_prod_info where product='{0}'", product.Substring(0,6));

                dt = SqlHelper.DataAdapter(isdebug == "1" ? _constr_yms_t : _constr_yms, sql);
                return dt;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return dt;
            }
        }
    }
}
