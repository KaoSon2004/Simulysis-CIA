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
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Octokit;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Ajax.Utilities;
using System;

using System.Net.Http;
using System.Security.AccessControl;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using MySqlX.XDevAPI;
using System.Data.SqlClient;
using System.Data;
using System.Drawing;
using OfficeOpenXml;
using MySql.Data.MySqlClient;
using DAO.DAO.SqlServerDAO.FileContent;

using MySqlX.XDevAPI.Relational;
using Microsoft.Office.Interop.Excel;
using Constants = Common.Constants;
using DataTable = System.Data.DataTable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;

namespace Simulysis.Controllers
{
    public class AnalysisController : Controller
    {
        IProjectDAO projectDAO = DAOFactory.GetDAO("IProjectDAO") as IProjectDAO;
        IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;
        IFilesRelationshipDAO fileRelationshipDAO = DAOFactory.GetDAO("IFilesRelationshipDAO") as IFilesRelationshipDAO;

        private readonly IWebHostEnvironment webHostEnvironment;

        private static string repositoryOwner;
        private static string repositoryName;
        private static IEnumerable<TreeItem> Files;

        // Notice: FK_ProjectFileId in deleteds are mapped to corresponding FK_ProjectFileId in added.
        private static List<LineDTO> deletedLineFilesDTO = new List<LineDTO>();
        private static List<LineDTO> addedLineFilesDTO = new List<LineDTO>();
        private static List<SystemDTO> deletedSystemFilesDTO = new List<SystemDTO>();
        private static List<SystemDTO> addedSystemFilesDTO = new List<SystemDTO>();
        private static List<SystemDTO> changedSystemFilesDTO = new List<SystemDTO>();
        private static List<long> treeImpactSet = new List<long>();
        private static List<ProjectFileDTO> impactSetProjectFileDTO = new List<ProjectFileDTO>();
        private static List<SystemDTO> impactSetSystemDTO = new List<SystemDTO>();

        // Version names:
        private static string newVersionName;
        private static string oldVersionName;

        // Current dig depth value:
        private static int curDigDepthValue;

        //Added Files list for tree highlight
        private static List<string> addedFilesDTO = new List<string>();
        public AnalysisController(IWebHostEnvironment webHostEnvironment)
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
                },
                LinkAction = "Index",
                LinkController = "Analysis",
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
                });
            }

            Loggers.SVP.Info($"List of {viewModel.ItemPerPage} at page {viewModel.CurrentPage}");


            return View(viewModel);

        }

        [Route("Analysis/Index/{projectId}")]
        public ActionResult Index(TableView prevModel, long projectId)
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
                },
                LinkAction = "Index",
                LinkController = "Analysis",
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
                });
            }

            Loggers.SVP.Info($"List of {viewModel.ItemPerPage} at page {viewModel.CurrentPage}");
            Loggers.SVP.Info("show a project file");

            ProjectDTO project = projectDAO.ReadProjectById(projectId);
            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectId = project.Id;

            ViewBag.FileName = project.Name;
            ViewBag.FileId = 0;
            ViewBag.SystemLevel = "Root";
            ViewBag.SwapView = Request.Query["swap"].ToString();
            ViewBag.FullNet = Request.Query["fullNet"].ToString();
            ViewBag.DisplayParents = Request.Query["displayParents"].ToString() ?? "true";
            ViewBag.DisplayEquals = Request.Query["displayEquals"].ToString() ?? "true";
            ViewBag.DisplayChildren = Request.Query["displayChildren"].ToString() ?? "false";
            ViewBag.DisplaySubChildren = Request.Query["displaySubChildren"].ToString() ?? "";
            ViewBag.DisplayChildLibraries = Request.Query["displayChildLibraries"].ToString() ?? "true";
            ViewBag.DisplayTreeView = "true";
            ViewBag.ViewType = string.IsNullOrEmpty(Request.Query["viewType"].ToString())
                ? Constants.INOUT_VIEW_TYPE
                : Request.Query["viewType"].ToString();
            ViewBag.WWWRoot = Configuration.WWWRoot;
            ViewBag.RootSysId = Request.Query["rootSysId"];

            return View(viewModel);
        }

        [Route("Analysis/Index/{projectId}/{fileId}")]
        public ActionResult Index(TableView prevModel, long projectId, long fileId)
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
                },
                LinkAction = "Index",
                LinkController = "Analysis",
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
                });
            }

            Loggers.SVP.Info($"List of {viewModel.ItemPerPage} at page {viewModel.CurrentPage}");


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
            ViewBag.DisplayTreeView = "true";
            ViewBag.ViewType = string.IsNullOrEmpty(Request.Query["viewType"].ToString())
                ? Constants.INOUT_VIEW_TYPE
                : Request.Query["viewType"].ToString();
            ViewBag.WWWRoot = Configuration.WWWRoot;
            ViewBag.RootSysId = Request.Query["rootSysId"];

            return View(viewModel);
        }

        [HttpGet]
        public ActionResult New()
        {
            return View();
        }

        [Route("Analysis/ProjectComparison")]
        public ActionResult ProjectComparison()
        {
            return View(projectDAO.ReadAllProjects().Select(x => new ExistingProjectInfo()
            {
                ProjectId = x.Id,
                Name = x.Name
            }));
        }

        [Route("Analysis/ProjectComparisonGetVersions/{projectId}")]
        public ActionResult ProjectComparisonGetVersions(long projectId)
        {
            Loggers.SVP.Info("Get list of projects");

            var viewModel = new AnalysisView()
            {
                ItemPerPage = Convert.ToInt32(Request.Query["show"].ToString() == "" ? 10 : Request.Query["show"].ToString()),
                PageSizes = new List<int>() { 10, 20, 50, 100 },
                CurrentPage = Convert.ToInt32(Request.Query["page"].ToString() == "" ? 1 : Request.Query["page"].ToString()),
                SearchContent = "",
                ColumnProps = new List<ColumnProp>()
                {
                    new ColumnProp("Version", "Project version", 35),
                    new ColumnProp("Id", "Id", 35),
                },
                LinkAction = "Index",
                LinkController = "Files",
                PropUseToDelete = "Id"
            };

            viewModel.DeleteViews = new List<DeleteView>();
            viewModel.Items = projectDAO.GetProjectVersions(projectId)
                .FindAll(project => project.Name.ToLower().Contains(viewModel.SearchContent.ToLower()));

            foreach (dynamic i in viewModel.PaginatedItems())
            {
                viewModel.DeleteViews.Add(new DeleteView()
                {
                    IdToDelete = i.GetType().GetProperty(viewModel.PropUseToDelete).GetValue(i),
                });
            }

            Loggers.SVP.Info($"List of {viewModel.ItemPerPage} at page {viewModel.CurrentPage}");

            return PartialView(viewModel);

        }

        public List<long> GetNeighborSet(long fileId, int digDepth)
        {
            List<long> neighbors = new();

            var relationships = fileRelationshipDAO.ReadFileRelationships(fileId);
            foreach (var relationship in relationships)
            {
                if (relationship.Type == FileRelationship.Child_Parent || relationship.Type == FileRelationship.Parent_Child)
                {
                    if (relationship.RelationshipType == RelationshipType.In_Out || relationship.RelationshipType == RelationshipType.From_Go_To)
                    {
                        neighbors.Add((relationship.FK_ProjectFileId1 == fileId) ? relationship.FK_ProjectFileId2 : relationship.FK_ProjectFileId1);
                    }
                }
            }

            if (digDepth > 1)
            {
                List<long> neighborDiscovered = new();

                foreach (var neighbor in neighbors)
                {
                    var discovered = GetNeighborSet(neighbor, digDepth - 1);
                    neighborDiscovered.AddRange(discovered.Where(x => !neighbors.Contains(x) && !neighborDiscovered.Contains(x)));
                }

                neighbors.AddRange(neighborDiscovered);
            }

            return neighbors;
        }

        public List<long> GetCallerSet(long fileId)
        {
            List<long> caller = new();

            var relationships = fileRelationshipDAO.ReadFileRelationships(fileId);
            foreach (var relationship in relationships)
            {
                if (relationship.Type == FileRelationship.Child_Parent && relationship.FK_ProjectFileId1 == fileId)
                {
                    if (relationship.RelationshipType == RelationshipType.In_Out || relationship.RelationshipType == RelationshipType.From_Go_To)
                    {
                        caller.Add(relationship.FK_ProjectFileId2);
                    }
                }
            }

            return caller;
        }

        public List<long> GetCalleeSet(long fileId)
        {
            List<long> caller = new();

            var relationships = fileRelationshipDAO.ReadFileRelationships(fileId);
            foreach (var relationship in relationships)
            {
                if (relationship.Type == FileRelationship.Child_Parent && relationship.FK_ProjectFileId2 == fileId)
                {
                    if (relationship.RelationshipType == RelationshipType.In_Out || relationship.RelationshipType == RelationshipType.From_Go_To)
                    {
                        caller.Add(relationship.FK_ProjectFileId1);
                    }
                }
            }

            return caller;
        }

        public List<long> CoreComputation(List<long> callGraph, List<long> changeSet, int digDepth)
        {
            List<long> coreSet = new();
            foreach (var unit in callGraph)
            {
                if (!changeSet.Contains(unit))
                {
                    var neighborSet = GetNeighborSet(unit, digDepth);
                    var neighborSetInChangeSetCount = neighborSet.Where(x => changeSet.Contains(x)).Count();
                    if (neighborSetInChangeSetCount > 0)
                    {
                        coreSet.Add(unit);
                    }
                }
            }
            coreSet.AddRange(changeSet);
            return coreSet;
        }

        public List<long> ImpactSetComputation(List<long> callGraph, List<long> coreSet)
        {
            List<long> impactSet = new(coreSet);
            while (true)
            {
                bool impactSetStabled = true;

                foreach (var unit in callGraph)
                {
                    if (!impactSet.Contains(unit))
                    {
                        var callerSet = GetCallerSet(unit);
                        var calleeSet = GetCalleeSet(unit);

                        if ((callerSet.Where(x => impactSet.Contains(x)).Count() != 0) && (calleeSet.Where(x => impactSet.Contains(x)).Count() != 0))
                        {
                            impactSet.Add(unit);
                            impactSetStabled = false;
                        }
                    }
                }

                if (impactSetStabled)
                {
                    break;
                }
            }

            return impactSet;
        }

        public List<long> ImpactSetComputation(long projectId, List<long> changeSet, int digDepth)
        {
            List<long> callGraph = projectFileDAO.ReadAllFiles(projectId, false).Select(x => x.Id).ToList();
            return ImpactSetComputation(callGraph, CoreComputation(callGraph, changeSet, digDepth));
        }

        // Calculate the impact set again when user change dig depth value in tree
        [HttpGet]
        [Route("Analysis/ProjectComparison/DigDepthChange")]
        public async Task<IActionResult> DigDepthChange(int digDepthValue, int newProjectId)
        {
            Console.WriteLine(digDepthValue);
            Console.WriteLine("Dig depth change inited!");
            long projectId = newProjectId;
            if (projectId == 0)
            {
                Console.WriteLine("There is no project id in the comparison tree");
            }

            HashSet<long> idChanged = changedSystemFilesDTO.Select(x => x.FK_ProjectFileId).ToHashSet();
            addedSystemFilesDTO.ForEach(x => idChanged.Add(x.FK_ProjectFileId));
            deletedSystemFilesDTO.ForEach(x => idChanged.Add(x.FK_NewVersionProjectFileID));
            addedFilesDTO.ForEach(x => idChanged.Add(int.Parse(x)));

            idChanged.Remove(-1);

            if (projectId != 0 && idChanged != null)
            {

                treeImpactSet = ImpactSetComputation(projectId, idChanged.ToList(), digDepthValue);
                curDigDepthValue = digDepthValue;
                ViewBag.digDepthValue = curDigDepthValue;

                ViewBag.treeImpactSet = new List<long>();
                foreach (long speaker in treeImpactSet)
                {
                    ViewBag.treeImpactSet.Add(speaker);
                }
            }
            else
            {
                Console.WriteLine("Error: Cannot calculate Impact Set when changing Dig Depth Value");
            }
            TableView tableView = new TableView()
            {
                treeImpactSet = treeImpactSet
            };
            for (int i = 0; i < treeImpactSet.Count; i++)
            {
                Console.WriteLine(treeImpactSet[i]);
            }
            return PartialView("_CompareTreePartial", tableView);
        }

        [Route("Analysis/Index/{projectId}/{fileId}/TreeNode")]
        public ActionResult TreeNode(long projectId, long fileId)
        {
            ViewBag.deletedLineFilesDTO = new List<LineDTO>();
            ViewBag.addedLineFilesDTO = new List<LineDTO>();
            ViewBag.deletedSystemFilesDTO = new List<SystemDTO>();
            ViewBag.addedSystemFilesDTO = new List<SystemDTO>();
            ViewBag.changedSystemFilesDTO = new List<SystemDTO>();
            ViewBag.impactSetSystemDTO = new List<SystemDTO>();

            foreach (LineDTO speaker in deletedLineFilesDTO)
            {
                ViewBag.deletedLineFilesDTO.Add(speaker);
            }

            foreach (LineDTO speaker in addedLineFilesDTO)
            {
                ViewBag.addedLineFilesDTO.Add(speaker);
            }

            foreach (SystemDTO speaker in deletedSystemFilesDTO)
            {
                ViewBag.deletedSystemFilesDTO.Add(speaker);
            }

            foreach (SystemDTO speaker in addedSystemFilesDTO)
            {
                ViewBag.addedSystemFilesDTO.Add(speaker);
            }
            foreach (SystemDTO speaker in changedSystemFilesDTO)
            {
                ViewBag.changedSystemFilesDTO.Add(speaker);
            }
            foreach (SystemDTO speaker in impactSetSystemDTO)
            {
                ViewBag.impactSetSystemDTO.Add(speaker);
            }

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
            ViewBag.DisplayTreeView = "true";
            ViewBag.ViewType = string.IsNullOrEmpty(Request.Query["viewType"].ToString())
                ? Constants.INOUT_VIEW_TYPE
                : Request.Query["viewType"].ToString();
            ViewBag.WWWRoot = Configuration.WWWRoot;
            ViewBag.RootSysId = Request.Query["rootSysId"];

            return View();
        }



        public static void GetFKProjectFileIds(ExcelPackage comparePackage, List<String> deletedLineFiles, List<String> addedLineFiles, List<String> deletedSystemFiles, List<String> addedSystemFiles)
        {
            ExcelWorksheet addedSys = comparePackage.Workbook.Worksheets["Added system"];
            ExcelWorksheet deletedSys = comparePackage.Workbook.Worksheets["Deleted system"];
            ExcelWorksheet addedLine = comparePackage.Workbook.Worksheets["Added line"];
            ExcelWorksheet deletedLine = comparePackage.Workbook.Worksheets["Deleted line"];

            for (int row = 3; row <= getWorksheetMaxRow(addedSys); row++)
            {
                addedSystemFiles.Add(addedSys.Cells[row, 6].Text);
            }

            // Process data for "Deleted system" worksheet
            for (int row = 3; row <= getWorksheetMaxRow(deletedSys); row++)
            {
                deletedSystemFiles.Add(deletedSys.Cells[row, 6].Text);
            }

            // Process data for "Added Line" worksheet
            for (int row = 3; row <= getWorksheetMaxRow(addedLine); row++)
            {
                addedLineFiles.Add(addedLine.Cells[row, 3].Text);
            }

            // Process data for "Deleted Line" worksheet
            for (int row = 3; row <= getWorksheetMaxRow(deletedLine); row++)
            {
                deletedLineFiles.Add(deletedLine.Cells[row, 3].Text);
            }
        }

        [Route("Analysis/Index/{project1Id}/{project2Id}/CompareTree")]
        public ActionResult CompareTree(TableView prevModel, long project1Id, long project2Id)
        {
            ViewBag.deletedLineFilesDTO = new List<LineDTO>();
            ViewBag.addedLineFilesDTO = new List<LineDTO>();
            ViewBag.deletedSystemFilesDTO = new List<SystemDTO>();
            ViewBag.addedSystemFilesDTO = new List<SystemDTO>();
            ViewBag.changedSystemFilesDTO = new List<SystemDTO>();
            ViewBag.addedFilesDTO = new List<string>();
            ViewBag.treeImpactSet = new List<long>();
            ViewBag.digDepthValue = curDigDepthValue;

            foreach (LineDTO speaker in deletedLineFilesDTO)
            {
                ViewBag.deletedLineFilesDTO.Add(speaker);
            }
            foreach (LineDTO speaker in addedLineFilesDTO)
            {
                ViewBag.addedLineFilesDTO.Add(speaker);
            }

            foreach (SystemDTO speaker in deletedSystemFilesDTO)
            {
                ViewBag.deletedSystemFilesDTO.Add(speaker);
            }

            foreach (SystemDTO speaker in addedSystemFilesDTO)
            {
                ViewBag.addedSystemFilesDTO.Add(speaker);
            }
            foreach (SystemDTO speaker in changedSystemFilesDTO)
            {
                ViewBag.changedSystemFilesDTO.Add(speaker);
            }
            foreach (string speaker in addedFilesDTO)
            {
                ViewBag.addedFilesDTO.Add(speaker);
            }
            foreach (long speaker in treeImpactSet)
            {
                ViewBag.treeImpactSet.Add(speaker);
            }


            var viewModel = new TableView()
            {
                ItemPerPage = Convert.ToInt32(Request.Query["show"].ToString() == "" ? 10 : Request.Query["show"].ToString()),
                PageSizes = new List<int>() { 10, 20, 50, 100 },
                CurrentPage = Convert.ToInt32(Request.Query["page"].ToString() == "" ? 1 : Request.Query["page"].ToString()),
                SearchContent = prevModel.SearchContent ?? "",
                ColumnProps = new List<ColumnProp>()
                {
                    new ColumnProp("Name", "Project name", 35),
                },
                LinkAction = "Index",
                LinkController = "Analysis",
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
                });
            }

            Loggers.SVP.Info($"List of {viewModel.ItemPerPage} at page {viewModel.CurrentPage}");


            Loggers.SVP.Info("show a project file");

            ProjectDTO project = projectDAO.ReadProjectById(project1Id);
            ViewBag.ProjectName = project.Name;
            ViewBag.ProjectId = project.Id;
            ViewBag.NewVersionName = newVersionName;
            ViewBag.OldVersionName = oldVersionName;

            ViewBag.FileName = project.Name;
            ViewBag.FileId = 0;
            ViewBag.SystemLevel = "Root";
            ViewBag.SwapView = Request.Query["swap"].ToString();
            ViewBag.FullNet = Request.Query["fullNet"].ToString();
            ViewBag.DisplayParents = Request.Query["displayParents"].ToString() ?? "true";
            ViewBag.DisplayEquals = Request.Query["displayEquals"].ToString() ?? "true";
            ViewBag.DisplayChildren = Request.Query["displayChildren"].ToString() ?? "false";
            ViewBag.DisplaySubChildren = Request.Query["displaySubChildren"].ToString() ?? "";
            ViewBag.DisplayChildLibraries = Request.Query["displayChildLibraries"].ToString() ?? "true";
            ViewBag.DisplayTreeView = "true";
            ViewBag.ViewType = string.IsNullOrEmpty(Request.Query["viewType"].ToString())
                ? Constants.INOUT_VIEW_TYPE
                : Request.Query["viewType"].ToString();
            ViewBag.WWWRoot = Configuration.WWWRoot;
            ViewBag.RootSysId = Request.Query["rootSysId"];

            return View(viewModel);
        }

        public ActionResult CompareProjects(int newVersionId, int oldVersionId)
        {
            // Generate Excel file 
            string connectionString = Entities.Configuration.ConnectionString;

            var id1 = 0;
            var id2 = 0;
            string direct = "";

            newVersionName = Request.Form["newVersionName"];
            oldVersionName = Request.Form["oldVersionName"];

            // Empty the previous comparison
            deletedLineFilesDTO = new List<LineDTO>();
            addedLineFilesDTO = new List<LineDTO>();
            deletedSystemFilesDTO = new List<SystemDTO>();
            addedSystemFilesDTO = new List<SystemDTO>();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                id1 = newVersionId;
                id2 = oldVersionId;

                Dictionary<string, List<string>> tables = GetTableNames(connection);

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (ExcelPackage excelPackage = new ExcelPackage())
                {
                    foreach (KeyValuePair<string, List<string>> table in tables)
                    {
                        string tableName = table.Key;
                        List<string> columns = table.Value;

                        List<int> projectFileIds1 = GetProjectFileIds(connection, id1);

                        List<string> projectData1 = GetProjectData(connection, tableName, projectFileIds1, id1);

                        if (projectData1.Count > 0)
                        {
                            AddDifferencesToExcelSheet(excelPackage, tableName, columns, projectData1, 1);
                        }

                        List<int> projectFileIds2 = GetProjectFileIds(connection, id2);

                        List<string> projectData2 = GetProjectData(connection, tableName, projectFileIds2, id2);

                        if (projectData2.Count > 0)
                        {
                            AddDifferencesToExcelSheet(excelPackage, tableName, columns, projectData2, 2);
                        }
                    }

                    if (!Directory.Exists("UploadedProjects"))
                    {
                        Directory.CreateDirectory("UploadedProjects");
                    }

                    FileInfo excelFile = new FileInfo("UploadedProjects/ProjectData.xlsx");

                    excelPackage.SaveAs(excelFile);
                }

                string filePath = "UploadedProjects/ProjectData.xlsx";
                string outputFilePath = "UploadedProjects/changes.xlsx";


                FileInfo fileInfo = new FileInfo(filePath);

                using (ExcelPackage dataPackage = new ExcelPackage(fileInfo))
                using (ExcelPackage comparePackage = new ExcelPackage())
                {
                    foreach (KeyValuePair<string, List<string>> table in tables)
                    {
                        string tableName = table.Key;
                        List<string> columns = table.Value;

                        CompareExcelSheets(tableName, columns, dataPackage, comparePackage);
                    }

                    GetDTO(dataPackage, comparePackage, deletedLineFilesDTO, addedLineFilesDTO,
                        deletedSystemFilesDTO, addedSystemFilesDTO, changedSystemFilesDTO);

                    HashSet<long> idChanged = changedSystemFilesDTO.Select(x => x.FK_ProjectFileId).ToHashSet();
                    addedSystemFilesDTO.ForEach(x => idChanged.Add(x.FK_ProjectFileId));
                    deletedSystemFilesDTO.ForEach(x => idChanged.Add(x.FK_NewVersionProjectFileID));
                    addedFilesDTO.ForEach(x => idChanged.Add(int.Parse(x)));

                    idChanged.Remove(-1);

                    // Default dig depth value when first compared
                    curDigDepthValue = 1;

                    // Compute the impact set and store it as 'treeImpactSet' (global) to be pushed into a ViewBag
                    // and later rendered in the CompareTree.cshtml view.
                    treeImpactSet = ImpactSetComputation(id1, idChanged.ToList(), curDigDepthValue);
                    Console.WriteLine(id1);
                    for (int i = 0; i < treeImpactSet.Count; i++)
                    {
                        Console.WriteLine(treeImpactSet[i]);
                    }

                    // Generate a list of impactSetProjectFileDTO objects based on ImpactSet IDs
                    // Caution: At this point, I just iterate through new version data.
                    impactSetProjectFileDTO = new List<ProjectFileDTO>();
                    ExcelWorksheet newVerPrjWorksheet = dataPackage.Workbook.Worksheets["projectfile 1"];
                    for (int i = 0; i < treeImpactSet.Count; i++)
                    {
                        for (int row = 3; row <= getWorksheetMaxRow(newVerPrjWorksheet); row++)
                        {
                            long Id = long.TryParse(newVerPrjWorksheet.Cells[row, 1].Text, out var id) ? id : 0;
                            if (Id == treeImpactSet[i])
                            {
                                ProjectFileDTO foundData = new ProjectFileDTO()
                                {
                                    Id = long.TryParse(newVerPrjWorksheet.Cells[row, 1].Text, out var blockId) ? blockId : 0,
                                    Name = newVerPrjWorksheet.Cells[row, 2].Text,
                                    Path = newVerPrjWorksheet.Cells[row, 3].Text,
                                    FK_ProjectId = long.TryParse(newVerPrjWorksheet.Cells[row, 4].Text, out var fkPrjId) ? fkPrjId : 0,
                                    Description = newVerPrjWorksheet.Cells[row, 5].Text,
                                    MatlabVersion = newVerPrjWorksheet.Cells[row, 6].Text,
                                    SystemLevel = newVerPrjWorksheet.Cells[row, 7].Text,
                                    Status = bool.TryParse(newVerPrjWorksheet.Cells[row, 8].Text, out var status) && status,
                                    ErrorMsg = newVerPrjWorksheet.Cells[row, 9].Text,
                                    LevelVariant = newVerPrjWorksheet.Cells[row, 10].Text
                                };
                                impactSetProjectFileDTO.Add(foundData);
                            }
                        }
                    }

                    // Generate a list of impactSetSystemDTO objects based on ImpactSet IDs
                    // Caution: At this point, I just iterate through new version data.
                    ExcelWorksheet newVerSystemWorksheet = dataPackage.Workbook.Worksheets["system 1"];
                    impactSetSystemDTO = new List<SystemDTO>();
                    for (int i = 0; i < treeImpactSet.Count; i++)
                    {
                        for (int row = 3; row <= getWorksheetMaxRow(newVerSystemWorksheet); row++)
                        {
                            long FK_ProjectFileId = long.TryParse(newVerSystemWorksheet.Cells[row, 6].Text, out var projectFileId) ? projectFileId : 0;
                            if (FK_ProjectFileId == treeImpactSet[i])
                            {
                                SystemDTO system = new SystemDTO()
                                {
                                    Id = long.TryParse(newVerSystemWorksheet.Cells[row, 1].Text, out var id) ? id : 0, // Default value 0 if cell 1 is null or not a valid long
                                    BlockType = newVerSystemWorksheet.Cells[row, 2].Text,
                                    Name = newVerSystemWorksheet.Cells[row, 3].Text,
                                    SID = newVerSystemWorksheet.Cells[row, 4].Text,
                                    FK_ParentSystemId = long.TryParse(newVerSystemWorksheet.Cells[row, 5].Text, out var parentSysId) ? parentSysId : 0, // Default value 0 if cell 5 is null or not a valid long
                                    FK_ProjectFileId = long.TryParse(newVerSystemWorksheet.Cells[row, 6].Text, out var projectFileId1) ? projectFileId1 : 0, // Default value 0 if cell 6 is null or not a valid long 
                                    Properties = newVerSystemWorksheet.Cells[row, 7].Text,
                                    SourceBlock = newVerSystemWorksheet.Cells[row, 8].Text,
                                    SourceFile = newVerSystemWorksheet.Cells[row, 9].Text,
                                    GotoTag = newVerSystemWorksheet.Cells[row, 10].Text,
                                    ConnectedRefSrcFile = newVerSystemWorksheet.Cells[row, 11].Text,
                                    FK_FakeProjectFileId = long.TryParse(newVerSystemWorksheet.Cells[row, 12].Text, out var fakeProjectFileId) ? fakeProjectFileId : 0 // Default value 0 if cell 12 is null or not a valid long
                                };
                                impactSetSystemDTO.Add(system);
                            }
                        }
                    }
                    Console.WriteLine("impactSetSystemDTOcount" + impactSetSystemDTO.Count);
                    foreach (var impacted in impactSetSystemDTO)
                    {
                        Console.WriteLine(impacted.Name);
                    }

                    comparePackage.SaveAs(new FileInfo(outputFilePath));
                }
                direct = "CompareTree";
            }

            TempData["AlertMessage"] = "Comparison successful!";

            return RedirectToAction(direct, new { project1Id = id1, project2Id = id2 });
        }

        public static List<Tuple<long, long>> GetProjectIdMap(ExcelPackage dataPackage)
        {
            ExcelWorksheet newVersionSheet = dataPackage.Workbook.Worksheets["projectfile 1"];
            ExcelWorksheet oldVersionSheet = dataPackage.Workbook.Worksheets["projectfile 2"];

            if (newVersionSheet == null || oldVersionSheet == null)
            {
                Console.WriteLine("Something null");
                return new List<Tuple<long, long>>();
            }

            List<Tuple<long, string>> newProjectFileIds = new List<Tuple<long, string>>();
            List<Tuple<long, string>> oldProjectFileIds = new List<Tuple<long, string>>();

            // Fetch data from each version:
            for (int row = 3; row <= getWorksheetMaxRow(newVersionSheet); row++)
            {
                long Id = long.TryParse(newVersionSheet.Cells[row, 1].Text, out var id) ? id : 0;
                string path = newVersionSheet.Cells[row, 3].Text;
                newProjectFileIds.Add(new Tuple<long, string>(Id, path));
            }
            for (int row = 3; row <= getWorksheetMaxRow(oldVersionSheet); row++)
            {
                long Id = long.TryParse(oldVersionSheet.Cells[row, 1].Text, out var id) ? id : 0;
                string path = oldVersionSheet.Cells[row, 3].Text;
                oldProjectFileIds.Add(new Tuple<long, string>(Id, path));
            }

            List<Tuple<long, long>> idMap = new List<Tuple<long, long>>();

            foreach (var newItem in newProjectFileIds)
            {
                foreach (var oldItem in oldProjectFileIds)
                {
                    if (newItem.Item2 == oldItem.Item2)
                    {
                        idMap.Add(new Tuple<long, long>(newItem.Item1, oldItem.Item1));
                    }
                }
            }
            return idMap;
        }

        public static long GetNewIdOfOldSystem(List<Tuple<long, long>> idMap, long oldSystemFileId)
        {
            Tuple<long, long> matchingTuple = idMap.FirstOrDefault(tuple => tuple.Item2 == oldSystemFileId);

            if (matchingTuple != null)
            {
                return matchingTuple.Item1;
            }
            return -1;
        }

        public static long CheckIfIsNewFile(List<Tuple<long, long>> idMap, long oldSystemFileId)
        {
            Tuple<long, long> matchingTuple = idMap.FirstOrDefault(tuple => tuple.Item1 == oldSystemFileId);

            if (matchingTuple != null)
            {
                return matchingTuple.Item2;
            }
            return -1;
        }

        public static void GetDTO(ExcelPackage dataPackage, ExcelPackage comparePackage,
            List<LineDTO> deletedLineFilesDTO, List<LineDTO> addedLineFilesDTO,
            List<SystemDTO> deletedSystemFilesDTO, List<SystemDTO> addedSystemFilesDTO,
            List<SystemDTO> changedSystemFilesDTO)
        {
            ExcelWorksheet addedSys = comparePackage.Workbook.Worksheets["Added system"];
            ExcelWorksheet deletedSys = comparePackage.Workbook.Worksheets["Deleted system"];
            ExcelWorksheet changedSys = comparePackage.Workbook.Worksheets["After changed system"];

            ExcelWorksheet addedLine = comparePackage.Workbook.Worksheets["Added line"];
            ExcelWorksheet deletedLine = comparePackage.Workbook.Worksheets["Deleted line"];

            deletedLineFilesDTO.Clear();
            addedLineFilesDTO.Clear();
            deletedSystemFilesDTO.Clear();
            addedSystemFilesDTO.Clear();
            changedSystemFilesDTO.Clear();

            List<Tuple<long, long>> idMap = GetProjectIdMap(dataPackage);

            //added system
            for (int row = 2; row <= getWorksheetMaxRow(addedSys); row++)
            {
                SystemDTO system = new SystemDTO()
                {
                    Id = long.TryParse(addedSys.Cells[row, 1].Text, out var id) ? id : 0, // Default value 0 if cell 1 is null or not a valid long
                    BlockType = addedSys.Cells[row, 2].Text,
                    Name = addedSys.Cells[row, 3].Text,
                    SID = addedSys.Cells[row, 4].Text,
                    FK_ParentSystemId = long.TryParse(addedSys.Cells[row, 5].Text, out var parentSysId) ? parentSysId : 0, // Default value 0 if cell 5 is null or not a valid long
                    FK_ProjectFileId = long.TryParse(addedSys.Cells[row, 6].Text, out var projectFileId) ? projectFileId : 0, // Default value 0 if cell 6 is null or not a valid long 
                    Properties = addedSys.Cells[row, 7].Text,
                    SourceBlock = addedSys.Cells[row, 8].Text,
                    SourceFile = addedSys.Cells[row, 9].Text,
                    GotoTag = addedSys.Cells[row, 10].Text,
                    ConnectedRefSrcFile = addedSys.Cells[row, 11].Text,
                    FK_FakeProjectFileId = long.TryParse(addedSys.Cells[row, 12].Text, out var fakeProjectFileId) ? fakeProjectFileId : 0 // Default value 0 if cell 12 is null or not a valid long
                };
                //Check If this a new file added
                if (CheckIfIsNewFile(idMap, long.TryParse(addedSys.Cells[row, 6].Text, out projectFileId) ? projectFileId : 0) == -1)
                {
                    addedFilesDTO.Add(system.FK_ProjectFileId.ToString());
                }
                addedSystemFilesDTO.Add(system);
            }

            // Process data for "Deleted system" worksheet
            for (int row = 2; row <= getWorksheetMaxRow(deletedSys); row++)
            {
                SystemDTO system = new SystemDTO()
                {
                    Id = long.TryParse(deletedSys.Cells[row, 1].Text, out var id) ? id : 0,
                    BlockType = deletedSys.Cells[row, 2].Text,
                    Name = deletedSys.Cells[row, 3].Text,
                    SID = deletedSys.Cells[row, 4].Text,
                    FK_ParentSystemId = long.TryParse(deletedSys.Cells[row, 5].Text, out var parentSysId) ? parentSysId : 0,
                    FK_ProjectFileId = long.TryParse(deletedSys.Cells[row, 6].Text, out var projectFileId) ? projectFileId : 0,
                    FK_NewVersionProjectFileID = GetNewIdOfOldSystem(idMap, long.TryParse(deletedSys.Cells[row, 6].Text, out projectFileId) ? projectFileId : 0),
                    Properties = deletedSys.Cells[row, 7].Text,
                    SourceBlock = deletedSys.Cells[row, 8].Text,
                    SourceFile = deletedSys.Cells[row, 9].Text,
                    GotoTag = deletedSys.Cells[row, 10].Text,
                    ConnectedRefSrcFile = deletedSys.Cells[row, 11].Text,
                    FK_FakeProjectFileId = long.TryParse(deletedSys.Cells[row, 12].Text, out var fakeProjectFileId) ? fakeProjectFileId : 0
                };
                deletedSystemFilesDTO.Add(system);
            }

            // Process data for "After change system" worksheet
            for (int row = 2; row <= getWorksheetMaxRow(changedSys); row++)
            {
                SystemDTO system = new SystemDTO()
                {
                    Id = long.TryParse(changedSys.Cells[row, 1].Text, out var id) ? id : 0,
                    BlockType = changedSys.Cells[row, 2].Text,
                    Name = changedSys.Cells[row, 3].Text,
                    SID = changedSys.Cells[row, 4].Text,
                    FK_ParentSystemId = long.TryParse(changedSys.Cells[row, 5].Text, out var parentSysId) ? parentSysId : 0,
                    FK_ProjectFileId = long.TryParse(changedSys.Cells[row, 6].Text, out var projectFileId) ? projectFileId : 0,
                    Properties = changedSys.Cells[row, 7].Text,
                    SourceBlock = changedSys.Cells[row, 8].Text,
                    SourceFile = changedSys.Cells[row, 9].Text,
                    GotoTag = changedSys.Cells[row, 10].Text,
                    ConnectedRefSrcFile = changedSys.Cells[row, 11].Text,
                    FK_FakeProjectFileId = long.TryParse(changedSys.Cells[row, 12].Text, out var fakeProjectFileId) ? fakeProjectFileId : 0
                };

                changedSystemFilesDTO.Add(system);
            }
            //Console.WriteLine(addedSystemFilesDTO.Count);

            // Process data for "Added Line" worksheet
            for (int row = 2; row <= getWorksheetMaxRow(addedLine); row++)
            {
                LineDTO line = new LineDTO()
                {
                    Id = long.TryParse(addedLine.Cells[row, 1].Text, out var id) ? id : 0,
                    FK_SystemId = long.TryParse(addedLine.Cells[row, 2].Text, out var systemId) ? systemId : 0,
                    FK_ProjectFileId = long.TryParse(addedLine.Cells[row, 3].Text, out var projectFileId) ? projectFileId : 0,
                    Properties = addedLine.Cells[row, 4].Text
                };
                addedLineFilesDTO.Add(line);
            }

            // Process data for "Deleted Line" worksheet
            for (int row = 2; row <= getWorksheetMaxRow(deletedLine); row++)
            {
                LineDTO line = new LineDTO()
                {
                    Id = long.TryParse(deletedLine.Cells[row, 1].Text, out var id) ? id : 0,
                    FK_SystemId = long.TryParse(deletedLine.Cells[row, 2].Text, out var systemId) ? systemId : 0,
                    FK_ProjectFileId = long.TryParse(deletedLine.Cells[row, 3].Text, out var projectFileId) ? projectFileId : 0,
                    Properties = deletedLine.Cells[row, 4].Text
                };

                deletedLineFilesDTO.Add(line);
            }
        }

        public static int getWorksheetMaxRow(ExcelWorksheet worksheet)
        {
            return worksheet.Dimension != null ? worksheet.Dimension.Rows : 0;

        }

        public static void AddColumnNames(ExcelWorksheet worksheet, List<string> columns, int rowIndex)
        {
            int colIndex = 1;

            // Add column names to worksheet
            foreach (string column in columns)
            {
                worksheet.Cells[rowIndex, colIndex].Value = column;
                worksheet.Cells[rowIndex, colIndex].Style.Font.Bold = true;
                colIndex++;
            }
            worksheet.Cells.AutoFitColumns();
        }

        public static void CompareExcelSheets(string tableName, List<string> columns, ExcelPackage dataPackage, ExcelPackage comparePackage)
        {
            if (tableName == "line" || tableName == "system")
            {
                ExcelWorksheet newVersion = dataPackage.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == $"{tableName} 1");
                ExcelWorksheet oldVersion = dataPackage.Workbook.Worksheets.FirstOrDefault(ws => ws.Name == $"{tableName} 2");

                if (newVersion == null || oldVersion == null)
                {
                    return;
                }

                ExcelWorksheet addedWorksheet = comparePackage.Workbook.Worksheets.Add($"Added {tableName}");
                AddColumnNames(addedWorksheet, columns, 1);


                ExcelWorksheet beforeChangedWorksheet = comparePackage.Workbook.Worksheets.Add($"Before changed {tableName}");
                AddColumnNames(beforeChangedWorksheet, columns, 1);

                ExcelWorksheet afterChangedWorksheet = comparePackage.Workbook.Worksheets.Add($"After changed {tableName}");
                AddColumnNames(afterChangedWorksheet, columns, 1);


                ExcelWorksheet deletedWorksheet = comparePackage.Workbook.Worksheets.Add($"Deleted {tableName}");
                AddColumnNames(deletedWorksheet, columns, 1);

                int rowCount1 = newVersion.Dimension.Rows;
                int rowCount2 = oldVersion.Dimension.Rows;

                int[] analyzingCols = GetAnalysingColumns(tableName);
                if (analyzingCols.Length == 0)
                {
                    return;
                }
                // Ways: Kiem tra xem row trong new version co trong old version hay khong
                // Neu khong, cho row do vao addedWorksheet
                for (int row1 = 3; row1 <= rowCount1; row1++)
                {
                    bool rowExistedInOldVersion = false;
                    for (int row2 = 3; row2 <= rowCount1; row2++)
                    {
                        bool twoRowAreEqual = true;
                        foreach (var col in analyzingCols)
                        {
                            if (AreTwoRowUnequal(newVersion.Cells[row1, col].Text, oldVersion.Cells[row2, col].Text))
                            {
                                twoRowAreEqual = false;
                                break;
                            }
                        }
                        if (twoRowAreEqual)
                        {
                            rowExistedInOldVersion = true;
                            break;
                        }
                    }
                    if (!rowExistedInOldVersion)
                    {
                        CopyRow(newVersion, row1, addedWorksheet);
                    }
                }
                for (int row2 = 3; row2 < rowCount2; row2++)
                {
                    bool rowExistedInNewVersion = false;
                    for (int row1 = 3; row1 < rowCount1; row1++)
                    {
                        bool twoRowAreEqual = true;
                        foreach (var col in analyzingCols)
                        {
                            if (AreTwoRowUnequal(newVersion.Cells[row1, col].Text, oldVersion.Cells[row2, col].Text))
                            {
                                twoRowAreEqual = false;
                                break;
                            }
                        }
                        if (twoRowAreEqual)
                        {
                            rowExistedInNewVersion = true;
                            break;
                        }
                    }
                    if (!rowExistedInNewVersion)
                    {
                        CopyRow(oldVersion, row2, deletedWorksheet);
                    }
                }

                /* Kiem tra xem phan tu co phai la deleted xong roi added khong
                 * Neu dung, phan added them vao after changed &&
                 * phan deleted them vao before changed
                 */

                int addedRowCount = addedWorksheet.Dimension.Rows;
                int deletedRowCount = deletedWorksheet.Dimension.Rows;

                if (tableName == "system")
                {
                    List<int> addToChangeRow = new List<int>();
                    List<int> deleteToChangeRow = new List<int>();

                    for (int addRow = 2; addRow <= addedRowCount; addRow++)
                    {
                        for (int deleteRow = 2; deleteRow <= deletedRowCount; deleteRow++)
                        {
                            bool twoRowAreEqual = true;
                            foreach (var col in new[] { 3, 4 })
                            {
                                if (AreTwoRowUnequal(addedWorksheet.Cells[addRow, col].Text,
                                    deletedWorksheet.Cells[deleteRow, col].Text))
                                {
                                    twoRowAreEqual = false;
                                    break;
                                }
                            }

                            // Also check the empty block? - May need to solve later
                            if (twoRowAreEqual && addedWorksheet.Cells[addRow, 3].Text != "")
                            {
                                CopyRow(addedWorksheet, addRow, afterChangedWorksheet);
                                CopyRow(deletedWorksheet, deleteRow, beforeChangedWorksheet);

                                addToChangeRow.Add(addRow);
                                deleteToChangeRow.Add(deleteRow);
                                //Console.Write(deleteRow + " ");
                            }
                        }
                    }

                    foreach (int rowToDelete in addToChangeRow.OrderByDescending(i => i))
                    {
                        //Console.Write(rowToDelete + " ");
                        DeleteRow(addedWorksheet, rowToDelete);
                    }

                    foreach (int rowToDelete in deleteToChangeRow.OrderByDescending(i => i))
                    {
                        //Console.Write(rowToDelete + " ");
                        DeleteRow(deletedWorksheet, rowToDelete);
                    }
                }

                addedWorksheet.Cells.AutoFitColumns();
                beforeChangedWorksheet.Cells.AutoFitColumns();
                afterChangedWorksheet.Cells.AutoFitColumns();
                deletedWorksheet.Cells.AutoFitColumns();
            }
        }

        public static bool IsJsonLike(string jsonString)
        {
            if (!string.IsNullOrEmpty(jsonString))
            {
                return (jsonString[0] == '{' && jsonString[jsonString.Length - 1] == '}');
            }
            else
            {
                return false;
            }
        }

        public static bool AreTwoRowUnequal(String firstRow, String secondRow)
        {
            // Check if both strings are JSON-like
            if (IsJsonLike(firstRow) && IsJsonLike(secondRow))
            {

                // Parse the JSON-like strings into JTokens
                JToken firstToken = JToken.Parse(firstRow);
                JToken secondToken = JToken.Parse(secondRow);

                // Compare the JSON objects
                return !JToken.DeepEquals(firstToken, secondToken);
            }
            else
            {
                if (firstRow != secondRow)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static int[] GetAnalysingColumns(string tableName)
        {
            if (tableName == "instancedata" || tableName == "line" || tableName == "list")
            {
                return new int[] { 4 };
            }
            if (tableName == "projectfile")
            {
                return new int[] { 2, 3 };
            }
            if (tableName == "system")
            {
                return new int[] { 2, 3, 4, 7 };
            }

            // Return an empty array or handle other cases as needed
            return new int[0];
        }

        static void DeleteRow(ExcelWorksheet worksheet, int rowToDelete)
        {
            int rowCount = worksheet.Dimension != null ? worksheet.Dimension.Rows : 0;

            if (rowToDelete >= 1)
            {
                worksheet.DeleteRow(rowToDelete, 1);
            }
            else
            {
                Console.WriteLine("Invalid row index to delete: " + rowToDelete);
            }
        }

        static void CopyRow(ExcelWorksheet sourceWorksheet, int sourceRow, ExcelWorksheet destinationWorksheet)
        {

            int sourceColumnCount = sourceWorksheet.Dimension != null ? sourceWorksheet.Dimension.Columns : 0;

            int destinationCurRow = destinationWorksheet.Dimension != null ? destinationWorksheet.Dimension.Rows : 0;

            for (int col = 1; col <= sourceColumnCount; col++)
            {
                destinationWorksheet.Cells[destinationCurRow + 1, col].Value = sourceWorksheet.Cells[sourceRow, col].Value;
            }
        }


        // Get table names and their col counts

        static int GetProjectId(MySqlConnection connection, string projectName)
        {
            int projectId = 0;

            string query = $"SELECT Id FROM project WHERE Name = @projectName";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@projectName", projectName);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            int columnValue = reader.IsDBNull(0) ? -1 : reader.GetInt32(0);
                            projectId = columnValue;
                        }
                    }
                }
            }
            return projectId;
        }

        static Dictionary<string, List<string>> GetTableNames(MySqlConnection connection)
        {
            Dictionary<string, List<string>> tableColumns = new Dictionary<string, List<string>>();
            DataTable schemaTable = connection.GetSchema("Tables");

            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row["TABLE_NAME"];
                List<string> columnNames = new List<string>();

                using (DataTable columnsSchemaTable = connection.GetSchema("Columns", new string[] { null, null, tableName }))
                {
                    foreach (DataRow columnRow in columnsSchemaTable.Rows)
                    {
                        string columnName = (string)columnRow["COLUMN_NAME"];
                        columnNames.Add(columnName);
                    }
                }

                tableColumns.Add(tableName, columnNames);
            }

            return tableColumns;
        }

        static List<int> GetProjectFileIds(MySqlConnection connection, int id)
        {
            List<int> projectFileIds = new List<int>();

            string query = $"SELECT Id FROM projectfile WHERE FK_ProjectId = @ID";
            using (MySqlCommand command = new MySqlCommand(query, connection))
            {
                command.Parameters.AddWithValue("@ID", id);

                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            int columnValue = reader.IsDBNull(0) ? -1 : reader.GetInt32(0);
                            projectFileIds.Add(columnValue);
                        }
                    }
                }
            }
            return projectFileIds;
        }

        static List<String> GetProjectData(MySqlConnection connection, string tableName, List<int> projectFileIds, int projectId)
        {
            List<string> projectData = new List<string>
            { };

            // Extract data from project table
            if (tableName == "project")
            {
                string query = $"SELECT * FROM {tableName} WHERE Id = @PROJECTID";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PROJECTID", projectId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnValue = reader.IsDBNull(i) ? "NULL" : reader.GetString(i);
                                projectData.Add(columnValue);
                            }
                        }
                    }
                }
            }

            if (tableName == "projectfile")
            {
                string query = $"SELECT * FROM {tableName} WHERE FK_ProjectId = @PROJECTID";
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@PROJECTID", projectId);

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                string columnValue = reader.IsDBNull(i) ? "NULL" : reader.GetString(i);
                                projectData.Add(columnValue);
                            }
                        }
                    }
                }
            }

            if (tableName == "branch" || tableName == "instancedata" || tableName == "line" || tableName == "list" || tableName == "system")
            {
                foreach (int projectFileId in projectFileIds)
                {
                    string query = $"SELECT * FROM `{tableName}` WHERE FK_ProjectFileId = @PROJECTFILEID";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PROJECTFILEID", projectFileId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnValue = reader.IsDBNull(i) ? "NULL" : reader.GetString(i);
                                    projectData.Add(columnValue);
                                }
                            }
                        }
                    }
                }

            }

            if (tableName == "filesrelationship")
            {
                foreach (int projectFileId in projectFileIds)
                {
                    string query = $"SELECT * FROM {tableName} WHERE FK_ProjectFileId1 = @PROJECTFILEID OR FK_ProjectFileId2 = @PROJECTFILEID";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@PROJECTFILEID", projectFileId);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                for (int i = 0; i < reader.FieldCount; i++)
                                {
                                    string columnValue = reader.IsDBNull(i) ? "NULL" : reader.GetString(i);
                                    projectData.Add(columnValue);
                                }
                            }
                        }
                    }
                }
            }

            return projectData;
        }

        static void AddDifferencesToExcelSheet(ExcelPackage excelPackage, string tableName, List<string> columns, List<string> differences, int projectNum)
        {

            ExcelWorksheet worksheet = excelPackage.Workbook.Worksheets.FirstOrDefault(sheet => sheet.Name == tableName);

            if (worksheet == null)
            {
                if (tableName == "project" || tableName == "projectfile" || tableName == "branch" || tableName == "filesrelationship" || tableName == "instancedata" || tableName == "line" || tableName == "list" || tableName == "system")
                {
                    worksheet = excelPackage.Workbook.Worksheets.Add($"{tableName} {projectNum}");

                    int rowIndex = 1;
                    int colIndex = 1;

                    // Add header
                    worksheet.Cells[1, 1].Value = $"Project {projectNum} ";
                    worksheet.Cells[1, 1].Style.Font.Bold = true;
                    rowIndex++;

                    // Add differences
                    foreach (string column in columns)
                    {
                        worksheet.Cells[rowIndex, colIndex].Value = column;
                        worksheet.Cells[rowIndex, colIndex].Style.Font.Bold = true;
                        colIndex++;
                    }

                    rowIndex++;
                    colIndex = 1;

                    foreach (string difference in differences)
                    {
                        worksheet.Cells[rowIndex, colIndex].Value = difference;
                        if (colIndex < columns.Count)
                        {
                            colIndex++;
                        }
                        else
                        {
                            rowIndex++;
                            colIndex = 1;
                        }
                    }
                }
                worksheet.Cells.AutoFitColumns();
            }
        }

        private bool HasProjectExisted(string projectName)
        {
            return projectDAO.ReadProjectByName(projectName.Trim()) != null;
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