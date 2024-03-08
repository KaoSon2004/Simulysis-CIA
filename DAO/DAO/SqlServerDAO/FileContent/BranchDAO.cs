using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using MySql.Data.MySqlClient;
using Entities.Logging;

namespace DAO.DAO.SqlServerDAO.FileContent
{
    public class BranchDAO : AbstractSqlServerDAO, IBranchDAO

    {
        public long CreateBranch(BranchDTO branch)
        {
            try
            {
                Loggers.SVP.Info($"Create branch for line {branch.FK_LineId} in file {branch.FK_ProjectFileId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_new_branch", CommandType.StoredProcedure);
                model.AddParam("i_Fk_LineId", branch.FK_LineId, MySqlDbType.Int64);
                model.AddParam("i_Properties", branch.Properties, MySqlDbType.String);
                model.AddParam("i_FK_BranchId", branch.FK_BranchId, MySqlDbType.Int64);
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

        public void CreateBranches(ICollection<BranchDTO> branches)
        {
            if (branches.Count == 0)
            {
                return;
            }

            Loggers.SVP.Info(
                $"Create branches for line {branches.First().FK_LineId} in file {branches.First().FK_ProjectFileId}"
            );

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();
                StringBuilder stringBuilder =
                    new StringBuilder(
                        "INSERT INTO Branch (Id, FK_LineId, FK_ProjectFileId, Properties, FK_BranchId) VALUES");

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;

                foreach (BranchDTO branch in branches)
                {
                    stringBuilder.Append(
                        $"(@id{counter}, @lineId{counter}, @fileId{counter}, @props{counter}, @branchId{counter}),");
                    cmd.Parameters.Add(new MySqlParameter($"id{counter}", branch.Id));
                    cmd.Parameters.Add(new MySqlParameter($"lineId{counter}", branch.FK_LineId));
                    cmd.Parameters.Add(new MySqlParameter($"fileId{counter}", branch.FK_ProjectFileId));
                    cmd.Parameters.Add(new MySqlParameter($"props{counter}", branch.Properties));
                    cmd.Parameters.Add(new MySqlParameter($"branchId{counter}", branch.FK_BranchId));
                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info(
                    $"net_write/read_timeout SET for branches in line {branches.First().FK_LineId} in file {branches.First().FK_ProjectFileId}"
                );

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {branches.Count} BRANCHES");
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
                        $"****************FILE {branches.First().FK_ProjectFileId} BRANCHES SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        public List<BranchDTO> ReadBranchs(long i_FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();

                model.SetCommand("read_branches", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId", i_FK_ProjectFileId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<BranchDTO> list = new List<BranchDTO>();
                list = DataTableToListBranch(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return null;
            }
        }

        public List<BranchDTO> DataTableToListBranch(DataTable dataTable)
        {
            List<BranchDTO> list = new List<BranchDTO>();

            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Error("branch data table is null");
                    return new List<BranchDTO>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    BranchDTO resourceTypeModel = new BranchDTO();
                    resourceTypeModel.Id = Convert.ToInt64(dataTable.Rows[i]["Id"]);
                    resourceTypeModel.FK_LineId = Convert.ToInt64(dataTable.Rows[i]["FK_LineId"]);
                    resourceTypeModel.FK_ProjectFileId = Convert.ToInt64(dataTable.Rows[i]["FK_ProjectFileId"]);
                    resourceTypeModel.Properties = dataTable.Rows[i]["Properties"].ToString();
                    resourceTypeModel.FK_BranchId = Convert.ToInt64(dataTable.Rows[i]["FK_BranchId"]);

                    list.Add(resourceTypeModel);
                }
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<BranchDTO>();
            }

            return list;
        }

        public List<BranchDTO> GetAllBranchesInAProject(long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();

                model.SetCommand("get_all_branches_in_project", CommandType.StoredProcedure);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<BranchDTO> list = new List<BranchDTO>();
                list = DataTableToListBranch(dataTable);

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