using System;
using System.Collections.Generic;
using System.Data;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using MySql.Data.MySqlClient;

namespace DAO.DAO.SqlServerDAO.ProjectFile
{
    public class ProjectFileDAO : AbstractSqlServerDAO, IProjectFileDAO
    {
        public void ChangeDescriptionAndSystemLevel(long id, string Description, string SystemLevel)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("edit_file", CommandType.StoredProcedure);
                model.AddParam("i_Id", id, MySqlDbType.Int64);
                model.AddParam("i_Description", Description, MySqlDbType.String);
                model.AddParam("i_SystemLevel", SystemLevel, MySqlDbType.String, 255);

                ExecuteNonQuery(model);
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }
        }


        /**
         * The first param projectId can be obtained from ProjectDTO after create a project
         */
        public long CreateProjectFile(ProjectFileDTO projectFile)
        {
            try
            {
                Loggers.SVP.Info($"{projectFile.Name}: Create file {projectFile.Name} for project {projectFile.FK_ProjectId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_project_file", CommandType.StoredProcedure);
                model.AddParam("i_Name", projectFile.Name, MySqlDbType.VarChar, 255);
                model.AddParam("i_Path", projectFile.Path, MySqlDbType.VarChar, 4000);
                model.AddParam("i_FK_ProjectId", projectFile.FK_ProjectId, MySqlDbType.Int64); //BIGINT
                model.AddParam("i_MathlabVersion", projectFile.MatlabVersion, MySqlDbType.String, 255);
                model.AddParam("i_Description", projectFile.Description, MySqlDbType.String);
                model.AddParam("i_SystemLevel", projectFile.SystemLevel, MySqlDbType.String, 255);
                model.AddParam("i_Status", projectFile.Status, MySqlDbType.Bit);
                model.AddParam("i_ErrorMsg", projectFile.ErrorMsg, MySqlDbType.MediumText);

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

        public void UpdateStatus(ProjectFileDTO projectFile)
        {
            try
            {
                Loggers.SVP.Info($"Update status file {projectFile.Name} for project {projectFile.FK_ProjectId}");

                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("update_project_file_status", CommandType.StoredProcedure);
                model.AddParam("i_FileId", projectFile.Id, MySqlDbType.Int64);
                model.AddParam("i_Status", projectFile.Status, MySqlDbType.Bit);
                model.AddParam("i_ErrorMsg", projectFile.ErrorMsg, MySqlDbType.MediumText);

                ExecuteNonQuery(model);
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }
        }

        public List<ProjectFileDTO> DatatableToListProjectFiles(DataTable ProjectFilesTable)
        {
            List<ProjectFileDTO> projectFiles = new List<ProjectFileDTO>();
            try
            {
                if (ProjectFilesTable == null)
                {
                    return new List<ProjectFileDTO>();
                }

                for (int i = 0; i < ProjectFilesTable.Rows.Count; i++)
                {
                    ProjectFileDTO resourceTyeModel = new ProjectFileDTO();
                    resourceTyeModel.Id = Convert.ToInt64(ProjectFilesTable.Rows[i]["Id"]);
                    resourceTyeModel.Name = ProjectFilesTable.Rows[i]["Name"].ToString();
                    resourceTyeModel.Path = ProjectFilesTable.Rows[i]["Path"].ToString();
                    resourceTyeModel.Description = ProjectFilesTable.Rows[i]["Description"].ToString();
                    resourceTyeModel.FK_ProjectId = string.IsNullOrEmpty(ProjectFilesTable.Rows[i]["Id"].ToString())
                        ? 0
                        : long.Parse(ProjectFilesTable.Rows[i]["FK_ProjectId"].ToString());
                    resourceTyeModel.MatlabVersion = ProjectFilesTable.Rows[i]["MathlabVersion"].ToString();
                    resourceTyeModel.SystemLevel = ProjectFilesTable.Rows[i]["SystemLevel"].ToString();
                    resourceTyeModel.Status = Convert.ToBoolean(ProjectFilesTable.Rows[i]["Status"]);
                    resourceTyeModel.ErrorMsg = ProjectFilesTable.Rows[i]["ErrorMsg"].ToString();
                    resourceTyeModel.LevelVariant = ProjectFilesTable.Rows[i]["LevelVariant"].ToString();
                    projectFiles.Add(resourceTyeModel);
                }

                return projectFiles;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<ProjectFileDTO>();
            }
        }

        public long DeleteProjectFile(long fileId)
        {
            try
            {
                Loggers.SVP.Info($"Delete file {fileId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("delete_file", CommandType.StoredProcedure);
                model.AddParam("i_Id", fileId, MySqlDbType.Int64);
                object obj = ExecuteNonQuery(model);

                long id = Convert.ToInt64(obj.ToString());

                return id;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return 0;
            }
        }

        public List<ProjectFileDTO> ReadAllFiles(long projectId, bool includeVirtualHiddenFiles = false)
        {
            try
            {
                Loggers.SVP.Info($"Read files from project {projectId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_all_files", CommandType.StoredProcedure);
                model.AddParam("i_projectId", projectId, MySqlDbType.Int64);
                model.AddParam("i_includeVirtualFiles", includeVirtualHiddenFiles ? 1 : 0, MySqlDbType.Int32);

                DataTable table = ExecuteReader(model);

                List<ProjectFileDTO> projectFiles = DatatableToListProjectFiles(table);

                return projectFiles;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<ProjectFileDTO>();
            }
        }

        public List<long> GetAllFileIDsOfLevel(long projectId, string level)
        {
            try
            {
                Loggers.SVP.Info($"Read files from project {projectId}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_all_file_of_level_in_projects", CommandType.StoredProcedure);
                model.AddParam("i_projectId", projectId, MySqlDbType.Int64);
                model.AddParam("s_level", level, MySqlDbType.MediumText);

                DataTable table = ExecuteReader(model);
                return DataTableToListId(table);
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<long>();
            }
        }

        public ProjectFileDTO ReadFileById(long id)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_file_by_id", CommandType.StoredProcedure);
                model.AddParam("i_Id", id, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<ProjectFileDTO> list = new List<ProjectFileDTO>();
                list = DatatableToListProjectFiles(dataTable);

                ProjectFileDTO projectFile = list.Count == 0 ? new ProjectFileDTO() : list[0];

                return projectFile;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new ProjectFileDTO();
            }
        }


        public string GetSystemLevel(long projectId, long projectFileId, int count)
        {
            string systemLevel = "";
            switch (count)
            {
                case 0:
                    systemLevel = "ECU";
                    break;
                case 1:
                    systemLevel = "MFMdl";
                    break;
                case 2:
                    systemLevel = "Function";
                    break;
                case 3:
                    systemLevel = "Logic";
                    break;
                case 4:
                    systemLevel = "Block";
                    break;

                default:
                    systemLevel = "Block Level " + count;
                    break;
            }

         return systemLevel;
        }

        public void UpdateSystemLevel(long projectId, long projectFileId, string systemLevel)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("update_systemlevel", CommandType.StoredProcedure);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.Int64);
                model.AddParam("i_ProjectFileId", projectFileId, MySqlDbType.Int64);
                model.AddParam("i_SystemLevel", systemLevel, MySqlDbType.String);

                ExecuteNonQuery(model);

        
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
               
            }
        }

        public void GetAndUpdateSystemLevel(long projectId, long projectFileId, int count)
        {
            string systemLevel = GetSystemLevel(projectId, projectFileId, count);
            UpdateSystemLevel(projectId, projectFileId, systemLevel);
        }


        public List<long> GetSubFileId(long projectId, long projectFileId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("get_sub_file_list", CommandType.StoredProcedure);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.Int64);
                model.AddParam("i_ProjectFileId", projectFileId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<long> subFileList = DataTableToSubfileList(dataTable);
                return subFileList;
            }
            catch (Exception ex)
            {
                Loggers.SVP.RecurException(ex.Message, ex);
                return new List<long>();
            }
        }

        public List<long> DataTableToSubfileList(DataTable dataTable)
        {
            List<long> subFileList = new List<long>();

            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Info("SubfileDataTable is null");
                    return new List<long>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    long subfileId;
                    subfileId = Convert.ToInt64(dataTable.Rows[i]["Id"]);
                    subFileList.Add(subfileId);
                }

                return subFileList;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<long>();
            }
        }

        public List<long> GetFileIdList(long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_all_file_id", CommandType.StoredProcedure);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.UInt64);

                DataTable dataTable = ExecuteReader(model);

                List<long> IdList = DataTableToListId(dataTable);

                return IdList;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<long>();
            }
        }

        public List<long> DataTableToListId(DataTable dataTable)
        {
            List<long> IdList = new List<long>();

            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Info("FileIdListDataTable is null");
                    return new List<long>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    long FileId;
                    FileId = Convert.ToInt64(dataTable.Rows[i]["Id"]);
                    IdList.Add(FileId);
                }

                return IdList;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<long>();
            }
        }

        public ProjectFileDTO ReadFileByName(string fileName, long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_file_by_name", CommandType.StoredProcedure);
                model.AddParam("i_Name", fileName, MySqlDbType.String);
                model.AddParam("i_Id", projectId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<ProjectFileDTO> list = new List<ProjectFileDTO>();
                list = DatatableToListProjectFiles(dataTable);

                ProjectFileDTO projectFile = list.Count == 0 ? new ProjectFileDTO() : list[0];

                return projectFile;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new ProjectFileDTO();
            }
        }

        public List<ProjectFileDTO> FindFilesByFileLevel(string fileLevel, long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("find_projectfiles_by_file_level", CommandType.StoredProcedure);
                model.AddParam("i_FileLevel", fileLevel, MySqlDbType.String);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<ProjectFileDTO> list = new List<ProjectFileDTO>();
                list = DatatableToListProjectFiles(dataTable);
                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<ProjectFileDTO>();
            }
        }

        public void UpdateSystemLevelVariant(long projectId, long projectFileId, string variant)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("update_systemlevelvariant", CommandType.StoredProcedure);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.Int64);
                model.AddParam("i_ProjectFileId", projectFileId, MySqlDbType.Int64);
                model.AddParam("i_Variant", variant, MySqlDbType.String);

                ExecuteNonQuery(model);
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);

            }
        }
    }
}