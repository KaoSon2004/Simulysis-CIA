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
    public class PortDAO : AbstractSqlServerDAO, IPortDAO
    {
        public long CreatePort(PortDTO port)
        {
            try
            {
                Loggers.SVP.Info($"Create port for system {port.FK_SystemId} in file {port.FK_ProjectFileId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_new_port", CommandType.StoredProcedure);
                model.AddParam("i_FK_SystemId", port.FK_SystemId, MySqlDbType.Int64);
                model.AddParam("i_Properties", port.Properties, MySqlDbType.String);

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

        public void CreatePorts(ICollection<PortDTO> ports)
        {
            if (ports.Count == 0)
            {
                return;
            }

            Loggers.SVP.Info(
                $"Create ports for system {ports.First().FK_SystemId} in file {ports.First().FK_ProjectFileId}"
            );

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();
                StringBuilder stringBuilder =
                    new StringBuilder("INSERT INTO Port (Id, FK_SystemId, FK_ProjectFileId, Properties) VALUES");

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;

                foreach (PortDTO port in ports)
                {
                    stringBuilder.Append($"(@id{counter}, @parentId{counter}, @fileId{counter}, @props{counter}),");
                    cmd.Parameters.Add(new MySqlParameter($"id{counter}", port.Id));
                    cmd.Parameters.Add(new MySqlParameter($"parentId{counter}", port.FK_SystemId));
                    cmd.Parameters.Add(new MySqlParameter($"fileId{counter}", port.FK_ProjectFileId));
                    cmd.Parameters.Add(new MySqlParameter($"props{counter}", port.Properties));
                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info(
                    $"net_write/read_timeout SET for ports in system {ports.First().FK_SystemId} in file {ports.First().FK_ProjectFileId}"
                );

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {ports.Count} PORTS");
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
                        $"****************FILE {ports.First().FK_ProjectFileId}, SYSTEM {ports.First().FK_SystemId} PORTS SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        public List<PortDTO> ReadPorts(long FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_ports", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId", FK_ProjectFileId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);
                List<PortDTO> list = DataTableToListPort(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }

            return new List<PortDTO>();
        }

        public List<PortDTO> DataTableToListPort(DataTable PortDataTable)
        {
            List<PortDTO> list = new List<PortDTO>();
            try
            {
                if (PortDataTable == null)
                {
                    return new List<PortDTO>();
                }

                for (int i = 0; i < PortDataTable.Rows.Count; i++)
                {
                    PortDTO port = new PortDTO();
                    port.Id = string.IsNullOrEmpty(PortDataTable.Rows[i]["Id"].ToString())
                        ? 0
                        : long.Parse(PortDataTable.Rows[i]["Id"].ToString());
                    port.Properties = PortDataTable.Rows[i]["Properties"].ToString();

                    list.Add(port);
                }
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }

            return list;
        }

        public List<PortDTO> GetAllPortsInProject(long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_ports_in_project", CommandType.StoredProcedure);
                model.AddParam("i_projectId", projectId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);
                List<PortDTO> list = DataTableToListPort(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }

            return new List<PortDTO>();
        }
    }
}