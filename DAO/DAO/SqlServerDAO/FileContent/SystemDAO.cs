using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Common;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using MySql.Data.MySqlClient;

namespace DAO.DAO.SqlServerDAO.FileContent
{
    public class SystemDAO : AbstractSqlServerDAO, ISystemDAO
    {
        //public long CreateSystem(SystemDTO system)
        //{
        //    try
        //    {
        //        Loggers.SVP.Info($"Create system {system.Name} in file {system.FK_ProjectFileId}");
        //        CommandExecutionModel model = new CommandExecutionModel();
        //        model.SetCommand("create_new_system", CommandType.StoredProcedure);
        //        model.AddParam("i_FK_ParentSystemId", system.FK_ParentSystemId, MySqlDbType.Int64);
        //        model.AddParam("i_BlockType", system.BlockType, MySqlDbType.String, 255);
        //        model.AddParam("i_name", system.Name, MySqlDbType.String, 255);
        //        model.AddParam("i_SID", system.SID, MySqlDbType.String, 255);
        //        model.AddParam("i_FK_ProjectFileId", system.FK_ProjectFileId, MySqlDbType.Int64);
        //        model.AddParam("i_Properties", system.Properties, MySqlDbType.String);

        //        object obj = ExecuteScalar(model);

        //        long id = Convert.ToInt64(obj.ToString());

        //        return id;
        //    }
        //    catch (MySqlException ex)
        //    {
        //        Loggers.SVP.Exception(ex.Message, ex);
        //        return 0;
        //    }
        //}

        public void CreateSystems(ICollection<SystemDTO> systems)
        {
            if (systems.Count == 0)
            {
                return;
            }

            Loggers.SVP.Info($"Create systems in file {systems.First().FK_ProjectFileId}");

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();

                StringBuilder stringBuilder =
                    new StringBuilder(
                        "INSERT INTO `System` (Id, BlockType, Name, sid, FK_ParentSystemId, FK_ProjectFileId, Properties, GotoTag, SourceBlock, SourceFile, ConnectedRefSrcFile, FK_FakeProjectFileId) VALUES"
                    );

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;

                foreach (SystemDTO system in systems)
                {
                    stringBuilder.Append(
                        $"(@id{counter}, @blockType{counter}, @name{counter}, @sid{counter}, @parentId{counter}, @fileId{counter}, @props{counter}, @gotoTag{counter}, @sourceBlock{counter}, @sourceFile{counter}, @connectedRefSrcFile{counter}, @fakeProjectFileId{counter}),");
                    cmd.Parameters.Add(new MySqlParameter($"id{counter}", system.Id));
                    cmd.Parameters.Add(new MySqlParameter($"blockType{counter}", system.BlockType));
                    cmd.Parameters.Add(new MySqlParameter($"name{counter}", system.Name));
                    cmd.Parameters.Add(new MySqlParameter($"sid{counter}", system.SID));
                    cmd.Parameters.Add(new MySqlParameter($"parentId{counter}", system.FK_ParentSystemId));
                    cmd.Parameters.Add(new MySqlParameter($"fileId{counter}", system.FK_ProjectFileId));
                    cmd.Parameters.Add(new MySqlParameter($"props{counter}", system.Properties));
                    cmd.Parameters.Add(new MySqlParameter($"gotoTag{counter}", system.GotoTag));
                    cmd.Parameters.Add(new MySqlParameter($"sourceBlock{counter}", system.SourceBlock));
                    cmd.Parameters.Add(new MySqlParameter($"sourceFile{counter}", system.SourceFile));
                    cmd.Parameters.Add(new MySqlParameter($"@connectedRefSrcFile{counter}", system.ConnectedRefSrcFile));

                    if (system.FK_FakeProjectFileId >= 0)
                        cmd.Parameters.Add(new MySqlParameter($"@fakeProjectFileId{counter}", system.FK_FakeProjectFileId));
                    else
                        cmd.Parameters.Add(new MySqlParameter($"@fakeProjectFileId{counter}", null));

                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info($"net_write/read_timeout SET for systems in file {systems.First().FK_ProjectFileId}");

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {systems.Count} SYSTEMS");
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
                        $"****************FILE {systems.First().FK_ProjectFileId} SYSTEMS SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        public List<SystemDTO> ReadSystems(long FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_systems", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId", FK_ProjectFileId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        private List<SystemDTO> DataTableToListSystemDTO(DataTable dataTable)
        {
            List<SystemDTO> list = new List<SystemDTO>();

            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Error("System datatable is null");
                    return new List<SystemDTO>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    SystemDTO resourceTypeModel = new SystemDTO();
                    resourceTypeModel.Id = string.IsNullOrEmpty(dataTable.Rows[i]["Id"].ToString())
                        ? 0
                        : long.Parse(dataTable.Rows[i]["Id"].ToString());
                    resourceTypeModel.SID = dataTable.Rows[i]["sid"].ToString();
                    resourceTypeModel.Name = dataTable.Rows[i]["Name"].ToString();
                    resourceTypeModel.BlockType = dataTable.Rows[i]["BlockType"].ToString();
                    resourceTypeModel.FK_ParentSystemId =
                        string.IsNullOrEmpty(dataTable.Rows[i]["FK_ParentSystemId"].ToString())
                            ? 0
                            : long.Parse(dataTable.Rows[i]["FK_ParentSystemId"].ToString());
                    resourceTypeModel.FK_ProjectFileId =
                        string.IsNullOrEmpty(dataTable.Rows[i]["FK_ProjectFileId"].ToString())
                            ? 0
                            : long.Parse(dataTable.Rows[i]["FK_ProjectFileId"].ToString());
                    resourceTypeModel.Properties = dataTable.Rows[i]["Properties"].ToString();
                    resourceTypeModel.SourceBlock = dataTable.Rows[i]["SourceBlock"].ToString();
                    resourceTypeModel.SourceFile = dataTable.Rows[i]["SourceFile"].ToString();
                    resourceTypeModel.GotoTag = dataTable.Rows[i]["GotoTag"].ToString();
                    resourceTypeModel.ConnectedRefSrcFile = dataTable.Rows[i]["ConnectedRefSrcFile"].ToString();
                    resourceTypeModel.ContainingFile = dataTable.Columns.Contains("ContainingFile")
                        ? dataTable.Rows[i]["ContainingFile"].ToString()
                        : null;
                    resourceTypeModel.FK_FakeProjectFileId =
                        string.IsNullOrEmpty(dataTable.Rows[i]["FK_FakeProjectFileId"].ToString())
                            ? -1
                            : long.Parse(dataTable.Rows[i]["FK_FakeProjectFileId"].ToString());
                    list.Add(resourceTypeModel);
                }
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }

            return list;
        }

        public List<SystemDTO> SearchInportOutportSystemByNameInAFile(string Name, long ProjectFileId)
        {
            try
            {
                // find system that matchs the name first then find corresponding system later
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("search_inport_outport_system_by_name_in_a_file", CommandType.StoredProcedure);
                model.AddParam("i_Name", Name, MySqlDbType.String);
                model.AddParam("i_FK_ProjectFileId", ProjectFileId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        //FROM GOTO
        public List<SystemDTO> SearchSystemByGotoTagInAFile(string GotoTag, long ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("search_system_by_goto_tag_in_a_file", CommandType.StoredProcedure);
                model.AddParam("i_GotoTag", GotoTag, MySqlDbType.String);
                model.AddParam("i_FK_ProjectFileId", ProjectFileId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        public List<SystemDTO> SearchSystemByCalibrationInAFile(string calibration, long projectFileId, string dataType)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("search_system_by_calibration_in_a_file", CommandType.StoredProcedure);
                model.AddParam("i_Calibration", calibration, MySqlDbType.String);
                model.AddParam("i_FK_ProjectFileId", projectFileId, MySqlDbType.UInt64);
                if (dataType == "single") { dataType = "Value"; }
                model.AddParam("i_DataType", dataType, MySqlDbType.String);
                // model.AddParam("i_Label", Label, MySqlDbType.String)
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }
        public List<SystemDTO> SearchSystemByGoToTagInProject(string gotoTag, long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("search_system_by_goto_tag_in_project",CommandType.StoredProcedure);
                model.AddParam("i_GotoTag", gotoTag, MySqlDbType.String);
                model.AddParam("i_FK_ProjectId", projectId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch(Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }
        public List<SystemDTO> SearchInPortOutPortSystemByNameInProject(string Name, long ProjectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("search_inport_outport_system_by_name_in_project", CommandType.StoredProcedure);
                model.AddParam("i_Name", Name, MySqlDbType.String);
                model.AddParam("i_ProjectId", ProjectId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        public List<SystemDTO> SearchSystemByCalibrationInProject(string calibration, long projectId, string dataType)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("search_system_by_calibration_in_project", CommandType.StoredProcedure);
                model.AddParam("i_Calibration", calibration, MySqlDbType.String);
                model.AddParam("i_FK_ProjectId", projectId, MySqlDbType.UInt64);
                if(dataType == "single") { dataType = "Value"; }
                model.AddParam("i_DataType", dataType, MySqlDbType.String);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        public List<SystemDTO> SearchMatchedFromGoToSystem(SystemDTO systemDTO, long ProjectFileId)
        {
            if (systemDTO.BlockType == Constants.FROM)
            {
                //we will search for Goto
                try
                {
                    CommandExecutionModel model = new CommandExecutionModel();
                    model.SetCommand("search_matched_system", CommandType.StoredProcedure);
                    model.AddParam("i_GotoTag", systemDTO.GotoTag, MySqlDbType.String);
                    model.AddParam("i_BlockType", Constants.GOTO, MySqlDbType.String);
                    model.AddParam("i_FK_ProjectFileId", ProjectFileId, MySqlDbType.Int64);
                    DataTable dataTable = ExecuteReader(model);
                    List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                    return list;
                }
                catch (Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message, ex);
                    return new List<SystemDTO>();
                }
            }

            else
            {
                //We will search for From
                try
                {
                    CommandExecutionModel model = new CommandExecutionModel();
                    model.SetCommand("search_matched_system", CommandType.StoredProcedure);
                    model.AddParam("i_GotoTag", systemDTO.GotoTag, MySqlDbType.String);
                    model.AddParam("i_BlockType", Constants.FROM, MySqlDbType.String);
                    model.AddParam("i_FK_ProjectFileId", ProjectFileId, MySqlDbType.Int64);
                    DataTable dataTable = ExecuteReader(model);
                    List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                    return list;
                }
                catch (Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message, ex);
                    return new List<SystemDTO>();
                }
            }
        }

        public List<SystemDTO> SearchMatchedInportOutPortSystemInProject(SystemDTO systemDTO, long ProjectId)
        {
            if (systemDTO.BlockType == Constants.INPORT)
            {
                //we will search for Outport
                try
                {
                    CommandExecutionModel model = new CommandExecutionModel();
                    model.SetCommand("search_matched_inport_outport_system_in_project", CommandType.StoredProcedure);
                    model.AddParam("i_Name", systemDTO.Name, MySqlDbType.String);
                    model.AddParam("i_BlockType", Constants.OUTPORT, MySqlDbType.String);
                    model.AddParam("i_FK_ProjectId", ProjectId, MySqlDbType.Int64);
                    DataTable dataTable = ExecuteReader(model);
                    List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                    return list;
                }
                catch (Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message, ex);
                    return new List<SystemDTO>();
                }
            }

            else
            {
                //We will search for Inport
                try
                {
                    CommandExecutionModel model = new CommandExecutionModel();
                    model.SetCommand("search_matched_inport_outport_system_in_project", CommandType.StoredProcedure);
                    model.AddParam("i_Name", systemDTO.Name, MySqlDbType.String);
                    model.AddParam("i_BlockType", Constants.INPORT, MySqlDbType.String);
                    model.AddParam("i_FK_ProjectId", ProjectId, MySqlDbType.Int64);
                    DataTable dataTable = ExecuteReader(model);
                    List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                    return list;
                }
                catch (Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message, ex);
                    return new List<SystemDTO>();
                }
            }
        }

        public SystemDTO SearchParentSystem(SystemDTO systemDTO)
        {
            try
            {
                Loggers.SVP.Info("StartfFind parent system");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("find_parent_system", CommandType.StoredProcedure);
                model.AddParam("i_FK_ParentSystemId", systemDTO.FK_ParentSystemId, MySqlDbType.UInt64);
                model.AddParam("i_FK_ProjectFileId", systemDTO.FK_ProjectFileId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                SystemDTO parentSystem = list.Count == 0 ? new SystemDTO() : list[0];
                Loggers.SVP.Info("End find parent system");
                return parentSystem;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new SystemDTO();
            }
        }
        public List<SystemDTO> SearchSystemsByGotoTagInASetOfFiles(string GotoTag, List<long> idList)
        {
            List<SystemDTO> list = new List<SystemDTO>();
            Parallel.ForEach(idList,
                new ParallelOptions { MaxDegreeOfParallelism = Entities.Configuration.MaxThreadNumber },
                fileId =>
                {
                    List<SystemDTO> tempList = SearchSystemByGotoTagInAFile(GotoTag, fileId);
                    lock (list)
                    {
                        list.AddRange(tempList);
                    }
                }
            );

            return list;

        }
        public List<SystemDTO> SearchInPortOutPortSystemsByNameInASetOfFiles(string name, List<long> idList)
        {
            List<SystemDTO> list = new List<SystemDTO>();
            Parallel.ForEach(idList,
                new ParallelOptions {MaxDegreeOfParallelism = Entities.Configuration.MaxThreadNumber},
                fileId =>
                {
                    List<SystemDTO> tempList = SearchInportOutportSystemByNameInAFile(name, fileId);

                    lock (list)
                    {
                        list.AddRange(tempList);
                    }
                }
            );

            return list;
        }

        //public List<SystemDTO> SearchMatchedInportOutPortSystemInASetOfFiles(SystemDTO system, List<long> idList)
        //{
        //    List<SystemDTO> list = new List<SystemDTO>();
        //    Parallel.ForEach(idList,
        //        new ParallelOptions {MaxDegreeOfParallelism = Entities.Configuration.MaxThreadNumber},
        //        fileId =>
        //        {
        //            List<SystemDTO> tempList = SearchMatchedInportOutportSystemInAFile(system, fileId);

        //            lock (list)
        //            {
        //                list.AddRange(tempList);
        //            }
        //        }
        //    );

        //    return list;
        //}

        public List<SystemDTO> SearchSystemByCalibrationInASetOfFiles(string calibration, List<long> idList, string dataType)
        {
            List<SystemDTO> list = new List<SystemDTO>();
            string combindedString = string.Join(",", idList.ToArray());
            Loggers.SVP.Info("Searching Calibration " + dataType + ": " + calibration + " in files: " + combindedString);   
            Parallel.ForEach(idList,
                new ParallelOptions {MaxDegreeOfParallelism = Entities.Configuration.MaxThreadNumber},
                fileId =>
                {
                    List<SystemDTO> tempList = SearchSystemByCalibrationInAFile(calibration, fileId, dataType);

                    lock (list)
                    {
                        list.AddRange(tempList);
                    }
                }
            );

            list.ForEach(p => Loggers.SVP.Info(p.Properties));
            return list;
        }

        public List<SystemDTO> SearchMatchedInportOutportSystemInAFile(SystemDTO systemDTO, long fileId)
        {
            if (systemDTO.BlockType == Constants.INPORT)
            {
                try
                {
                    CommandExecutionModel model = new CommandExecutionModel();
                    model.SetCommand("search_matched_inport_outport_system_in_a_file", CommandType.StoredProcedure);
                    model.AddParam("i_Name", systemDTO.Name, MySqlDbType.String);
                    model.AddParam("i_BlockType", Constants.OUTPORT, MySqlDbType.String);
                    model.AddParam("i_FK_ProjectFileId", fileId, MySqlDbType.Int64);
                    DataTable dataTable = ExecuteReader(model);
                    List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                    return list;
                }
                catch (Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message, ex);
                    return new List<SystemDTO>();
                }
            }
            else
            {
                try
                {
                    CommandExecutionModel model = new CommandExecutionModel();
                    model.SetCommand("search_matched_inport_outport_system_in_a_file", CommandType.StoredProcedure);
                    model.AddParam("i_Name", systemDTO.Name, MySqlDbType.String);
                    model.AddParam("i_BlockType", Constants.INPORT, MySqlDbType.String);
                    model.AddParam("i_FK_ProjectFileId", fileId, MySqlDbType.Int64);
                    DataTable dataTable = ExecuteReader(model);
                    List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                    return list;
                }
                catch (Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message, ex);
                    return new List<SystemDTO>();
                }
            }
        }

        public List<SystemDTO> GetAllFromGotoSystemInAFile(long FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_from_goto_systems_in_a_file", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId", FK_ProjectFileId, MySqlDbType.Int64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        public List<SystemDTO> GetAllInportOutportSystemInAProject(long FK_ProjectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_inport_ouport_system_in_a_project", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectId", FK_ProjectId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        public List<SystemDTO> GetAllInportOutportSystemInAFile(long FK_ProjectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_inport_ouport_system_in_a_file", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId", FK_ProjectFileId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        public List<SystemDTO> GetAllEmptyBlockTypeSystemsInAProject(long ProjectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_empty_blocktype_systems_in_a_project", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectId",ProjectId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);

                return list;
            }
            catch(Exception ex)
            {
                Loggers.SVP.RecurException(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        public List<SystemDTO> GetAllSystemsInAProject(long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_systems_in_a_project", CommandType.StoredProcedure);
                model.AddParam("i_projectId", projectId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<SystemDTO>();
            }
        }

        Queue<SystemDTO> ISystemDAO.GetSystemAndParentAndSubSysChildren(long fileId, long systemId, long parentId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_sys_and_parent_subsys_child", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId", fileId, MySqlDbType.UInt64);
                model.AddParam("i_SystemId", systemId, MySqlDbType.UInt64);
                model.AddParam("i_FK_ParentSystemId", parentId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<SystemDTO> list = DataTableToListSystemDTO(dataTable);
                return new Queue<SystemDTO>(list);
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new Queue<SystemDTO>();
            }
        }
    }
}