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

        [Route("/Projects/NewFromGit")]
        [HttpGet]
        public IActionResult NewFromGit(long baseProjectId)
        {
            GitUpload upload = new GitUpload();

            if (baseProjectId != 0)
            {
                ProjectDTO originalProject = projectDAO.ReadProjectById(baseProjectId);
                
                if (originalProject != null)
                {
                    upload.GitLink = originalProject.SourceLink;
                }
            }

            return PartialView(upload);
        }

        public static int LevenshteinDistance(string source, string target)
        {
            // degenerate cases
            if (source == target) return 0;
            if (source.Length == 0) return target.Length;
            if (target.Length == 0) return source.Length;

            // create two work vectors of integer distances
            int[] v0 = new int[target.Length + 1];
            int[] v1 = new int[target.Length + 1];

            // initialize v0 (the previous row of distances)
            // this row is A[0][i]: edit distance for an empty s
            // the distance is just the number of characters to delete from t
            for (int i = 0; i < v0.Length; i++)
                v0[i] = i;

            for (int i = 0; i < source.Length; i++)
            {
                // calculate v1 (current row distances) from the previous row v0

                // first element of v1 is A[i+1][0]
                //   edit distance is delete (i+1) chars from s to match empty t
                v1[0] = i + 1;

                // use formula to fill in the rest of the row
                for (int j = 0; j < target.Length; j++)
                {
                    var cost = (source[i] == target[j]) ? 0 : 1;
                    v1[j + 1] = Math.Min(v1[j] + 1, Math.Min(v0[j + 1] + 1, v0[j] + cost));
                }

                // copy v1 (current row) to v0 (previous row) for next iteration
                for (int j = 0; j < v0.Length; j++)
                    v0[j] = v1[j];
            }

            return v1[target.Length];
        }

        /// <summary>
        /// Calculate percentage similarity of two strings
        /// <param name="source">Source String to Compare with</param>
        /// <param name="target">Targeted String to Compare</param>
        /// <returns>Return Similarity between two strings from 0 to 1.0</returns>
        /// </summary>
        public static double CalculateSimilarity(string source, string target)
        {
            if ((source == null) || (target == null)) return 0.0;
            if ((source.Length == 0) || (target.Length == 0)) return 0.0;
            if (source == target) return 1.0;

            int stepsToSame = LevenshteinDistance(source, target);
            return (1.0 - ((double)stepsToSame / (double)Math.Max(source.Length, target.Length)));
        }

        private readonly int PAGE_SIZE = 5;

        [HttpGet]
        [Route("Projects/ListGitBranches")]
        public async Task<IActionResult> ListGitBranches(string projectUrl, string currentPage = "", string filter = "")
        {
            string repositoryUrl = projectUrl;

            // Convert URL to owner and repository name

            var uri = new Uri(repositoryUrl);
            var pathSegments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length < 2)
            {
                Console.WriteLine("Invalid GitHub repository URL.");
                return PartialView();
            }
            else
            {
                repositoryOwner = pathSegments[pathSegments.Length - 2];
                repositoryName = pathSegments[pathSegments.Length - 1].Split('.')[0];
                string token = Entities.Configuration.PAT;

                var github = new GitHubClient(new ProductHeaderValue(repositoryName));

                var tokenAuth = new Credentials(token);
                github.Credentials = tokenAuth;

                var branches = await github.Repository.Branch.GetAll(repositoryOwner, repositoryName);

                IEnumerable<Branch> columnItemsIte = branches;

                if (filter != null && filter != "")
                {
                    columnItemsIte = columnItemsIte.Where(x => (CalculateSimilarity(x.Name, filter) >= 0.2));
                }

                int totalCount = columnItemsIte.Count();

                int pageCount = (totalCount + PAGE_SIZE - 1) / PAGE_SIZE;
                int currentPageReal = currentPage != null ? (currentPage.Length == 0 ? 1 : int.Parse(currentPage)) : 1;

                var itemsArr = columnItemsIte.Take(new System.Range((currentPageReal - 1) * PAGE_SIZE, (currentPageReal - 1) * PAGE_SIZE + Math.Min(PAGE_SIZE, totalCount - (currentPageReal - 1) * PAGE_SIZE)))
                    .Select(x => new GitColumnItem()
                {
                    itemName = x.Name,
                    cssId = x.Name,
                    cssClass = "branch"
                }).ToArray();

                GitColumnView gcv = new GitColumnView()
                {
                    Items = itemsArr,
                    CurrentPage = currentPageReal,
                    PageCount = pageCount,
                    ColumnName = "Branches",
                    ControllerName = "Projects",
                    CallName = "ListGitBranches"
                };

                return PartialView("GitColumn", gcv);
            }
        }

        [HttpGet]
        [Route("Projects/ListGitCommits")]
        public async Task<IActionResult> ListGitCommits(string projectUrl, string branch, string currentPage = "1", string filter = "")
        {
            string repositoryUrl = projectUrl;

            // Convert URL to owner and repository name

            var uri = new Uri(repositoryUrl);
            var pathSegments = uri.AbsolutePath.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathSegments.Length < 2)
            {
                Console.WriteLine("Invalid GitHub repository URL.");
                return PartialView();
            }
            else
            {
                repositoryOwner = pathSegments[pathSegments.Length - 2];
                repositoryName = pathSegments[pathSegments.Length - 1].Split('.')[0];
                string token = Entities.Configuration.PAT;

                var github = new GitHubClient(new ProductHeaderValue(repositoryName));

                var tokenAuth = new Credentials(token);
                github.Credentials = tokenAuth;

                var commits = await github.Repository.Commit.GetAll(repositoryOwner, repositoryName, new CommitRequest()
                {
                    Sha = branch
                });

                IEnumerable<GitHubCommit> columnItemsIte = commits;

                if (filter != null && filter != "")
                {
                    columnItemsIte = columnItemsIte.Where(x => (CalculateSimilarity(x.Commit.Message, filter) >= 0.2));
                }

                int totalCount = columnItemsIte.Count();

                int pageCount = (totalCount + PAGE_SIZE - 1) / PAGE_SIZE;
                int currentPageReal = currentPage != null ? (currentPage.Length == 0 ? 1 : int.Parse(currentPage)) : 1;

                var itemsArr = columnItemsIte.Take(new System.Range((currentPageReal - 1) * PAGE_SIZE, (currentPageReal - 1) * PAGE_SIZE + Math.Min(PAGE_SIZE, totalCount - (currentPageReal - 1) * PAGE_SIZE)))
                    .Select(x => new GitColumnItem()
                    {
                        itemName = x.Commit.Message,
                        cssId = x.Sha,
                        cssClass = "commit"
                    }).ToArray();

                GitColumnView gcv = new GitColumnView()
                {
                    Items = itemsArr,
                    CurrentPage = currentPageReal,
                    PageCount = pageCount,
                    ColumnName = "Commits",
                    ControllerName = "Projects",
                    CallName = "ListGitCommits"
                };

                return PartialView("GitColumn", gcv);
            }
        }

        [Route("Projects/CreateNewFromGit")]
        [HttpPost]
        public async Task<IActionResult> CreateNewFromGitAsync(GitUpload upload)
        {

            string token = Entities.Configuration.PAT;

            var github = new GitHubClient(new ProductHeaderValue(repositoryName));

            var tokenAuth = new Credentials(token);
            github.Credentials = tokenAuth;

            var targetCommit = await github.Git.Commit.Get(repositoryOwner, repositoryName, upload.Commit);
            var treeResponse = await github.Git.Tree.GetRecursive(repositoryOwner, repositoryName, upload.Commit);

            var tree = treeResponse.Tree;

            string fileName = repositoryName;
            string folderPath = Path.Combine(webHostEnvironment.ContentRootPath, Constants.UPLOADED_PROJ_ROOT_PATH);
            string projectPath = Path.Combine(folderPath, fileName);
            
            Directory.CreateDirectory(projectPath);

            /*string projectNameNoZip = Path.ChangeExtension(fileName, "");*/


            string message = "";
            bool status = false;
            long baseProjectId = upload.OriginalProjectId;

            ProjectDTO? originalProject = (upload.OriginalProjectId != 0) ? projectDAO.ReadProjectById(upload.OriginalProjectId) : null;
            
            string unqProjectName = (upload.OriginalProjectId == 0) || originalProject == null ? upload.ProjectName : originalProject.Name + "$$" + upload.ProjectName;

            if (HasProjectExisted(unqProjectName, out _))
            {
                message = Constants.PROJ_ALREADY_EXISTS;
            }
            else
            {
                int count = 0;
                foreach (var entry in tree)
                {

                    if (entry.Type == TreeType.Blob)
                    {
                        var entryExt = Path.GetExtension(entry.Path).ToLower();
                        if (entryExt == ".slx" || entryExt == ".mdl")
                        {
                            var content = await github.Repository.Content.GetRawContent(repositoryOwner, repositoryName, entry.Path);


                            string filePath = Path.Combine(projectPath, entry.Path);
                            System.IO.File.WriteAllBytes(filePath, content);
                        }
                    }
                    else if (entry.Type == TreeType.Tree) // Directory
                    {
                        Console.WriteLine(++count);
                        string subfolderPath = Path.Combine(projectPath, entry.Path);
                        Directory.CreateDirectory(subfolderPath);
                    }
                }

                status = UploadProject(unqProjectName, fileName, (upload.OriginalProjectId == 0) ? "" : upload.ProjectName, "", true, ref message, projectPath, baseProjectId,
                    (originalProject != null) ? null : upload.GitLink);
            }

            ViewBag.Status = status;
            ViewBag.Message = message;

            if (Convert.ToBoolean(ViewBag.Status) == false)
            {
                return StatusCode(500);
            }

            return Ok();
        }
/*        [Route("Projects/NewFromGit/{branch}/{sha}/{index}")]
        public async Task<IActionResult> NewFromGitAsync(GitUpload gitUpload, string branch, string sha, string index)
        {
            
            
           
           
            



          

            return View(gitUpload);
        }*/
        public ActionResult Test()
        {          
            return View();
        }
    }
}