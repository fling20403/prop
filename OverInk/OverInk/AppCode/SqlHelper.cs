using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using OverInk.AppCode;

namespace OverInk.AppData
{
    public class SqlHelper
    {
        public static DataTable DataAdapter(string constr, string sql, params OracleParameter[] paras)
        {
            OracleConnection conn;
            DataTable dt = new DataTable();
            try
            {
                using (conn = new OracleConnection(constr))
                {
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    if (paras.Length > 0)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddRange(paras);
                    }

                    //SysItem.writeLog(sql);

                    DataSet ds = new DataSet();
                    OracleDataAdapter ad = new OracleDataAdapter(cmd);
                    ad.Fill(ds);
                    return ds.Tables[0];

                }
            }
            catch (Exception e)
            {
                //     SysItem.writeLog(e.Message);
                Console.WriteLine(e.Message);
                return dt;
            }


        }

        public static DataTable DataAdapter_mes(string constr,string sql, params OracleParameter[] paras)
        {
            OracleConnection conn;
            DataTable dt = new DataTable();
            try
            {
                using (conn = new OracleConnection(constr))
                {
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    if (paras.Length > 0)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddRange(paras);
                    }
                    DataSet ds = new DataSet();
                    OracleDataAdapter ad = new OracleDataAdapter(cmd);
                    ad.Fill(ds);
                    return ds.Tables[0];

                }
            }
            catch (Exception e)
            {
                //     SysItem.writeLog(e.Message);
                Console.WriteLine(e.Message);
                return dt;
            }


        }

        public static bool DataTransaction( string constr,string sql, params OracleParameter[] paras)
        {
            OracleConnection conn;
            bool isSuccess = false;

            using (conn = new OracleConnection(constr))
            {
                conn.Open();
                OracleCommand cmd = conn.CreateCommand();
                OracleTransaction tran;
                tran = conn.BeginTransaction(IsolationLevel.ReadCommitted);
                cmd.Transaction = tran;
                try
                {
                    cmd.CommandText = sql;
                    if (paras.Length > 0)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddRange(paras);
                    }
                    cmd.ExecuteNonQuery();
                    tran.Commit();
                    isSuccess = true;
                }
                catch (Exception e)
                {
                    tran.Rollback();
                    Console.WriteLine(e.Message);
                    // e.ToString();
                }
                return isSuccess;
            }
            
        }

        public static int ExecuteNonQuery(string constr,string sql, params OracleParameter[] paras)
        {
            OracleConnection conn;
            try
            {
                using (conn = new OracleConnection(constr))
                {
                    conn.Open();
                    OracleCommand cmd = new OracleCommand(sql, conn);
                    if (paras.Length > 0)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddRange(paras);
                    }
                    return cmd.ExecuteNonQuery();

                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }


        }

    }
}
