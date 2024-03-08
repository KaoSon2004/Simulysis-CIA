using Common;
using DAO.DAO;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using Entities.Types;
using Entities;
using Simulysis.Models;
using System;
using Simulysis.Helpers;
using DAO.DAO.SqlServerDAO.FileContent;
using Microsoft.AspNetCore.Mvc;

namespace Simulysis.Controllers
{
    public class FilesController : Controller
    {
        IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;
        IProjectDAO projectDAO = DAOFactory.GetDAO("IProjectDAO") as IProjectDAO;

        private readonly IWebHostEnvironment webHostEnvironment;

        public FilesController(IWebHostEnvironment webHostEnvironment)
        {
            this.webHostEnvironment = webHostEnvironment;
        }

        [Route("Files/Index/{projectId}")]
        public ActionResult Index(ProjectFilesView prevView, long projectId)
        {
            Loggers.SVP.Info("show all project files");
            //long projectId = Convert.ToInt64(Request.Params["projectId"]);
            var viewModel = new ProjectFilesView()
            {
                ItemPerPage = Convert.ToInt32(Request.Query["show"].ToString() == "" ? 10 : Request.Query["show"].ToString()),
                PageSizes = new List<int>() { 10, 20, 50, 100 },
                CurrentPage = Convert.ToInt32(Request.Query["page"].ToString() == "" ? 1 : Request.Query["page"].ToString()),
/*                ItemPerPage = Convert.ToInt32(Request.Query["show"] ?? "10"),
                PageSizes = new List<int> { 10, 20, 50, 100 },
                CurrentPage = Convert.ToInt32(Request.Query["page"] ?? "1"),*/
                SearchContent = prevView.SearchContent ?? "",
                ColumnProps = new List<ColumnProp>
                {
                    new ColumnProp("Name", "File name", 18),
                    new ColumnProp("MatlabVersion", "Version", 10),
                    new ColumnProp("SystemLevel", "Level", 10),
                    new ColumnProp("Path", "Path", 25),
                    new ColumnProp("StatusDisplay", "Status", 12),
                    new ColumnProp("ErrorMsg", "Error message", 25)
                },
                LinkAction = "Show",
                LinkController = "Files",
                PropUseToDelete = "Id"
            };

            ProjectDTO project = projectDAO.ReadProjectById(projectId);

            viewModel.DeleteViews = new List<DeleteView>();
            viewModel.Items = projectFileDAO.ReadAllFiles(projectId)
                .FindAll(file => file.Name.ToLower().Contains(viewModel.SearchContent.ToLower()));
            viewModel.ProjectVersions = projectDAO.GetProjectVersions(projectId).Select(prj => new ExistingProjectInfo()
            {
                ProjectId = prj.Id,
                Name = prj.Version != null ? prj.Version : prj.Name
            }).ToList();

            foreach (dynamic i in viewModel.PaginatedItems())
            {
                viewModel.DeleteViews.Add(new DeleteView()
                {
                    IdToDelete = i.GetType().GetProperty(viewModel.PropUseToDelete).GetValue(i),
                    IsSelected = false,
                    AdditionalInfo = new Dictionary<string, string>()
                    {
                        {"projectName", project.Name},
                        {"projectId", project.Id.ToString()},
                        {"filePath", i.Path}
                    }
                });
            }

            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectId = project.Id;
            ViewBag.BaseProjectId = project.BaseProjectId <= 0 ? project.Id : project.BaseProjectId;

            return View(viewModel);
        }
        [Route("Files/New/{projectId}")]
        [HttpGet]
        public ActionResult New(long projectId)
        {
            Loggers.SVP.Info("add new file");
            var model = new NewFileView()
            {
                ProjectId = projectId,
  
            };

            ProjectDTO project = projectDAO.ReadProjectById(projectId);
            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectId = project.Id;

            return View(model);
        }

        [Route("Files/{projectId}/{fileId}/Show")]
        public ActionResult Show(long projectId, long fileId)
        {
            Loggers.SVP.Info("show a project file");
            ProjectDTO project = projectDAO.ReadProjectById(projectId);
            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectId = project.Id;

            ProjectFileDTO projectFile = projectFileDAO.ReadFileById(fileId);
            ViewBag.FileName = projectFile.Name;
            ViewBag.FileId = projectFile.Id;
            ViewBag.SystemLevel = projectFile.SystemLevel;
            ViewBag.SwapView = Request.Query["swap"].ToString();
            ViewBag.FullNet = Request.Query["fullNet"].ToString();
            ViewBag.DisplayParents = Request.Query["displayParents"].ToString() ?? "true";
            ViewBag.DisplayEquals = Request.Query["displayEquals"].ToString() ?? "true";
            ViewBag.DisplayChildren = Request.Query["displayChildren"].ToString() ?? "false";
            ViewBag.DisplaySubChildren = Request.Query["displaySubChildren"].ToString() ?? "";
            ViewBag.DisplayChildLibraries = Request.Query["displayChildLibraries"].ToString() ?? "true";
            ViewBag.DisplayTreeView = Request.Query["displayTreeView"].ToString() ?? "true";
            ViewBag.ViewType = string.IsNullOrEmpty(Request.Query["viewType"].ToString())
                ? Constants.INOUT_VIEW_TYPE
                : Request.Query["viewType"].ToString();
            ViewBag.WWWRoot = Configuration.WWWRoot;
            ViewBag.RootSysId = Request.Query["rootSysId"];

            return View();
        }
        [HttpPost]
        public ActionResult New(NewFileView newfileView)
        {
            bool status = false;
            string message = string.Empty;

            if (ModelState.IsValid)
            {
                var allowedExtensions = new[] { ".mdl", ".slx" };
                string fileExt = Path.GetExtension(newfileView.File.FileName).ToLower();

                if (allowedExtensions.Contains(fileExt))
                {
                    try
                    {
                        // save file
                        ProjectDTO project = projectDAO.ReadProjectById(newfileView.ProjectId);
                        ViewBag.ProjectName = project.Name;
                        ViewBag.ProjectId = project.Id;

                        string projectFullPath = Path.Combine(webHostEnvironment.ContentRootPath, project.Path);
                        string filePath = Path.Combine(
                            projectFullPath,
                            Path.GetFileName(newfileView.File.FileName)
                        );

                        // check if exists
                        if (System.IO.File.Exists(filePath))
                        {
                            ViewBag.Status = false;
                            ViewBag.Message = Constants.FILE_UP_EXIST;
                            return View(newfileView);
                        }
                        //save file to disk
                        newfileView.File.CopyTo(System.IO.File.OpenWrite(filePath));

                        List<FileContent> newFileContentList = new List<FileContent>();

                        //read content of new file and store in filecontentList
                        long id = FileHelper.ReadSingleFile(
                            filePath, fileExt, newfileView.ProjectId, projectFullPath, "ECU",
                            newfileView.Description, newFileContentList
                        );
                        //just set ECU as default level, we will recalculate and save it later
                        newFileContentList[0].FileLevel = "ECU";

                       // ProjectHelper.ReplaceRefWithRootRef(newFileContentList);
                        //save content of newly uploaded file to db
                        ProjectHelper.SaveFileContents(newFileContentList);

                        //calculate file level for newly uploaded file (and also save parent-child relationship to db)
                        FilesRelationshipHelper filesRelationshipHelper = new FilesRelationshipHelper();

                        List<long> subfileIds = new List<long>(); 

                        subfileIds.AddRange(projectFileDAO.GetSubFileId(newfileView.ProjectId, newFileContentList[0].FileId));

                        foreach (long fileId in subfileIds)
                        {
                            filesRelationshipHelper.SaveChildParentRelationship(fileId,newFileContentList[0].FileId);
                        }

                        ProjectFileDTO subfile = new ProjectFileDTO();
                        if (subfileIds.Count > 0)
                        {
                            subfile = projectFileDAO.ReadFileById(subfileIds[0]);

                            int count = DetermineFileLevelHelper.LevelToCount(subfile.SystemLevel);
                            
                            projectFileDAO.GetAndUpdateSystemLevel(newfileView.ProjectId, newFileContentList[0].FileId, count - 1);
                            //also update to newfilecontentlist
                            newFileContentList[0].FileLevel = DetermineFileLevelHelper.CountToLevel(count - 1);
                        }
                        else if (subfileIds.Count == 0)
                        {
                            //then subfile is ECU level
                            projectFileDAO.GetAndUpdateSystemLevel(newfileView.ProjectId, newFileContentList[0].FileId, 0);
                        }
                        //find parent of newly uploaded file and update relationship

                        int parentFileLevelCount = DetermineFileLevelHelper.LevelToCount(newFileContentList[0].FileLevel);

                        if(newFileContentList[0].FileLevel!="ECU")
                        {
                            string parentFileLevel = DetermineFileLevelHelper.CountToLevel(parentFileLevelCount - 1);
                            //find in db candidate parent have this level
                            List<ProjectFileDTO> candidateParent = projectFileDAO.FindFilesByFileLevel(parentFileLevel,newfileView.ProjectId);
                            foreach (ProjectFileDTO projectFile in candidateParent)
                            {
                                List<long> subfileIdList = projectFileDAO.GetSubFileId(newfileView.ProjectId,projectFile.Id);
                                if(subfileIdList.Contains(newFileContentList[0].FileId))
                                    {
                                    //save to db
                                    filesRelationshipHelper.SaveChildParentRelationship(newFileContentList[0].FileId, projectFile.Id);
                                }
                            }
                        }

                        //equal relationship
                        //add new filecontent to filecontentlist
                        List<FileContent> fileContentList = new List<FileContent>();
                        fileContentList = new FileContentDAO().GetFileContentOfAProject(newfileView.ProjectId);
                        fileContentList.AddRange(newFileContentList);
                        ProjectHelper.ReplaceRefWithRootRef(fileContentList);

                        foreach (var fileContent in fileContentList)
                        {
                            Reader.AddFromGotoConnectedSys(fileContent.Systems, fileContent.Lines);
                        }

                        //recal
                        List<CalibrationDTO> calibrations = ProjectHelper.ReadAllMFiles(projectFullPath, newfileView.ProjectId);
                        List<FilesRelationshipDTO> newFileRels =
                           ProjectHelper.IdentifyEqualRelationship(fileContentList, calibrations, newfileView.ProjectId);
                        //recal all relation and filter => update to db
                        var filteredRel = newFileRels.FindAll(rel => rel.FK_ProjectFileId1 == id || rel.FK_ProjectFileId2 == id);
                        //call update
                        ProjectHelper.SaveRemaining(filteredRel, calibrations);

                        status = true;
                        message = Constants.FILE_UP_SUCCESS;
                    }
                    catch (Exception e)
                    {
                        Loggers.SVP.Exception(e.Message, e);
                        message = Constants.FILE_UP_FAIL;
                    }
                }
                else
                {
                    message = Constants.FILE_UP_FAIL_EXT;
                }
            }
            else
            {
                message = Constants.FILE_UP_FAIL;
            }

            ViewBag.Message = message;
            ViewBag.Status = status;

           

            return View(newfileView);
        }

        [Route("Files/{projectId}/{fileId}/Trackline")]
        [HttpGet]
        public ActionResult Trackline(long projectId, long fileId)
        {
            Loggers.SVP.Info("Trackline from file");
            ProjectDTO project = projectDAO.ReadProjectById(projectId);
            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectId = project.Id;

            ProjectFileDTO projectFile = projectFileDAO.ReadFileById(fileId);
            ViewBag.FileName = projectFile.Name;
            ViewBag.FileId = projectFile.Id;
            ViewBag.WWWRoot = Configuration.WWWRoot;
            ViewBag.SystemLevel = projectFile.SystemLevel;
            ViewBag.RootSysId = Request.Query["rootSysId"].ToString();

            return View();
        }
        [Route("Files/{projectId}/{fileId}/TracklineDetail")]
        [HttpGet]
        public ActionResult TracklineDetail (long projectId, long fileId)
        {
            

            return View();
        }

    }
}