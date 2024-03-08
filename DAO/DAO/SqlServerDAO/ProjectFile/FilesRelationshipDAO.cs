using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace DAO.DAO.SqlServerDAO.ProjectFile
{
    public class FilesRelationshipDAO : AbstractSqlServerDAO, IFilesRelationshipDAO
    {
        public long CreateFilesRelationship(FilesRelationshipDTO filesRelationshipDTO)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_files_relationship", CommandType.StoredProcedure);
                model.AddParam("i_FK_ProjectFileId1", filesRelationshipDTO.FK_ProjectFileId1, MySqlDbType.UInt64);
                model.AddParam("i_FK_ProjectFileId2", filesRelationshipDTO.FK_ProjectFileId2, MySqlDbType.UInt64);
                model.AddParam("i_Count", filesRelationshipDTO.Count, MySqlDbType.UInt32);
                model.AddParam("i_UniCount", filesRelationshipDTO.Count, MySqlDbType.UInt32);
                model.AddParam("i_Type", filesRelationshipDTO.Type, MySqlDbType.Int32);
                model.AddParam("i_RelationshipType", filesRelationshipDTO.RelationshipType, MySqlDbType.Int32);
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

        public void CreateFilesRelationships(ICollection<FilesRelationshipDTO> fileRels)
        {
            if (fileRels.Count == 0)
            {
                return;
            }

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();

                StringBuilder stringBuilder =
                    new StringBuilder(
                        "INSERT INTO FilesRelationship (FK_ProjectFileId1, FK_ProjectFileId2, System1, System2, Count, UniCount, Type, RelationshipType, Name) VALUES"
                    );

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;

                foreach (var fileRel in fileRels)
                {
                    stringBuilder.Append(
                        $"(@file1{counter}, @file2{counter}, @sys1{counter}, @sys2{counter}, @count{counter}, @uniCount{counter}, @type{counter}, @relaType{counter}, @name{counter}),"
                    );

                    cmd.Parameters.Add(new MySqlParameter($"file1{counter}", fileRel.FK_ProjectFileId1));
                    cmd.Parameters.Add(new MySqlParameter($"file2{counter}", fileRel.FK_ProjectFileId2));
                    cmd.Parameters.Add(new MySqlParameter($"sys1{counter}", fileRel.System1));
                    cmd.Parameters.Add(new MySqlParameter($"sys2{counter}", fileRel.System2));
                    cmd.Parameters.Add(new MySqlParameter($"count{counter}", fileRel.Count));
                    cmd.Parameters.Add(new MySqlParameter($"uniCount{counter}", fileRel.UniCount));
                    cmd.Parameters.Add(new MySqlParameter($"type{counter}", fileRel.Type));
                    cmd.Parameters.Add(new MySqlParameter($"relaType{counter}", fileRel.RelationshipType));
                    cmd.Parameters.Add(new MySqlParameter($"name{counter}", fileRel.Name));

                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info("net_write/read_timeout SET for file relationships");

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {fileRels.Count} FILE RELATIONSHIPS");
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
                        $"****************FILE RELATIONSHIP SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        public List<FilesRelationshipDTO> ReadFilesRelationship(long fileId1, long fileId2, RelationshipType type)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_files_relationship", CommandType.StoredProcedure);
                model.AddParam("i_RelationshipType", type, MySqlDbType.UInt32);
                model.AddParam("i_FK_ProjectFileId1", fileId1, MySqlDbType.UInt64);
                model.AddParam("i_FK_ProjectFileId2", fileId2, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<FilesRelationshipDTO> relationships = DataTableToFilesRelationshipDTO(dataTable);

                return relationships;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<FilesRelationshipDTO>();
            }
        }

        public List<FilesRelationshipDTO> ReadFileRelationships(long fileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_parent_equal_file_relationships", CommandType.StoredProcedure);
                model.AddParam("fileId", fileId, MySqlDbType.UInt64);
                DataTable dataTable = ExecuteReader(model);
                List<FilesRelationshipDTO> relationships = DataTableToFilesRelationshipDTO(dataTable);

                return relationships;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<FilesRelationshipDTO>();
            }
        }

        public List<FilesRelationshipDTO> DataTableToFilesRelationshipDTO(DataTable dataTable)
        {
            List<FilesRelationshipDTO> list = new List<FilesRelationshipDTO>();
            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Info("filesRelationship datatable is null");
                    return new List<FilesRelationshipDTO>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    FilesRelationshipDTO resourceTypeModel = new FilesRelationshipDTO();
                    resourceTypeModel.Id = Convert.ToInt64(dataTable.Rows[i]["Id"]);
                    resourceTypeModel.FK_ProjectFileId1 = Convert.ToInt64(dataTable.Rows[i]["FK_ProjectFileId1"]);
                    resourceTypeModel.FK_ProjectFileId2 = Convert.ToInt64(dataTable.Rows[i]["FK_ProjectFileId2"]);
                    resourceTypeModel.System1 = dataTable.Rows[i]["System1"].ToString();
                    resourceTypeModel.System2 = dataTable.Rows[i]["System2"].ToString();
                    resourceTypeModel.Count = Convert.ToInt32(dataTable.Rows[i]["Count"]);
                    resourceTypeModel.UniCount = Convert.ToInt32(dataTable.Rows[i]["UniCount"]);
                    resourceTypeModel.Type = (FileRelationship) Convert.ToInt64(dataTable.Rows[i]["Type"]);
                    resourceTypeModel.RelationshipType = (RelationshipType) Convert.ToInt64(dataTable.Rows[i]["RelationshipType"]);
                    resourceTypeModel.Name = dataTable.Rows[i]["Name"].ToString();

                    list.Add(resourceTypeModel);
                }

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<FilesRelationshipDTO>();
            }
        }

        //public List<long> GetListOfFileIdInDependencyView(long curProjectFileId)
        //{
        //    try
        //    {
        //        CommandExecutionModel model = new CommandExecutionModel();
        //        model.SetCommand("get_list_of_fileIds_in_dependency_view", CommandType.StoredProcedure);
        //        model.AddParam("i_ProjectFileId", curProjectFileId, MySqlDbType.UInt64);

        //        DataTable dataTable = ExecuteReader(model);
        //        List<FilesRelationshipDTO> list = DataTableToFilesRelationshipDTO(dataTable);
        //        List<long> IdList = new List<long>();
        //        IdList.Add(curProjectFileId);
        //        foreach (var relation in list)
        //        {
        //            if (relation.FK_ProjectFileId1 != curProjectFileId)
        //            {
        //                IdList.Add(relation.FK_ProjectFileId1);
        //            }
        //            else
        //            {
        //                IdList.Add(relation.FK_ProjectFileId2);
        //            }
        //        }

        //        return IdList;
        //    }
        //    catch (Exception ex)
        //    {
        //        Loggers.SVP.Exception(ex.Message, ex);
        //        return new List<long>();
        //    }
        //}
    }
}