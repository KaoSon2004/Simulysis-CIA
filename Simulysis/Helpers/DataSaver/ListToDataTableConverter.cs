using Entities.Logging;
using log4net.Repository.Hierarchy;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Simulysis.Helpers.DataSaver
{
    public class ListToDataTableConverter
    {
        public DataTable ToDataTable<T>(List<T> items, string tableName)
        {
            Loggers.SVP.Info("create data table " + tableName + " with size " + items.Count);
            DataTable dataTable = new DataTable(tableName);
            Loggers.SVP.Info("Get all properties");
            PropertyInfo[] Props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Loggers.SVP.Info("After get all properties");

            foreach (PropertyInfo prop in Props)
            {
                //Setting column names as Property names
                dataTable.Columns.Add(prop.Name);
            }
            Loggers.SVP.Info("After add props");

            //the error is definitely here
            // I disposed of a try catch here

            foreach (T item in items)
            {
                var values = new object[Props.Length];
                Loggers.SVP.Info("Before get values");
                for (int i = 0; i < Props.Length; i++)
                {
                    //inserting property values to datatable rows
                    values[i] = Props[i].GetValue(item, null) ?? DBNull.Value;
                }
                Loggers.SVP.Info("After get values");
                try
                {
                    dataTable.Rows.Add(values);
                    Random rnd = new Random();
                    int x = rnd.Next(1,10);
                    if(x == 6)
                    {
                        
                    }
                }
                catch (NullReferenceException ex)
                {
                    //this is super silly but people on the internet tell it works?
                    //https://www.pcreview.co.uk/threads/nullreference-exception-on-datatable-rows-add.2410740/
                    Loggers.SVP.Info("there is exception");
                    dataTable.Rows.Add(values);
                    Loggers.SVP.Exception(ex.Message, ex);
                   
                }
                
                Loggers.SVP.Info("After add row");

            }
            Loggers.SVP.Info("convert table sucessfully");
            //put a breakpoint here and check datatable
            return dataTable;
        }
    }
}
