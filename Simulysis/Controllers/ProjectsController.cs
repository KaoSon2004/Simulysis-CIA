using Common;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities;
using Entities.DTO;
using Entities.Logging;
using Entities.Types;
using Microsoft.AspNetCore.Mvc;
using Simulysis.Helpers;
using Simulysis.Helpers.DataSaver;
using Simulysis.Models;

using Octokit;

namespace Simulysis.Controllers
{
    public class ProjectsController : Controller
    {
        IProjectDAO projectDAO = DAOFactory.GetDAO("IProjectDAO") as IProjectDAO;
        IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        private readonly IWebHostEnvironment webHostEnvironment;
        
        private static string repositoryOwner;
        private static string repositoryName;
        private static IEnumerable<TreeItem> Files;
        

        public ProjectsController(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }

        // GET: Project
        public ActionResult Index(TableView prevModel)
        {
            Loggers.SVP.Info("Get list of projects");

            var viewModel = new TableView()
            {
                ItemPerPage = Convert.ToInt32(Request.Query["show"].ToString() == "" ? 10 : Request.Query["show"].ToString()),
                PageSizes = new List<int>() { 10, 20, 50, 100 },
                CurrentPage = Convert.ToInt32(Request.Query["page"].ToString() == "" ? 1 : Request.Query["page"].ToString()),
                SearchContent = prevModel.SearchContent ?? "",
                ColumnProps = new List<ColumnProp>()
                {
                    new ColumnProp("Name", "Project name", 35),
                    new ColumnProp("Description", "Description", 65)
                },
                LinkAction = "Index",
                LinkController = "Files",
                PropUseToDelete = "Id"
            };

            viewModel.DeleteViews = new List<DeleteView>();
            viewModel.Items = projectDAO.ReadAllProjects()
                .FindAll(project => project.Name.ToLower().Contains(viewModel.SearchContent.ToLower()));

            foreach (dynamic i in viewModel.PaginatedItems())
            {
                viewModel.DeleteViews.Add(new DeleteView()
                {
                    IdToDelete = i.GetType().GetProperty(viewModel.PropUseToDelete).GetValue(i),
                    IsSelected = false,
                    AdditionalInfo = new Dictionary<string, string>()
                    {
                        {"projectPath", i.Path}
                    }
                });
            }

            Loggers.SVP.Info($"List of {viewModel.ItemPerPage} at page {viewModel.CurrentPage}");

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult New(long baseProjectId)
        {
            ProjectView model = new ProjectView();
            List<ProjectDTO> projects = projectDAO.ReadAllProjects();

            model.ExistingProjectInfos = projects.Select(prj => new ExistingProjectInfo()
            {
                ProjectId = prj.Id,
                Name = prj.Name,
                IsGitProject = prj.SourceLink != null && prj.SourceLink != ""
            }).ToList();

            model.ExistingProjectInfos.Insert(0, new ExistingProjectInfo()
            {
                Name = "None",
                ProjectId = 0
            });
            model.OriginalProjectId = baseProjectId;

            return View(model);
        }

        private bool HasProjectExisted(string projectName, out long projectId)
        {
            projectId = 0;

            var projectObj = projectDAO.ReadProjectByName(projectName.Trim());
            if (projectObj != null)
            {
                projectId = projectObj.Id;
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool HasProjectExisted(string projectName, string projectVersion, out long projectId)
        {
            projectId = 0;

            var projectObj = projectDAO.ReadProjectByName(projectName.Trim());
            if (projectObj != null)
            {
                projectId = projectObj.Id;
                if (projectObj.Version == projectVersion)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool UploadProject(string projectName, string projectDesc, string version, string projectZipFilePath, bool safeUpload, ref string message, string projectPathYet, long baseProjectId,
            string gitLink = null)
        {
            long projectId = -1;

            string projectPath = "";
            if (projectPathYet != "")
            {
                projectPath = projectPathYet;
            }
            else
            {
                projectPath = ProjectHelper.GetProjectPath(
                    Path.Combine(webHostEnvironment.ContentRootPath, Constants.UPLOADED_PROJ_ROOT_PATH),
                    projectName
                );
            }


            try
            {
                Loggers.SVP.Info($"NUMBER OF READING THREADS: {Entities.Configuration.MaxThreadNumber}");
                Loggers.SVP.Info($"NUMBER OF INSERTING TO DB THREADS: {Entities.Configuration.MaxInsertThreadNumber}");
                Loggers.SVP.Info($"SQL COMMAND TIMEOUT: {Entities.Configuration.SQLCommandTimeOut}");
                Loggers.SVP.Info($"FILE VERSION: {GenericUtils.GetFileVersion()}");

                string projectDescription = "";
                DateTime startTime = DateTime.Now;

                DataSaver.disableForeignKeyCheck();
                if(projectPathYet == "")
                {
                    ProjectHelper.ExtractZip(projectZipFilePath, projectPath);
                    System.IO.File.Delete(projectZipFilePath);
                }


                ProjectDTO project = new ProjectDTO()
                {
                    Name = projectName.Trim(),
                    Description = projectDesc,
                    Path = $"{Constants.UPLOADED_PROJ_ROOT_PATH}/{projectName.Trim()}",
                    BaseProjectId = baseProjectId,
                    Version = version
                };

                Loggers.SVP.Info("SAVE PROJECT TO DB");
                projectId = projectDAO.CreateProject(project);

                Loggers.SVP.Info("Start read project files");

                FileContent.fileContentList = new List<FileContent>();
                FileContent.fileContentList = ProjectHelper.ReadAllFiles(projectId, projectPath);
                int totalProjectFile = FileContent.fileContentList.Count();

                Loggers.SVP.Info("start identify child-parent relationships");
                List<FilesRelationshipDTO> childParentRelationships = ProjectHelper.IdentifyChildParentRelationships(projectId, FileContent.fileContentList);

                if (safeUpload == true)
                {
                    Loggers.SVP.Info("normal upload");
                    ProjectHelper.SaveFileContents(FileContent.fileContentList);
                }
                else
                {
                    Loggers.SVP.Info("bulk upload");
                    ProjectHelper.BulkInsertFileContents(FileContent.fileContentList, projectPath);
                }


                Loggers.SVP.Info("start determine file level");
                DetermineFileLevelHelper.DetermineFileLevel(projectId, FileContent.fileContentList);

                Loggers.SVP.Info("start read constant file");
                List<CalibrationDTO> calibrations = ProjectHelper.ReadAllMFiles(projectPath, projectId);

                Loggers.SVP.Info("start identify all equal relationship between files in project");
                List<FilesRelationshipDTO> equalRelationships = ProjectHelper.IdentifyEqualRelationship(FileContent.fileContentList, calibrations, projectId);

                Loggers.SVP.Info("start save remaining");
                ProjectHelper.SaveRemaining(equalRelationships.Concat(childParentRelationships).ToList(), calibrations);

                DateTime endTime = DateTime.Now;
                projectDescription += "Total upload time : " + (endTime - startTime).TotalSeconds + " seconds" + "\n";
                projectDescription += "Total files : " + totalProjectFile + "\n";
                projectDescription += "Status : success";
                projectDAO.UpdateProjectDescription(projectId, projectDescription);

                if (gitLink != null)
                {
                    if (baseProjectId == 0)
                    {
                        projectDAO.SetProjectGitLink(projectId, gitLink);
                    }
                }

                DataSaver.enableForeignKeyCheck();

                message = Constants.PROJ_UP_SUCCESS;
                Loggers.SVP.Info("PROJECT UPLOADED SUCCESSFULLY");
            }
            catch (Exception ex)
            {
                Loggers.SVP.Info("PROJECT UPLOAD FAILED");
                Loggers.SVP.Exception(ex.Message, ex);
                ProjectHelper.DeleteProject(projectId, projectPath);
                Loggers.SVP.Exception(ex.Message, ex);
                message = Constants.PROJ_UP_FAIL;

                return false;
            }
            finally
            {
                FileContent.fileContentList = new List<FileContent>();
            }

            return true;
        }

        [HttpPost]
        public ActionResult New(ProjectView projectView)
        {
            ModelState.Remove("Description");

            Loggers.SVP.Info("Upload project");

            bool status = false;
            string message = string.Empty;

            if (ModelState.IsValid)
            {
                if (!ProjectHelper.CheckExtension(projectView.File.FileName))
                {
                    message = Constants.PROJ_UP_FAIL_EXT;
                }
                else
                {
                    if (HasProjectExisted(projectView.ProjectName.Trim(), out _))
                    {
                        message = Constants.PROJ_ALREADY_EXISTS;
                    }
                    else
                    {
                        try
                        {
                            string projectPath = ProjectHelper.GetProjectPath(
                                Path.Combine(webHostEnvironment.ContentRootPath, Constants.UPLOADED_PROJ_ROOT_PATH),
                                projectView.ProjectName
                            );

                            string zipPath = ProjectHelper.SaveZipToDisk(
                                projectPath,
                                Path.GetFileName(projectView.File.FileName),
                                projectView.File
                            );

                            status = UploadProject(projectView.ProjectName, projectView.Description, projectView.ProjectName, zipPath, projectView.safeUpload, ref message, "", projectView.OriginalProjectId);
                        }
                        catch (Exception ex)
                        {
                            Loggers.SVP.Info("PROJECT UPLOAD FAILED");
                            Loggers.SVP.Exception(ex.Message, ex);
                            message = Constants.PROJ_UP_FAIL;
                        }
                    }
                }
            }
            else
            {
                message = Constants.PROJ_UP_FAIL;
            }

            ViewBag.Message = message;
            ViewBag.Status = status;

            return View(projectView);
        }


        [HttpPost]
        public ActionResult Delete(TableView tableView)
        {
            Loggers.SVP.Info("Delete");
            if (tableView.DeleteViews == null)
            {
                Loggers.SVP.Info(Constants.PROJ_DEL_NONE);
                TempData[Constants.TEMPDATA_DEL_STATUS_PROP] = false;
                TempData[Constants.TEMPDATA_DEL_MSG_PROP] = Constants.PROJ_DEL_NONE;
                return RedirectToAction("Index");
            }

            bool none = true;
            Parallel.For(0, tableView.DeleteViews.Count, new ParallelOptions { MaxDegreeOfParallelism = Entities.Configuration.MaxThreadNumber },
                (i, state) =>
                {
                    DeleteView view = tableView.DeleteViews[i];
                    Loggers.SVP.Info($"Delete project {view.IdToDelete}");
                    if (view.IsSelected)
                    {
                        projectDAO.DeleteProject(view.IdToDelete);

                        string projectPath = Path.Combine(webHostEnvironment.ContentRootPath, Request.Form[$"projectPath#{i}"]);

                        if (Directory.Exists(projectPath))
                        {
                            Directory.Delete(projectPath, true);
                        }

                        none = false;
                    }
                }
            );

            if (none)
            {
                Loggers.SVP.Info(Constants.PROJ_DEL_NONE);
                TempData[Constants.TEMPDATA_DEL_STATUS_PROP] = false;
                TempData[Constants.TEMPDATA_DEL_MSG_PROP] = Constants.PROJ_DEL_NONE;
            }
            else
            {
                Loggers.SVP.Info(Constants.PROJ_DEL_SUCCESS);
                TempData[Constants.TEMPDATA_DEL_STATUS_PROP] = true;
                TempData[Constants.TEMPDATA_DEL_MSG_PROP] = Constants.PROJ_DEL_SUCCESS;
            }

            return RedirectToAction("Index");
        }

        public ActionResult Test()
        {          
            return View();
        }
    }
}