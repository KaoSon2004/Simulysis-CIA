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
    public class ListDAO : AbstractSqlServerDAO, IListDAO
    {
        public long CreateList(ListDTO list)
        {
            try
            {
                Loggers.SVP.Info($"Create list for system {list.FK_SystemId} in file {list.FK_ProjectFileId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_new_list", CommandType.StoredProcedure);
                model.AddParam("i_FK_SystemId", list.FK_SystemId, MySqlDbType.Int64);
                model.AddParam("i_Properties", list.Properties, MySqlDbType.String);
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

        public void CreateLists(ICollection<ListDTO> lists)
        {
            if (lists.Count == 0)
            {
                return;
            }

            Loggers.SVP.Info(
                $"Create lists for system {lists.First().FK_SystemId} in file {lists.First().FK_ProjectFileId}"
            );

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();
                StringBuilder stringBuilder =
                    new StringBuilder("INSERT INTO List (Id, FK_SystemId, FK_ProjectFileId, Properties) VALUES");

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;


                foreach (ListDTO list in lists)
                {
                    stringBuilder.Append($"(@id{counter}, @parentId{counter}, @fileId{counter}, @props{counter}),");
                    cmd.Parameters.Add(new MySqlParameter($"id{counter}", list.Id));
                    cmd.Parameters.Add(new MySqlParameter($"parentId{counter}", list.FK_SystemId));
                    cmd.Parameters.Add(new MySqlParameter($"fileId{counter}", list.FK_ProjectFileId));
                    cmd.Parameters.Add(new MySqlParameter($"props{counter}", list.Properties));
                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info(
                    $"net_write/read_timeout SET for lists in system {lists.First().FK_SystemId} in file {lists.First().FK_ProjectFileId}"
                );

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {lists.Count} LISTS");
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
                        $"****************FILE {lists.First().FK_ProjectFileId}, SYSTEM {lists.First().FK_SystemId} LISTS SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        public List<ListDTO> ReadLists(long FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_lists", CommandType.StoredProcedure);

                model.AddParam("i_FK_ProjectFileId", FK_ProjectFileId, MySqlDbType.Int64);
                DataTable dataTable = ExecuteReader(model);

                List<ListDTO> list = new List<ListDTO>();
                list = DataTableToListList(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<ListDTO>();
            }
        }

        public List<ListDTO> DataTableToListList(DataTable listDataTable)
        {
            List<ListDTO> list = new List<ListDTO>();

            try
            {
                if (listDataTable == null)
                {
                    return new List<ListDTO>();
                }

                for (int i = 0; i < listDataTable.Rows.Count; i++)
                {
                    ListDTO resourceTypeModel = new ListDTO();
                    resourceTypeModel.Id = Convert.ToInt64(listDataTable.Rows[i]["Id"]);
                    resourceTypeModel.FK_SystemId = Convert.ToInt64(listDataTable.Rows[i]["FK_SystemId"]);
                    resourceTypeModel.FK_ProjectFileId = Convert.ToInt64(listDataTable.Rows[i]["FK_ProjectFileId"]);
                    resourceTypeModel.Properties = listDataTable.Rows[i]["Properties"].ToString();

                    list.Add(resourceTypeModel);
                }
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }

            return list;
        }

        public List<ListDTO> GetAllListsInAProject(long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_lists_in_project", CommandType.StoredProcedure);

                model.AddParam("i_projectId", projectId, MySqlDbType.Int64);
                DataTable dataTable = ExecuteReader(model);

                List<ListDTO> list = new List<ListDTO>();
                list = DataTableToListList(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<ListDTO>();
            }
        }
    }
}