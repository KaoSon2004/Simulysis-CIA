using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace DAO.DAO.SqlServerDAO.ProjectFile
{
    public class ProjectDAO : AbstractSqlServerDAO, IProjectDAO
    {
        public long CreateProject(ProjectDTO project)
        {
            try
            {
                Loggers.SVP.Info($"Create project {project.Name}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("create_new_project", CommandType.StoredProcedure);
                model.AddParam("i_name", project.Name, MySqlDbType.VarChar, 255);
                model.AddParam("i_description",project.Description,MySqlDbType.VarChar, 65535);
                model.AddParam("i_path", project.Path, MySqlDbType.VarChar, 4000);

                if (project.BaseProjectId == 0)
                {
                    model.AddParam("i_baseProjectId", null, MySqlDbType.Int64);
                }
                else
                {
                    model.AddParam("i_baseProjectId", project.BaseProjectId, MySqlDbType.Int64);
                }

                if (project.Version == null)
                {
                    model.AddParam("i_version", null, MySqlDbType.VarChar, 255);
                }
                else
                {
                    model.AddParam("i_version", project.Version, MySqlDbType.VarChar, 255);
                }
               

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

        public List<ProjectDTO> ReadAllProjects()
        {
            CommandExecutionModel model = new CommandExecutionModel();
            model.SetCommand("read_all_projects",CommandType.StoredProcedure);
            DataTable dataTable = ExecuteReader(model);
            List<ProjectDTO> projectList = DataTableToProjectList(dataTable);
            return projectList;
        }

        public List<ProjectDTO> GetProjectVersions(long projectId)
        {
            CommandExecutionModel model = new CommandExecutionModel();
            model.SetCommand("get_project_versions", CommandType.StoredProcedure);
            model.AddParam("i_projectId", projectId, MySqlDbType.Int64);
            DataTable dataTable = ExecuteReader(model);
            List<ProjectDTO> projectList = DataTableToProjectList(dataTable);
            foreach (var project in projectList)
            {
                if (project.Version == null || project.Version == "")
                {
                    project.Version = project.Name;
                }
            }
            
            return projectList;
        }

        public long DeleteProject(long id)
        {
            try
            {
                Loggers.SVP.Info($"Delete project {id}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("delete_project", CommandType.StoredProcedure);
                model.AddParam("i_Id", id, MySqlDbType.Int64);
                ExecuteScalar(model);
                return id;
            }
            catch(Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return -1;
            }
        }

        public ProjectDTO ReadProjectById(long id)
        {
            try
            {
                Loggers.SVP.Info($"Read project {id}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_project_by_id",CommandType.StoredProcedure);
                model.AddParam("i_Id",id,MySqlDbType.Int64);
                DataTable dataTable = ExecuteReader(model);
                List<ProjectDTO> list = new List<ProjectDTO>();
                list = DataTableToProjectList(dataTable);
                ProjectDTO projectDTO = list.Count == 0 ? null : list[0];
                return projectDTO;
            }
            catch(Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return null;
            }
        }

        public ProjectDTO ReadProjectByName(string name)
        {
            try
            {
                Loggers.SVP.Info($"Read project {name}");
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_project_by_name", CommandType.StoredProcedure);
                model.AddParam("i_name", name, MySqlDbType.VarChar, 255);
                DataTable dataTable = ExecuteReader(model);
                List<ProjectDTO> list = new List<ProjectDTO>();
                list = DataTableToProjectList(dataTable);
                ProjectDTO projectDTO = list.Count == 0 ? null : list[0];
                return projectDTO;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return null;
            }
        }

        public List<ProjectDTO> DataTableToProjectList(DataTable projectDataTable)
        {
            List<ProjectDTO> list = new List<ProjectDTO>();
            try
            {
                if (projectDataTable == null)
                {
                    return new List<ProjectDTO>();
                }

                for (int i = 0; i < projectDataTable.Rows.Count; i++)
                {
                    ProjectDTO resourceTypeModel = new ProjectDTO();
                    resourceTypeModel.Id = Convert.ToInt64(projectDataTable.Rows[i]["Id"]);
                    resourceTypeModel.Description = projectDataTable.Rows[i]["Description"].ToString();
                    resourceTypeModel.Name = projectDataTable.Rows[i]["Name"].ToString();
                    resourceTypeModel.Path = projectDataTable.Rows[i]["Path"].ToString();

                    var baseProjectIdVal = projectDataTable.Rows[i]["BaseProjectId"];

                    resourceTypeModel.BaseProjectId = Convert.ToInt64(baseProjectIdVal is DBNull ? 0 : baseProjectIdVal);
                    resourceTypeModel.Version = projectDataTable.Rows[i]["Version"] == null ? null : projectDataTable.Rows[i]["Version"].ToString();
                    resourceTypeModel.SourceLink = projectDataTable.Rows[i]["SourceLink"] == null ? null : projectDataTable.Rows[i]["SourceLink"].ToString();

                    list.Add(resourceTypeModel);

                }

            }

            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<ProjectDTO>();
            }

            return list;
        }

        public void UpdateProjectDescription(long projectId, string description)
        {
            try
            {
                Loggers.SVP.Info("update project description : " + description);
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("update_project_description", CommandType.StoredProcedure);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.Int64);
                model.AddParam("i_description", description, MySqlDbType.String);

                ExecuteNonQuery(model);
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }
        }

        public void SetProjectGitLink(long projectId, string gitLink)
        {
            try
            {
                Loggers.SVP.Info("update project git link : " + gitLink);
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("update_project_git_link", CommandType.StoredProcedure);
                model.AddParam("i_ProjectId", projectId, MySqlDbType.Int64);
                model.AddParam("i_Link", gitLink, MySqlDbType.String);

                ExecuteNonQuery(model);
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }
        }

        public long DeleteProject_V2(long id)
        {
            Loggers.SVP.Info("ABOUT TO DELETE PROJECT "+ id);
            string connectionString = Entities.Configuration.ConnectionString;
            string query = "DELETE FROM project WHERE Id = @Id";


            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();


            using (MySqlConnection connection = new MySqlConnection(connectionString))
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@Id", id);
                command.CommandTimeout = Entities.Configuration.SQLCommandTimeOut*100;
                connection.Open();
                int rowsAffected = command.ExecuteNonQuery();
                Loggers.SVP.Info($"{rowsAffected} rows deleted.");


                stopwatch.Stop();

                Loggers.SVP.Info($"DELETE SUCCESSFULLY IN: {stopwatch.ElapsedMilliseconds} ms");
                connection.Close();
                return rowsAffected;
            }
        }
    }
}
