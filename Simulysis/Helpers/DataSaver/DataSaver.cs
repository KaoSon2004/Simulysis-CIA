using Common;
using DAO.DAO.SqlServerDAO.FileContent;
using Entities;
using Entities.DTO;
using Entities.EntityDataTable;
using Entities.Logging;
using log4net.Repository.Hierarchy;
using MySql.Data.MySqlClient;
using MySqlConnector;
using Simulysis.Helpers.DataSaver.EntityDataTable;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using File = System.IO.File;
using MySqlCommand = MySql.Data.MySqlClient.MySqlCommand;
using MySqlConnection = MySqlConnector.MySqlConnection;
using MySqlTransaction = MySql.Data.MySqlClient.MySqlTransaction;

namespace Simulysis.Helpers.DataSaver
{
    public class DataSaver
    {
        //datatable to CSV
        public String CreateCSVfile(DataTable dtable, String projectPath)
        {
            //create new file path = //uploaded + project + name 
            var csvFolderPath = Path.Combine(projectPath, "CSV");
            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }
            var finalPath = Path.Combine(csvFolderPath, "systems" + ".csv");
            if (!System.IO.File.Exists(finalPath))
            {
                var file = System.IO.File.Create(finalPath);
                file.Close();
            }

            StreamWriter sw = new StreamWriter(finalPath, false);
            int icolcount = dtable.Columns.Count;

            for (int i = 0; i < icolcount; i++)
            {
                String header = "";
                header += dtable.Columns[i].ColumnName;

                if (i < icolcount - 1)
                {
                    header += ",";
                }
                sw.Write(header);
            }
            sw.WriteLine(sw.NewLine);
            foreach (DataRow drow in dtable.Rows)
            {
                for (int i = 0; i < icolcount; i++)
                {

                    if (!Convert.IsDBNull(drow[i]))
                    {
                        String value = drow[i].ToString();
                        if (value.IndexOf("\"") >= 0)
                            value = value.Replace("\"", "\"\"");

                        //If separtor are is in value, ensure it is put in double quotes.
                        if (value.IndexOf(",") >= 0)
                            value = "\"" + value + "\"";

                        //If string contain new line character
                        while (value.Contains("\r"))
                        {
                            value = value.Replace("\r", "");
                        }
                        while (value.Contains("\n"))
                        {
                            value = value.Replace("\n", "");
                        }

                        sw.Write(value);
                    }
                    if (i < icolcount - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
            sw.Dispose();
            return finalPath;
        }

        public void saveDataTableToDatabase(DataTable dataTable, List<MySqlBulkCopyColumnMapping> columnMappings)
        {
            // open the connection
            Loggers.SVP.Info("start to save : " + dataTable.TableName);
            using (var connection = new MySqlConnection(Configuration.ConnectionString + ";AllowLoadLocalInfile=True"))
            {
                //try
                //{
                    connection.Open();
                    var bulkCopy = new MySqlBulkCopy(connection);
                    bulkCopy.DestinationTableName = dataTable.TableName;
                    foreach (var mapping in columnMappings)
                    {
                        bulkCopy.ColumnMappings.Add(mapping);
                    }
                    Loggers.SVP.Info("start write to server");
                    var result = bulkCopy.WriteToServer(dataTable);
                    Loggers.SVP.Info("after write to server");

                    if (result.Warnings.Count != 0)
                    {
                        Loggers.SVP.Warning("Something happened during bulk uploading : "+result.Warnings.ToString());
                    }
                   // this.enableForeignKeyCheck(connection);
                    connection.Close();
                //}
                //catch (Exception ex)
                //{
                //    Loggers.SVP.Exception(ex.Message,ex);
                //}
            }
            Loggers.SVP.Info("end save : " + dataTable.TableName);
            // check for problems
            //if (result.Warnings.Count != 0) { /* handle potential data loss warnings */ }

        }

        public List<List<T>> partition<T>(List<T> values, int chunkSize)
        {
            return values.Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / chunkSize)
                .Select(x => x.Select(v => v.Value).ToList())
                .ToList();
        }

        public void saveSystem(List<SystemDTO> systemDTOs, int chunkSize)
        {
            Loggers.SVP.Info("About to insert " + systemDTOs.Count + " system records");

            List<List<SystemDTO>> systemChunks = this.partition(systemDTOs, chunkSize);

            Loggers.SVP.Info("done create system chunks");

            SystemDataTable systemDataTable = new SystemDataTable();

            foreach (List<SystemDTO> systemChunk in systemChunks)
            {
                DataTable system = systemDataTable.createDataTable(systemChunk);

                if (systemDataTable.ColumnMappings != null)
                {
                    this.saveDataTableToDatabase(system, systemDataTable.ColumnMappings);
                }
                else
                {
                    this.saveDataTableToDatabase(system, systemDataTable.GetColumnMappings());
                }

            }
            Loggers.SVP.Info("done insert systems");
        }

        public void saveBranch(List<BranchDTO> branchDTOs, int chunkSize)
        {
            Loggers.SVP.Info("About to insert " + branchDTOs.Count + " branch records");
            List<List<BranchDTO>> branchChunks = this.partition(branchDTOs, chunkSize);
            Loggers.SVP.Info("done create branch chunk");
            BranchDataTable branchDataTable = new BranchDataTable();

            foreach (List<BranchDTO> branchChunk in branchChunks)
            {
                DataTable branch = branchDataTable.createDataTable(branchChunk);
                if (branchDataTable.ColumnMappings != null)
                {
                    this.saveDataTableToDatabase(branch, branchDataTable.ColumnMappings);
                }
                else
                {
                    this.saveDataTableToDatabase(branch, branchDataTable.GetColumnMappings());
                }

            }
            Loggers.SVP.Info("done insert branch");
        }
        public void saveInstanceData(List<InstanceDataDTO> instanceDataDTOs,int chunkSize)
        {
            Loggers.SVP.Info("About to insert " + instanceDataDTOs.Count + " instance data records");
            List<List<InstanceDataDTO>> instanceDataChunks = this.partition(instanceDataDTOs, chunkSize);
            Loggers.SVP.Info("done create instance data chunk");
            InstanceDataDataTable instanceDataDataTable = new InstanceDataDataTable();

            foreach (List<InstanceDataDTO> instanceDataChunk in instanceDataChunks)
            {
                DataTable branch = instanceDataDataTable.createDataTable(instanceDataChunk);
                if (instanceDataDataTable.ColumnMappings != null)
                {
                    this.saveDataTableToDatabase(branch, instanceDataDataTable.ColumnMappings);
                }
                else
                {
                    this.saveDataTableToDatabase(branch, instanceDataDataTable.GetColumnMappings());
                }

            }
            Loggers.SVP.Info("done insert instance datas");
        }

        public void saveLine(List<LineDTO> lineDTOs,int chunkSize)
        {
            Loggers.SVP.Info("About to insert " + lineDTOs.Count + " instance data records");
            List<List<LineDTO>> lineChunks = this.partition(lineDTOs, chunkSize);
            Loggers.SVP.Info("done create line data chunk");
            LineDataTable lineDataTable = new LineDataTable();

            foreach (List<LineDTO> lineChunk in lineChunks)
            {
                DataTable line = lineDataTable.createDataTable(lineChunk);
                if (lineDataTable.ColumnMappings != null)
                {
                    this.saveDataTableToDatabase(line, lineDataTable.ColumnMappings);
                }
                else
                {
                    this.saveDataTableToDatabase(line, lineDataTable.GetColumnMappings());
                }

            }
            Loggers.SVP.Info("done insert lines");
        }
        public void saveList(List<ListDTO> listDTOs, int chunkSize)
        {
            Loggers.SVP.Info("About to insert " + listDTOs.Count + " list data records");
            List<List<ListDTO>> listChunks = this.partition(listDTOs, chunkSize);
            Loggers.SVP.Info("done create list chunk");
            ListDataTable listDataTable = new ListDataTable();


            foreach (List<ListDTO> listChunk in listChunks)
            {
                DataTable list = listDataTable.createDataTable(listChunk);
                if (listDataTable.ColumnMappings != null)
                {
                    this.saveDataTableToDatabase(list, listDataTable.ColumnMappings);
                }
                else
                {
                    this.saveDataTableToDatabase(list, listDataTable.GetColumnMappings());
                }

            }
            Loggers.SVP.Info("done insert lists");
        }

    

        public void savePort(List<PortDTO> portDTOs, int chunkSize)
        {
            Loggers.SVP.Info("About to insert " + portDTOs.Count + " port data records");
            List<List<PortDTO>> portChunks = this.partition(portDTOs, chunkSize);
            Loggers.SVP.Info("done create port chunk");
            PortDataTable portDataTable = new PortDataTable();

            foreach (List<PortDTO> portChunk in portChunks)
            {
                DataTable port = portDataTable.createDataTable(portChunk);
                if (portDataTable.ColumnMappings != null)
                {
                    this.saveDataTableToDatabase(port, portDataTable.ColumnMappings);
                }
                else
                {
                    this.saveDataTableToDatabase(port, portDataTable.GetColumnMappings());
                }

            }
            Loggers.SVP.Info("done insert ports");
        }

        public static void disableForeignKeyCheck()
        {
            try
            {
                Loggers.SVP.Info("start disable foreign key check");
                var mySqlConnection= new MySql.Data.MySqlClient.MySqlConnection(Configuration.ConnectionString);
                mySqlConnection.Open();
                string disableFK = "SET GLOBAL FOREIGN_KEY_CHECKS = 0";
                MySqlCommand cmd = new MySqlCommand(disableFK, mySqlConnection);
                cmd.ExecuteNonQuery();
                mySqlConnection.Close();
                Loggers.SVP.Info("disable foreign key check");
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }
        }

        public static void enableForeignKeyCheck()
        {
            try
            {
                Loggers.SVP.Info("start enable foreign key check");
                var mySqlConnection = new MySql.Data.MySqlClient.MySqlConnection(Configuration.ConnectionString);
                mySqlConnection.Open();
                string enableFK = "SET GLOBAL FOREIGN_KEY_CHECKS = 1";
                MySqlCommand cmd = new MySqlCommand(enableFK, mySqlConnection);
                cmd.ExecuteNonQuery();
                mySqlConnection.Close();

                Loggers.SVP.Info("enable foreign key check");

            }
            catch(Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }
        }

    }


}
