using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using MySql.Data.MySqlClient;

namespace DAO.DAO.SqlServerDAO.FileContent
{
    public class LineDAO : AbstractSqlServerDAO, ILineDAO
    {
        public long CreateLine(LineDTO line)
        {
            try
            {
                Loggers.SVP.Info($"Create line for system {line.FK_SystemId} in file {line.FK_ProjectFileId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_new_line", CommandType.StoredProcedure);
                model.AddParam("i_FK_SystemId", line.FK_SystemId, MySqlDbType.Int64);
                model.AddParam("i_Properties", line.Properties, MySqlDbType.String);
                object obj = ExecuteScalar(model);

                long id = Convert.ToInt64(obj.ToString());

                return id;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return 0;
            }
        }

        public void CreateLines(ICollection<LineDTO> lines)
        {
            if (lines.Count == 0)
            {
                return;
            }

            Loggers.SVP.Info(
                $"Create lines for system {lines.First().FK_SystemId} in file {lines.First().FK_ProjectFileId}"
            );

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();
                StringBuilder stringBuilder =
                    new StringBuilder("INSERT INTO Line (Id, FK_SystemId, FK_ProjectFileId, Properties) VALUES");

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;

                foreach (LineDTO line in lines)
                {
                    stringBuilder.Append($"(@id{counter}, @parentId{counter}, @fileId{counter}, @props{counter}),");
                    cmd.Parameters.Add(new MySqlParameter($"id{counter}", line.Id));
                    cmd.Parameters.Add(new MySqlParameter($"parentId{counter}", line.FK_SystemId));
                    cmd.Parameters.Add(new MySqlParameter($"fileId{counter}", line.FK_ProjectFileId));
                    cmd.Parameters.Add(new MySqlParameter($"props{counter}", line.Properties));
                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info(
                    $"net_write/read_timeout SET for lines in system {lines.First().FK_SystemId} in file {lines.First().FK_ProjectFileId}"
                );

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {lines.Count} LINES");
                }
                catch (Exception e)
                {
                    string query = cmd.CommandText;

                    foreach (MySqlParameter parameter in cmd.Parameters)
                    {
                        query = Regex.Replace(
                            query,
                            $@"\b{parameter.ParameterName}\b",
                            parameter.Value != null ? parameter.Value.ToString() : "NULL"
                        );
                    }

                    query = query.Replace("@", "");

                    Loggers.SVP.Info($"FILE VERSION: {VersionUtils.GetFileVersion()}");
                    Loggers.SVP.Info(
                        $"****************FILE {lines.First().FK_ProjectFileId}, SYSTEM {lines.First().FK_SystemId} LINES SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        public List<LineDTO> ReadLines(long FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_lines", CommandType.StoredProcedure);

                model.AddParam("i_FK_ProjectFileId", FK_ProjectFileId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);
                List<LineDTO> list = new List<LineDTO>();
                list = DataTableToListLine(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<LineDTO>();
            }
        }

        public List<LineDTO> DataTableToListLine(DataTable dataTable)
        {
            List<LineDTO> list = new List<LineDTO>();

            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Info("datatable of Line is null");
                    return new List<LineDTO>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    LineDTO resourceTypeModel = new LineDTO();
                    resourceTypeModel.Id = Convert.ToInt64(dataTable.Rows[i]["Id"]);
                    resourceTypeModel.FK_SystemId = Convert.ToInt64(dataTable.Rows[i]["FK_SystemId"]);
                    resourceTypeModel.FK_ProjectFileId = Convert.ToInt64(dataTable.Rows[i]["FK_ProjectFileId"]);
                    resourceTypeModel.Properties = dataTable.Rows[i]["Properties"].ToString();

                    list.Add(resourceTypeModel);
                }
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<LineDTO>();
            }

            return list;
        }

        public List<LineDTO> GetAllLinesInAProject(long projectId)
        {

            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_lines_in_project", CommandType.StoredProcedure);

                model.AddParam("i_projectId", projectId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);
                List<LineDTO> list = new List<LineDTO>();
                list = DataTableToListLine(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<LineDTO>();
            }
        }
    }
}