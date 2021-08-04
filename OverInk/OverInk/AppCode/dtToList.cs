using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OverInk.AppCode
{
    public static class dtToList
    {
        public static List<T> dataTableToList<T>(this DataTable dt){
            var ts = new List<T>();
            PropertyInfo[] ps = typeof(T).GetProperties();

            foreach (DataRow dr in dt.Rows)
            {
                T t = Activator.CreateInstance<T>();
                foreach (var p in ps)
                {
                 //   Console.WriteLine(p.Name + ": " + p.GetMethod.ReturnType.ToString());
                   p.SetValue(t, ConvertTo(p.GetMethod.ReturnType,dr[p.Name].ToString()));

                }
                ts.Add(t);
            }
          

            return ts;

        }

        public static object ConvertTo(Type p,string val)
        {
            object o = new object();
            switch (p.Name)
            {
                case "Int32":
                    o = Convert.ToInt32(val);
                    break;

                case "String":
                    o = Convert.ToString(val);
                    break;
                case "Double":
                    o = Convert.ToDouble(val);
                    break;
            }

            return o;
        }
    }
}
