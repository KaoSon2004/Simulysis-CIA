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
    public class InstanceDataDAO : AbstractSqlServerDAO, IInstanceDataDAO
    {
        public long CreateInstanceData(InstanceDataDTO instanceData)
        {
            try
            {
                Loggers.SVP.Info(
                    $"Create instanceData for system {instanceData.FK_SystemId} in file {instanceData.FK_ProjectFileId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_new_instancedata", CommandType.StoredProcedure);
                model.AddParam("i_FK_SystemId", instanceData.FK_SystemId, MySqlDbType.Int64);
                model.AddParam("i_Properties", instanceData.Properties, MySqlDbType.String);
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

        public void CreateInstanceDatas(ICollection<InstanceDataDTO> instanceDatas)
        {
            if (instanceDatas.Count == 0)
            {
                return;
            }

            Loggers.SVP.Info(
                $"Create instanceDatas for system {instanceDatas.First().FK_SystemId} in file {instanceDatas.First().FK_ProjectFileId}"
            );

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();
                StringBuilder stringBuilder =
                    new StringBuilder("INSERT INTO InstanceData (Id, FK_SystemId, FK_ProjectFileId, Properties) VALUES");

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;

                foreach (InstanceDataDTO instanceData in instanceDatas)
                {
                    stringBuilder.Append($"(@id{counter}, @parentId{counter}, @fileId{counter}, @props{counter}),");
                    cmd.Parameters.Add(new MySqlParameter($"id{counter}", instanceData.Id));
                    cmd.Parameters.Add(new MySqlParameter($"parentId{counter}", instanceData.FK_SystemId));
                    cmd.Parameters.Add(new MySqlParameter($"fileId{counter}", instanceData.FK_ProjectFileId));
                    cmd.Parameters.Add(new MySqlParameter($"props{counter}", instanceData.Properties));
                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info(
                    $"net_write/read_timeout SET for instanceDatas in system {instanceDatas.First().FK_SystemId} in file {instanceDatas.First().FK_ProjectFileId}"
                );

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {instanceDatas.Count} INSTANCE DATA");
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
                        $"****************FILE {instanceDatas.First().FK_ProjectFileId}, SYSTEM {instanceDatas.First().FK_SystemId} INSTANCEDATAS SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        public List<InstanceDataDTO> ReadInstanceDatas(long i_FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_instancedatas", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId", i_FK_ProjectFileId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<InstanceDataDTO> list = new List<InstanceDataDTO>();

                list = DataTableToListInstanceData(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return null;
            }
        }

        public List<InstanceDataDTO> DataTableToListInstanceData(DataTable dataTable)
        {
            List<InstanceDataDTO> list = new List<InstanceDataDTO>();

            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Error("InstanceData data table is null");
                    return new List<InstanceDataDTO>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    InstanceDataDTO resourceTypeModel = new InstanceDataDTO();

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
            }

            return list;
        }

        public List<InstanceDataDTO> GetAllInstanceDatasInAProject(long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_instancedatas_in_project", CommandType.StoredProcedure);
                model.AddParam("i_projectId", projectId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<InstanceDataDTO> list = new List<InstanceDataDTO>();

                list = DataTableToListInstanceData(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return null;
            }
        }
    }
}