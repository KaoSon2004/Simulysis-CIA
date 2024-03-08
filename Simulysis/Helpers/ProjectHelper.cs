using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Common;
using Entities.Logging;
using Entities.Types;
using Entities.DTO;
using System.Threading;
using Entities.EntityDataTable;
using Simulysis.Helpers.DataSaver.EntityDataTable;
using System.Data;
using System.Text.Json;
using Simulysis.Helpers.CSVBulkLoader;

namespace Simulysis.Helpers
{
    public class ProjectHelper
    {
        private static IProjectDAO projectDAO = DAOFactory.GetDAO("IProjectDAO") as IProjectDAO;

        private static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        private static ISystemDAO systemDAO = DAOFactory.GetDAO("ISystemDAO") as ISystemDAO;
        private static ILineDAO lineDAO = DAOFactory.GetDAO("ILineDAO") as ILineDAO;
        private static IListDAO listDAO = DAOFactory.GetDAO("IListDAO") as IListDAO;
        private static IInstanceDataDAO instanceDataDAO = DAOFactory.GetDAO("IInstanceDataDAO") as IInstanceDataDAO;
        private static IPortDAO portDAO = DAOFactory.GetDAO("IPortDAO") as IPortDAO;
        private static IBranchDAO branchDAO = DAOFactory.GetDAO("IBranchDAO") as IBranchDAO;

        private static IFilesRelationshipDAO fileRelationshipDAO = DAOFactory.GetDAO("IFilesRelationshipDAO") as IFilesRelationshipDAO;

        private static ICalibrationDAO calibrationDAO = DAOFactory.GetDAO("ICalibrationDAO") as ICalibrationDAO;

        public static bool CheckExtension(string fileName)
        {
            string[] allowedExtensions = {".zip"};

            return allowedExtensions.Contains(Path.GetExtension(fileName).ToLower());
        }

        public static string SaveZipToDisk(string projectPath, string fileName, IFormFile file)
        {
            // nếu có project bị xóa lỗi -> project folder cũ vẫn còn mà lại đặt tên project mới trùng với folder này -> lỗi -> cần clear trước
            Loggers.SVP.Info("SAVE ZIP FILE TO DISK");
            string zipPath = Path.Combine(projectPath, fileName);
            Loggers.SVP.Info("Create project dir");
            if(!Directory.Exists(projectPath))
            {
               
                Directory.CreateDirectory(projectPath);

            }
            else
            {
                // nếu đã tồn tại -> rác -> clear 
                EmptyDirectory(projectPath);

            }
            Loggers.SVP.Info("Save zip to project dir");

            using (FileStream zipFileDest = File.OpenWrite(zipPath))
            {
                file.CopyTo(zipFileDest);
            }

            return zipPath;
        }

        
        public static void EmptyDirectory(string directoryPath)
        {
            // create a DirectoryInfo object for the directory
            DirectoryInfo directory = new DirectoryInfo(directoryPath);

            // loop through all the files in the directory and delete them
            foreach (FileInfo file in directory.GetFiles())
            {
                file.Delete();
            }

            // loop through all the subdirectories in the directory and delete them
            foreach (DirectoryInfo subdirectory in directory.GetDirectories())
            {
                subdirectory.Delete(true);
            }
        }

        public static string GetProjectPath(string uploadProjectRoot, string projectName)
        {
            return Path.Combine(uploadProjectRoot, projectName.Trim());
        }

        public static void ExtractZip(string zipPath, string projectPath)
        {
            Loggers.SVP.Info("EXTRACT ZIP FILE");
            ZipFile.ExtractToDirectory(zipPath, projectPath);
        }

        public static List<FileContent> ReadAllFiles(long projectId, string projectPath)
        {
            if (projectId <= 0)
            {
                Exception ex = new Exception("Fail to create project!");
                Loggers.SVP.Exception(ex.Message, ex);
                throw ex;
            }

            Loggers.SVP.Info("READ PROJECT FILES");
            Loggers.SVP.Info("Read list of .mdl & .slx file names");
            var filePaths =
                Directory.EnumerateFiles(projectPath, "*.*", SearchOption.AllDirectories)
                    .Where(path =>
                        path.EndsWith(".slx", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".mdl", StringComparison.OrdinalIgnoreCase)
                    );

            Loggers.SVP.Info($"NUMBER OF PROJECT FILES: {filePaths.Count()}");

            List<FileContent> fileContentList = new List<FileContent>(filePaths.Count());

            Parallel.ForEach(
                filePaths,
                new ParallelOptions {MaxDegreeOfParallelism = Configuration.MaxThreadNumber},
                filePath =>
                {
                    try
                    {
                        string fileExt = Path.GetExtension(filePath).ToLower();

                        FileHelper
                            .ReadFile(filePath, fileExt, projectId, projectPath, "MFMdl", "", filePaths, fileContentList);
                    }
                    catch (Exception ex)
                    {
                        Loggers.SVP.Exception(
                            $"Exception while reading file {Path.GetFileName(filePath)}: {ex.Message}", ex
                        );
                    }
                }
            );
            // preprocess file content
            ProjectHelper.ReplaceRefWithRootRef(fileContentList);

            foreach (var fileContent in fileContentList)
            {
                Reader.AddFromGotoConnectedSys(fileContent.Systems, fileContent.Lines);
            }

            // cleanup
            string slxExtractPath = Path.Combine(projectPath, Constants.SLX_EXTRACT_FOLDER);
            if (Directory.Exists(slxExtractPath)) {
                Directory.Delete(slxExtractPath, true);
            }

            return fileContentList;
        }

        public static void ReplaceRefWithRootRef(List<FileContent> fileContents)
        {
            Loggers.SVP.Info("FINDING ROOT OF REFERENCE SYSTEMS");
            //Parallel.ForEach(copiedList,
            //    new ParallelOptions { MaxDegreeOfParallelism = Configuration.MaxThreadNumber },
            //    fileContent =>
            //    {
            //        foreach (var system in fileContent.Systems)
            //        {
            //            if (system.BlockType.Equals(Constants.REF) && !system.SourceFile.Equals(Constants.MATLAB_LIB_REF))
            //            {
            //                try
            //                {
            //                    var refRoot = FindRootOfRefSys(copiedList, system);

            //                    system.SourceFile = refRoot.Item1;
            //                    system.SourceBlock = refRoot.Item2;

            //                }
            //                catch (Exception ex)
            //                {
            //                    Loggers.SVP.Exception(
            //                        $"Exception while finding root reference of block {system.Name} in file {fileContent.FileName}: {ex.Message}", ex
            //                    );
            //                }
            //            }
            //        }
            //    }
            //);

            Loggers.SVP.Info("Running using normal foreach v1.0.7.4");
            foreach (var fileContent in fileContents) {
                foreach (var system in fileContent.Systems)
                {
                    if (!string.IsNullOrEmpty(system.BlockType) && system.BlockType.Equals(Constants.REF) && string.IsNullOrEmpty(system.SourceFile))
                    {
                        Loggers.SVP.Info("============= Reference system without source file ===============\n");
                        Loggers.SVP.Info($"System {system.Name ?? ""}, block type = {system.BlockType ?? ""}, ref = {system.SourceFile ?? ""}/{system.SourceBlock ?? ""}\n");
                        Loggers.SVP.Info(system.Properties ?? "");
                        Loggers.SVP.Info("\n============= End System log =================\n");

                        continue;
                    }

                    if (system.BlockType.Equals(Constants.REF) && !system.SourceFile.Equals(Constants.MATLAB_LIB_REF))
                    {
                        try
                        {
                            var refRoot = FindRootOfRefSys(fileContents, system);

                            system.SourceFile = refRoot.Item1;
                            system.SourceBlock = refRoot.Item2;

                        }
                        catch (Exception ex)
                        {
                            Loggers.SVP.Exception(
                                $"Exception while finding root reference of block {system.Name} in file {fileContent.FileName}: {ex.Message}", ex
                            );
                        }
                    }
                }
            }
        }

        public static List<CalibrationDTO> ReadAllMFiles(string projectPath, long projectId)
        {
            Loggers.SVP.Info("READ PROJECT CONSTANTS");
            Loggers.SVP.Info("Read list of .m file names");

            List<CalibrationDTO> projectCalibrations = new List<CalibrationDTO>((int) InitialCapacity.Other);
            var filePaths = Directory.EnumerateFiles(projectPath, "*.m", SearchOption.AllDirectories);

            Parallel.ForEach(
                filePaths,
                new ParallelOptions {MaxDegreeOfParallelism = Configuration.MaxThreadNumber},
                filePath =>
                {
                    try
                    {
                        List<CalibrationDTO> fileCalibrations = FileHelper.ReadMFile(filePath, projectId);

                        lock (projectCalibrations)
                        {
                            projectCalibrations.AddRange(fileCalibrations);
                        }
                    }
                    catch (Exception ex)
                    {
                        Loggers.SVP.Exception(
                            $"Exception while reading file {Path.GetFileName(filePath)}: {ex.Message}", ex
                        );
                    }
                }
            );

            return projectCalibrations;
        }

        public static List<FilesRelationshipDTO> IdentifyChildParentRelationships(long projectId, List<FileContent> fileContentList)
        {
            List<FilesRelationshipDTO> relationships = new List<FilesRelationshipDTO>();
            Parallel.ForEach(
                fileContentList,
                new ParallelOptions { MaxDegreeOfParallelism = Configuration.MaxThreadNumber },
                fileContent =>
                {
                    var childParentRelationships = FileHelper.GetChildParentRelationships(projectId, fileContent, fileContentList);

                    lock (relationships)
                    {
                        relationships.AddRange(childParentRelationships);
                    }
                }
            );

            return relationships;
        }

        public static List<FilesRelationshipDTO> IdentifyEqualRelationship(
            List<FileContent> fileContentList,
            List<CalibrationDTO> projectCalibrations,
            long projectId
        )
        {
            Loggers.SVP.Info("IDENTIFY FILE RELATIONSHIPS");

            // get all in, out, from, goto, subsystem blocks & blocks that contain calibration for less looping later
            var allBlocks = GetAllBlockForRelationship(fileContentList, projectCalibrations, projectId);
            var subsystemDict = GenericUtils.ToDictOfLists((ConcurrentDictionary<long, ConcurrentBag<SystemDTO>>) allBlocks.subsystemDict);

            /*
             * FROM - GOTO RELATIONSHIPS
             */
            // get all the files from server for later name mapping
            List<ProjectFileDTO> fileList = projectFileDAO.ReadAllFiles(projectId);

            // initialize a list of file relationships
            List<FilesRelationshipDTO> fileRels = new List<FilesRelationshipDTO>((int) InitialCapacity.FileRelationship);

            Loggers.SVP.Info("Identify from-goto relationships");

            // loop through the content in each file
            Parallel.ForEach((ConcurrentBag<FromGotoContainer>) allBlocks.fromGotoContainers,
                new ParallelOptions {MaxDegreeOfParallelism = Configuration.MaxThreadNumber},
                container =>
                {
                    foreach (var gotoPair in container.GotoDict)
                    {
                        if (container.FromDict.TryGetValue(gotoPair.Key, out var matchFroms))
                        {
                            foreach (var gotoBlock in gotoPair.Value)
                            {
                                bool gotoIsRef = gotoBlock.ConnectedRefSys != null && gotoBlock.ConnectedRefSys.BlockType.Equals("Reference");
                                long gotoFileId = gotoIsRef
                                    ? GetRefSysContainingFile(gotoBlock.ConnectedRefSys, fileList)
                                    : gotoBlock.FK_ProjectFileId;

                                var gotoFileLevel = fileContentList.Find(fc => fc.FileId == gotoFileId)?.FileLevel;
                                var gotoParentSys = subsystemDict[gotoBlock.FK_ProjectFileId].Find(system => system.Id == gotoBlock.FK_ParentSystemId - 1);

                                foreach (var fromBlock in matchFroms)
                                {
                                    bool fromIsRef = fromBlock.ConnectedRefSys != null && fromBlock.ConnectedRefSys.BlockType.Equals("Reference");
                                    long fromFileId = fromIsRef
                                        ? GetRefSysContainingFile(fromBlock.ConnectedRefSys, fileList)
                                        : fromBlock.FK_ProjectFileId;

                                    var fromFileLevel = fileContentList.Find(fc => fc.FileId == fromFileId)?.FileLevel;
                                    var fromParentSys = subsystemDict[fromBlock.FK_ProjectFileId].Find(system => system.Id == fromBlock.FK_ParentSystemId - 1);

                                    FilesRelationshipDTO fileRelPrototype = new FilesRelationshipDTO()
                                    {
                                        System1 = gotoParentSys?.Name,
                                        System2 = fromParentSys?.Name,
                                        Count = 1,
                                        UniCount = 1,
                                        Name = gotoPair.Key,
                                        Type = FileRelationship.Equal,
                                        RelationshipType = RelationshipType.From_Go_To
                                    };

                                    if (gotoFileId != -1 && fromFileId != -1 && !string.IsNullOrEmpty(gotoFileLevel) && gotoFileLevel.Equals(fromFileLevel))
                                    {
                                        var fileToFileRela = new FilesRelationshipDTO(fileRelPrototype)
                                        {
                                            FK_ProjectFileId1 = gotoFileId,
                                            FK_ProjectFileId2 = fromFileId
                                        };

                                        lock (fileRels)
                                        {
                                            AddRelationshipToList(
                                                fileRels,
                                                fileToFileRela,
                                                new List<RelationshipType> { RelationshipType.From_Go_To }
                                            );
                                        }
                                    }

                                    if (gotoParentSys != null && fromParentSys != null)
                                    {
                                        // avoid self connection of subsystem
                                        if (gotoParentSys.Id == fromParentSys.Id) continue;

                                        var sysToSysRela = new FilesRelationshipDTO(fileRelPrototype)
                                        {
                                            FK_ProjectFileId1 = gotoParentSys.FK_FakeProjectFileId,
                                            FK_ProjectFileId2 = fromParentSys.FK_FakeProjectFileId
                                        };

                                        lock (fileRels)
                                        {
                                            AddRelationshipToList(
                                                fileRels,
                                                sysToSysRela,
                                                new List<RelationshipType> { RelationshipType.From_Go_To }
                                            );
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            );

            /*
             * INPORT-OUTPORT RELATIONSHIPS
             */

            Loggers.SVP.Info("Identify in-out relationships");

            Parallel.ForEach((ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>) allBlocks.outDict,
                new ParallelOptions {MaxDegreeOfParallelism = Configuration.MaxThreadNumber},
                outPair =>
                {
                    var inDict = (ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>) allBlocks.inDict;

                    if (inDict.TryGetValue(outPair.Key, out var matchIns))
                    {
                        foreach (var outBlock in outPair.Value)
                        {
                            var outParentSys = subsystemDict[outBlock.FK_ProjectFileId].Find(system => system.Id == outBlock.FK_ParentSystemId - 1);
                            var outFileLevel = fileContentList.Find(fc => fc.FileId == outBlock.FK_ProjectFileId)?.FileLevel;

                            foreach (var matchInOut in matchIns)
                            {
                                var inParentSys = subsystemDict[matchInOut.FK_ProjectFileId].Find(system => system.Id == matchInOut.FK_ParentSystemId - 1);
                                var inFileLevel = fileContentList.Find(fc => fc.FileId == matchInOut.FK_ProjectFileId)?.FileLevel;

                                FilesRelationshipDTO fileRelPrototype = new FilesRelationshipDTO()
                                {
                                    System1 = outParentSys?.Name,
                                    System2 = inParentSys?.Name,
                                    Count = 1,
                                    UniCount = 1,
                                    Name = outPair.Key,
                                    Type = FileRelationship.Equal,
                                    RelationshipType = RelationshipType.In_Out
                                };

                                if (!string.IsNullOrEmpty(outFileLevel) && outFileLevel.Equals(inFileLevel))
                                {
                                    var fileToFileRela = new FilesRelationshipDTO(fileRelPrototype)
                                    {
                                        FK_ProjectFileId1 = outBlock.FK_ProjectFileId,
                                        FK_ProjectFileId2 = matchInOut.FK_ProjectFileId
                                    };

                                    lock (fileRels)
                                    {
                                        AddRelationshipToList(
                                            fileRels,
                                            fileToFileRela,
                                            new List<RelationshipType> {RelationshipType.In_Out, RelationshipType.From_Go_To}
                                        );
                                    }
                                }

                                if (outBlock.FK_ProjectFileId == matchInOut.FK_ProjectFileId && outParentSys != null && inParentSys != null)
                                {
                                    if (outParentSys.Id == inParentSys.Id) continue;

                                    var sysToSysRela = new FilesRelationshipDTO(fileRelPrototype)
                                    {
                                        FK_ProjectFileId1 = outParentSys.FK_FakeProjectFileId,
                                        FK_ProjectFileId2 = inParentSys.FK_FakeProjectFileId
                                    };

                                    lock (fileRels)
                                    {
                                        AddRelationshipToList(
                                            fileRels,
                                            sysToSysRela,
                                            new List<RelationshipType> { RelationshipType.In_Out, RelationshipType.From_Go_To }
                                        );
                                    }
                                }
                            }
                        }
                    }
                }
            );

            /*
             * CALIBRATION RELATIONSHIPS
             */

            Loggers.SVP.Info("Identify calibration relationships");

            Parallel.ForEach((ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>) allBlocks.caliDict,
                new ParallelOptions {MaxDegreeOfParallelism = Configuration.MaxThreadNumber},
                caliPair =>
                {
                    foreach (var matchSystem1 in caliPair.Value)
                    {
                        var fileLevel1 = fileContentList.Find(fc => fc.FileId == matchSystem1.FK_ProjectFileId)?.FileLevel;

                        foreach (var matchSystem2 in caliPair.Value)
                        {
                            var fileLevel2 = fileContentList.Find(fc => fc.FileId == matchSystem2.FK_ProjectFileId)?.FileLevel;

                            if (
                                (matchSystem1.Id == matchSystem2.Id && matchSystem1.FK_ProjectFileId == matchSystem2.FK_ProjectFileId) ||
                                string.IsNullOrEmpty(fileLevel1) || string.IsNullOrEmpty(fileLevel2) || !fileLevel1.Equals(fileLevel2)
                            )
                            {
                                continue;
                            }

                            FilesRelationshipDTO fileRel = new FilesRelationshipDTO()
                            {
                                FK_ProjectFileId1 = matchSystem1.FK_ProjectFileId,
                                FK_ProjectFileId2 = matchSystem2.FK_ProjectFileId,
                                Count = 1,
                                UniCount = 1,
                                Name = caliPair.Key,
                                Type = FileRelationship.Equal,
                                RelationshipType = RelationshipType.Calibration
                            };

                            lock (fileRels)
                            {
                                AddRelationshipToList(
                                    fileRels,
                                    fileRel,
                                    new List<RelationshipType> {RelationshipType.Calibration}
                                );
                            }
                        }
                    }
                }
            );

            return fileRels;
        }
        public static void BulkInsertFileContents(List<FileContent> fileContentList,string projectPath)
        {
            Loggers.SVP.Info("SAVE ALL PROJECT DATA INTO DATABASE");
            List<SystemDTO> systemDTOs = new List<SystemDTO>();
            List<BranchDTO> branchDTOs = new List<BranchDTO>();
            List<LineDTO> lineDTOs = new List<LineDTO>();
            List<PortDTO> portDTOs = new List<PortDTO>();
            List<ListDTO> listDTOs = new List<ListDTO>();
            List<InstanceDataDTO> instanceDataDTOs = new List<InstanceDataDTO>();

            DataSaver.DataSaver dataSaver = new DataSaver.DataSaver();

            Loggers.SVP.Info("prepare data ");
            foreach (var fileContent in fileContentList)
            {
                systemDTOs.AddRange(fileContent.Systems);
                branchDTOs.AddRange(fileContent.Branches);
                lineDTOs.AddRange(fileContent.Lines);
                portDTOs.AddRange(fileContent.Ports);
                listDTOs.AddRange(fileContent.Lists);
                instanceDataDTOs.AddRange(fileContent.InstanceDatas);
            }


            CsvGenerator csvGenerator = new CsvGenerator();
            Loggers.SVP.Info("data prepared");

            BulkLoader bulk_loader = new BulkLoader();

            Loggers.SVP.Info("gen csv");

            var system_CSV_path = csvGenerator.genCSVForSystem(systemDTOs, projectPath);
            Loggers.SVP.Info("system path is "+ system_CSV_path);
            if(!File.Exists(system_CSV_path))
            {
                Loggers.SVP.Error("Cannot find system csv path");
            }

            var branch_CSV_path = csvGenerator.genCSVForBranch(branchDTOs, projectPath);
            Loggers.SVP.Error("branch path is " + branch_CSV_path);
            if (!File.Exists(branch_CSV_path))
            {
                Loggers.SVP.Error("Cannot find branch path");
            }


            var port_CSV_path = csvGenerator.genCSVForPort(portDTOs, projectPath);
            Loggers.SVP.Info("port path is " + port_CSV_path);

            if (!File.Exists(system_CSV_path))
            {
                Loggers.SVP.Error("Cannot find port csv path");
            }

            var line_CSV_path = csvGenerator.genCSVForLine(lineDTOs, projectPath);
            Loggers.SVP.Info("line path is " + line_CSV_path);

            if (!File.Exists(line_CSV_path))
            {
                Loggers.SVP.Error("Cannot find line csv path");
            }


            var list_CSV_path = csvGenerator.genCSVForList(listDTOs, projectPath);
            Loggers.SVP.Info("list path is " + list_CSV_path);
            if (!File.Exists(list_CSV_path))
            {
                Loggers.SVP.Error("Cannot find list csv path");
            }



            var instance_data_CSV_path = csvGenerator.genCSVForInstanceData(instanceDataDTOs, projectPath);

            Loggers.SVP.Info("intance data path is " + instance_data_CSV_path);
            if (!File.Exists(list_CSV_path))
            {
                Loggers.SVP.Error("Cannot find instance data csv path");
            }


            Loggers.SVP.Info("start to write system");
            int sys_insert_rows = bulk_loader.WriteToDatabase(system_CSV_path, "`system`");
            Loggers.SVP.Info("inserted " + sys_insert_rows + "sys rows");

            Loggers.SVP.Info("start to write branch");
            int branch_insert_rows = bulk_loader.WriteToDatabase(branch_CSV_path, "`branch`");
            Loggers.SVP.Info("inserted " + branch_insert_rows + "branch rows");


            Loggers.SVP.Info("start to write list");
            int list_inserted_rows = bulk_loader.WriteToDatabase(list_CSV_path, "`list`");
            Loggers.SVP.Info("inserted " + list_inserted_rows + "list rows");

            Loggers.SVP.Info("start to write line");
            int line_inserted_rows = bulk_loader.WriteToDatabase(line_CSV_path, "`line`");
            Loggers.SVP.Info("inserted " + line_inserted_rows + "line rows");

            Loggers.SVP.Info("start to write port");
            int port_inserted_rows = bulk_loader.WriteToDatabase(port_CSV_path, "`port`");
            Loggers.SVP.Info("inserted " + port_inserted_rows + "port rows");

            Loggers.SVP.Info("start to write instancedata");
            int instance_data_inserted_rows = bulk_loader.WriteToDatabase(instance_data_CSV_path, "`instancedata`");
            Loggers.SVP.Info("inserted " + instance_data_inserted_rows + "instancedata rows");


            Loggers.SVP.Info("DONE SAVE PROJECT DATA TO DB");

        }


        public static void SaveFileContents(List<FileContent> fileContentList)
        {
            Loggers.SVP.Info("SAVE ALL PROJECT DATA INTO DATABASE");

            var exceptions = new ConcurrentQueue<Exception>();

            long insertedSystems = 0;
            long insertedLines = 0;
            long insertedLists = 0;
            long insertedPorts = 0;
            long insertedBranches = 0;
            long insertedInstanceDatas = 0;

            Parallel.ForEach(fileContentList,
                new ParallelOptions { MaxDegreeOfParallelism = Configuration.MaxInsertThreadNumber },
                fileContent =>
                {
                    if (exceptions.Count > 0)
                    {
                        return;
                    }

                    var errorMsg = $"file {Path.GetFileName(fileContent.FileName)}'s content";

                    InsertRows(fileContent.Systems, ref insertedSystems, errorMsg, exceptions, systemDAO.CreateSystems);
                    InsertRows(fileContent.Lines, ref insertedLines, errorMsg, exceptions, lineDAO.CreateLines);
                    InsertRows(fileContent.Branches, ref insertedBranches, errorMsg, exceptions, branchDAO.CreateBranches);
                    InsertRows(fileContent.Lists, ref insertedLists, errorMsg, exceptions, listDAO.CreateLists);
                    InsertRows(fileContent.Ports, ref insertedPorts, errorMsg, exceptions, portDAO.CreatePorts);
                    InsertRows(fileContent.InstanceDatas, ref insertedInstanceDatas, errorMsg, exceptions, instanceDataDAO.CreateInstanceDatas);

                    if (exceptions.Count == 0)
                    {
                        Loggers.SVP.Info($"INSERTED FILE {Path.GetFileName(fileContent.FileName)}");
                    }
                }
            );

            if (exceptions.Count > 0)
            {
                Loggers.SVP.Info($"Inserted systems until error: {insertedSystems}");
                Loggers.SVP.Info($"Inserted lines until error: {insertedLines}");
                Loggers.SVP.Info($"Inserted branches until error: {insertedBranches}");
                Loggers.SVP.Info($"Inserted ports until error: {insertedPorts}");
                Loggers.SVP.Info($"Inserted lists until error: {insertedLists}");
                Loggers.SVP.Info($"Inserted instance datas until error: {insertedInstanceDatas}");
                Loggers.SVP.Info(
                    $"Sum of inserted file content until error: {insertedSystems + insertedLines + insertedBranches + insertedPorts + insertedLists + insertedInstanceDatas}"
                );
                throw new AggregateException(exceptions);
            }

            Loggers.SVP.Info($"TOTAL PROJECT INSERTED systems: {insertedSystems}");
            Loggers.SVP.Info($"TOTAL PROJECT INSERTED lines: {insertedLines}");
            Loggers.SVP.Info($"TOTAL PROJECT INSERTED branches: {insertedBranches}");
            Loggers.SVP.Info($"TOTAL PROJECT INSERTED ports: {insertedPorts}");
            Loggers.SVP.Info($"TOTAL PROJECT INSERTED lists: {insertedLists}");
            Loggers.SVP.Info($"TOTAL PROJECT INSERTED instance datas: {insertedInstanceDatas}");
            Loggers.SVP.Info(
                $"SUM OF INSERTED FILE CONTENT {insertedSystems + insertedLines + insertedBranches + insertedPorts + insertedLists + insertedInstanceDatas}"
            );
        }
        public static void SaveRemaining(
            List<FilesRelationshipDTO> fileRels,
            List<CalibrationDTO> calibrations
        )
        {
            var exceptions = new ConcurrentQueue<Exception>();

            // INSERT FILE RELATIONSHIPS
            long insertedFileRels = 0;
            InsertRows(fileRels, ref insertedFileRels, "file relationship", exceptions, fileRelationshipDAO.CreateFilesRelationships, true);
            if (exceptions.Count > 0)
            {
                Loggers.SVP.Info($"Inserted file relationships until error: {insertedFileRels}");
                throw new AggregateException(exceptions);
            }
            Loggers.SVP.Info($"TOTAL PROJECT INSERTED file relationships: {insertedFileRels}");


            // INSERT CALIBRATIONS
            long insertedCali = 0;
            InsertRows(calibrations, ref insertedCali, "calibrations", exceptions, calibrationDAO.CreateCalibrations, true);
            if (exceptions.Count > 0)
            {
                Loggers.SVP.Info($"Inserted calibrations until error: {insertedCali}");
                throw new AggregateException(exceptions);
            }
            Loggers.SVP.Info($"TOTAL PROJECT INSERTED calibrations: {insertedCali}");
        }

        public static void DeleteProject(long projectId, string projectPath)
        {
            Loggers.SVP.Info("DELETE PROJECT");
            // delete from database
            projectDAO.DeleteProject(projectId);

            // delete local files
            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }
        }

        public static void DeleteProject_V2(long projectId, string projectPath)
        {
            Loggers.SVP.Info("DELETE PROJECT");

            projectDAO.DeleteProject_V2(projectId);

            if (Directory.Exists(projectPath))
            {
                Directory.Delete(projectPath, true);
            }
        }

        private static void InsertRows<T>(
            List<T> collection,
            ref long inserted,
            string type,
            ConcurrentQueue<Exception> exceptions,
            Action<ICollection<T>> insertFunc,
            bool parallel = false
        )
        {
            long currentInserted = 0;
            Action<ICollection<T>> insertAction = (list) => {
                try
                {
                    insertFunc(list);
                    currentInserted += list.Count;
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    Loggers.SVP.Exception($"Exception while saving {type}: {e.Message}", e);
                    exceptions.Enqueue(e);
                }
            };

            if (collection.Count > Configuration.MaxRowsPerInsert)
            {
                if (parallel)
                {
                    Parallel.ForEach(GenericUtils.ChunkBy(collection, Configuration.MaxRowsPerInsert),
                        new ParallelOptions { MaxDegreeOfParallelism = Configuration.MaxInsertThreadNumber },
                        insertAction                            
                    );
                }
                else
                {
                    GenericUtils.ChunkBy(collection, Configuration.MaxRowsPerInsert).ForEach(insertAction);
                }
            }
            else
            {
                insertAction(collection);
            }

            inserted += currentInserted;
        }

        private static long GetRefSysContainingFile(SystemDTO refSys, List<ProjectFileDTO> fileList)
        {
            var containingFile = fileList.Find(file => file.Name.Equals(refSys.SourceFile));

            if (containingFile == null)
            {
                //Loggers.SVP.Warning($"Cannot find file with the name {refSys.SourceFile}");

                return -1;
            }

            return containingFile.Id;
        }

        private static bool CheckIsCalibration(string calibration)
        {
            if (string.IsNullOrEmpty(calibration)) return false;

            calibration = calibration.ToLower();
            if(calibration[0] >= 'a' && calibration[0] <= 'z' && calibration != "true" && calibration != "false")
            {
                return true;
            }
            
            return false;
        }

        private static void AddCalibrationSystemWithCustomProp(
            long projectId,
            ConcurrentDictionary<string, ConcurrentBag<SystemDTO>> caliDict,
            List<CalibrationDTO> calibrations,
            Dictionary<string, string> props,
            List<string> propNames,
            SystemDTO system
        )
        {
            if (propNames.Exists(propName => !CheckIsCalibration(props[propName]))) return;

            foreach (var propName in propNames)
            {
                if (caliDict.TryGetValue(props[propName], out var systems))
                {
                    systems.Add(system);
                }
                else
                {
                    caliDict.TryAdd(props[propName], new ConcurrentBag<SystemDTO> { system });
                    CalibrationDTO calibration = new CalibrationDTO { Name = props[propName], FK_ProjectId = projectId, DataType = propName, Value = 0 };
                    calibrations.Add(calibration);
                }
            }

            Loggers.SVP.Info("Calibration Added: " + system.Properties + "FileId and SourceFile: " + system.Id + " " + system.SourceFile);
        }

        private static dynamic GetAllBlockForRelationship(List<FileContent> fileContentList, List<CalibrationDTO> calibrations, long projectId)
        {
            // from-goto
            var fromGotoContainers = new ConcurrentBag<FromGotoContainer>();

            // in-out
            var inDict = new ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>();
            var outDict = new ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>();

            // calibrations
            var caliDict = new ConcurrentDictionary<string, ConcurrentBag<SystemDTO>>();

            var subsystemDict = new ConcurrentDictionary<long, ConcurrentBag<SystemDTO>>();

            foreach (var calibration in calibrations)
            {
                caliDict.TryAdd(calibration.Name, new ConcurrentBag<SystemDTO>());
            }

            foreach (var fileContent in fileContentList)
            {
                subsystemDict.TryAdd(fileContent.FileId, new ConcurrentBag<SystemDTO>());
            }

            Parallel.ForEach(fileContentList,
                new ParallelOptions {MaxDegreeOfParallelism = 1},
                fileContent =>
                {
                    var fromGotoContainer = new FromGotoContainer(fileContent.Systems, fileContent.Lines);

                    foreach (var system in fileContent.Systems)
                    {   
                        var props = JsonSerializer.Deserialize<Dictionary<string, string>>(system.Properties);
                        try
                        {
                            switch (system.BlockType)
                            {
                                case Constants.INPORT:
                                    if (inDict.TryGetValue(system.Name, out var inports))
                                    {
                                        inports.Add(system);
                                    }
                                    else
                                    {
                                        inDict.TryAdd(system.Name, new ConcurrentBag<SystemDTO> { system });
                                    }

                                    break;
                                case Constants.OUTPORT:
                                    if (outDict.TryGetValue(system.Name, out var outports))
                                    {
                                        outports.Add(system);
                                    }
                                    else
                                    {
                                        outDict.TryAdd(system.Name, new ConcurrentBag<SystemDTO> { system });
                                    }

                                    break;
                                case Constants.FROM:
                                    if (!string.IsNullOrEmpty(system.GotoTag))
                                    {
                                        if (fromGotoContainer.FromDict.TryGetValue(system.GotoTag, out var froms))
                                        {
                                            froms.Add(system);
                                        }
                                        else
                                        {
                                            fromGotoContainer.FromDict.TryAdd(system.GotoTag, new ConcurrentBag<SystemDTO> { system });
                                        }
                                    }

                                    break;
                                case Constants.GOTO:
                                    if (!string.IsNullOrEmpty(system.GotoTag))
                                    {
                                        if (fromGotoContainer.GotoDict.TryGetValue(system.GotoTag, out var gotos))
                                        {
                                            gotos.Add(system);
                                        }
                                        else
                                        {
                                            fromGotoContainer.GotoDict.TryAdd(system.GotoTag, new ConcurrentBag<SystemDTO> { system });
                                        }
                                    }

                                    break;
                                case Constants.REF:
                                    switch (props["SourceType"])
                                    {
                                        case Constants.MC_BACKUPRAM:
                                        case Constants.MC_EEPROM:
                                            AddCalibrationSystemWithCustomProp(projectId, caliDict, calibrations, props, new List<string> { "x0" }, system);
                                            break;
                                        case Constants.MSK_PREPROCESSORIF:
                                            AddCalibrationSystemWithCustomProp(projectId, caliDict, calibrations, props, new List<string> { "ifConstant1" }, system);
                                            break;
                                        case Constants.MSK_SATURATION:
                                            AddCalibrationSystemWithCustomProp(projectId, caliDict, calibrations, props, new List<string> { "msk_max", "msk_min" }, system);
                                            break;
                                        case Constants.MSK_TABLE:
                                        case Constants.MSK_MAP:
                                        case Constants.MSK_TABLE_I:
                                        case Constants.MSK_MAP_I:
                                        case Constants.MSK_INTERPOLATE1D:
                                        case Constants.MSK_INTERPOLATE2D:
                                        case Constants.MSK_INTERPOLATE1D_I:
                                        case Constants.MSK_INTERPOLATE2D_I:
                                            AddCalibrationSystemWithCustomProp(projectId, caliDict, calibrations, props, new List<string> { "Label" }, system);
                                            break;
                                        case Constants.MSK_GAIN:
                                            AddCalibrationSystemWithCustomProp(projectId, caliDict, calibrations, props, new List<string> { "Gain" }, system);
                                            break;
                                        case Constants.MSK_CONSTANT:
                                            AddCalibrationSystemWithCustomProp(projectId, caliDict, calibrations, props, new List<string> { "Value" }, system);
                                            break;
                                        case Constants.MSK_INDEX:
                                            AddCalibrationSystemWithCustomProp(projectId, caliDict, calibrations, props, new List<string> { "table_name" }, system);
                                            break;
                                        default:
                                            foreach (var calibration in caliDict.Keys)
                                            {
                                                if (CheckIsCalibration(calibration) && props.Values.Contains(calibration))
                                                {
                                                    caliDict[calibration].Add(system);
                                                }
                                            }
                                            break;
                                    }
                                    break;
                                case Constants.SUBSYSTEM:
                                    subsystemDict[fileContent.FileId].Add(system);
                                    break;
                                default:
                                    foreach (var calibration in caliDict.Keys)
                                    {
                                        if (CheckIsCalibration(calibration) && props.Values.Contains(calibration))
                                        {
                                            caliDict[calibration].Add(system);
                                        }
                                    }

                                    break;
                            }
                        }
                        catch (Exception ex)
                        {
                            Loggers.SVP.Info(
                               $"Error while read calibration at System {system.Name ?? ""}, " +
                               $"block type = {system.BlockType ?? ""}\n"
                            );
                            Loggers.SVP.Info($"Error block Properties: {system.Properties ?? ""}");
                            Loggers.SVP.Exception($"Exception while read calibration: {ex.Message}", ex);
                        }
                    }

                     fromGotoContainers.Add(fromGotoContainer);
                }
            );

            return new {fromGotoContainers, inDict, outDict, caliDict, subsystemDict};
        }

        private static void AddRelationshipToList(
            List<FilesRelationshipDTO> fileRels,
            FilesRelationshipDTO fileRel,
            List<RelationshipType> types
        )
        {
            // find all the equal relationships that identical in file ids
            var existList = fileRels.FindAll(fr =>
                types.Contains(fr.RelationshipType) &&
                fr.FK_ProjectFileId1 == fileRel.FK_ProjectFileId1 &&
                fr.FK_ProjectFileId2 == fileRel.FK_ProjectFileId2
            );

            // find all the equal relationships that reversely identical in file ids
            var existRList = fileRels.FindAll(fr =>
                types.Contains(fr.RelationshipType) &&
                fr.FK_ProjectFileId1 != fr.FK_ProjectFileId2 &&
                fr.FK_ProjectFileId1 == fileRel.FK_ProjectFileId2 &&
                fr.FK_ProjectFileId2 == fileRel.FK_ProjectFileId1
            );

            Predicate<FilesRelationshipDTO> duplicate = fr => fr.Name.Equals(fileRel.Name) && fr.System1 == fileRel.System1 && fr.System2 == fileRel.System2;
            Predicate<FilesRelationshipDTO> revDuplicate = fr => fr.Name.Equals(fileRel.Name) && fr.System1 == fileRel.System2 && fr.System2 == fileRel.System1;
            
            bool unique = !existList.Exists(duplicate) && !existRList.Exists(revDuplicate);

            foreach (var fr in existList)
            {
                fr.Count++;
                if (unique) fr.UniCount++;
            }

            foreach (var fr in existRList)
            {
                fr.Count++;
                if (unique) fr.UniCount++;
            }

            fileRel.Count = existList.Count == 0 ? existRList.Count == 0 ? 1 : existRList[0].Count : existList[0].Count;
            fileRel.UniCount = existList.Count == 0 ? existRList.Count == 0 ? 1 : existRList[0].UniCount : existList[0].UniCount;

            if (unique)
            {
                fileRels.Add(fileRel);
            }
        }

        private static Tuple<string, string> FindRootOfRefSys(List<FileContent> fileContents, SystemDTO system)
        {
            var refRoot = new Tuple<string, string>(system.SourceFile, system.SourceBlock);

            var refFileContent = fileContents.Find(fc => fc.FileName.Equals(system.SourceFile));


            if (refFileContent != null)
            {
                var systemNames = system.SourceBlock.Split('/');
                long parentId = 0;
                SystemDTO refSys = null;

                foreach (var systemName in systemNames)
                {
                    refSys = refFileContent.Systems.Find(sys => sys.Name.Equals(systemName) && sys.FK_ParentSystemId == parentId);

                    if (refSys == null)
                    {
                        break;
                    }

                    parentId = refSys.Id;
                }

                if (refSys != null)
                {
                    if (refSys.BlockType.Equals(Constants.REF) && !refSys.SourceFile.Equals(Constants.MATLAB_LIB_REF))
                    {
                        refRoot = FindRootOfRefSys(fileContents, system);
                    } 
                    else if (refSys.BlockType.Equals(Constants.MODEL_REF))
                    {
                        refRoot = new Tuple<string, string>(refSys.SourceFile, "");
                    }
                }
            }

            return refRoot;
        }

    }
}