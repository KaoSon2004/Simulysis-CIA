using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using Entities;
using Entities.Logging;
using System.IO;

namespace Simulysis.Helpers.CSVBulkLoader
{
    public class BulkLoader
    {
        public int WriteToDatabase(string path,string tablename)
        {
            using (MySqlConnection conn = new MySqlConnection(Entities.Configuration.ConnectionString + ";AllowLoadLocalInfile=True"))
            {
                try
                {
                    Loggers.SVP.Info("start to write to "+ tablename);
                    conn.Open();
                    MySqlBulkLoader bulkLoader = new MySqlBulkLoader(conn);
                    bulkLoader.TableName = tablename;
                    bulkLoader.FileName = path;
                    bulkLoader.FieldTerminator = ",";
                    //   bulkLoader.CharacterSet = "UTF8";
                    bulkLoader.Local = true;
                    bulkLoader.LineTerminator = "\r\n";
                    bulkLoader.FieldQuotationCharacter = '"';
                    bulkLoader.EscapeCharacter = '"';
                    bulkLoader.NumberOfLinesToSkip = 1;
                    bulkLoader.ConflictOption = MySqlBulkLoaderConflictOption.Ignore;
                    bulkLoader.Timeout = 3600 * 10 * 1000;
                    var row_inserted = bulkLoader.Load();
                    conn.Close();
                    Loggers.SVP.Info("end write to " + tablename);
                    Loggers.SVP.Info(row_inserted + " inserted ");
                    return row_inserted;
                }
                
                catch(Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message,ex);
                    Loggers.SVP.Info("error while inserting to table "+ tablename);
                    conn.Close();
                    return 0;
                }

            }

        }
    }
}