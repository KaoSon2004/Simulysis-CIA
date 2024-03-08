using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Entities.DTO;
using Entities.Logging;

namespace Simulysis.Helpers
{
    public class FileHelper
    {
        private static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        public static long ReadSingleFile(
            string filePath,
            string fileExt,
            long projectId,
            string projectPath,
            string systemLevel,
            string description
            , List<FileContent> fileContentList = null
        )
        {
            long id = -1;
            try
            {
                //get all file id
                var files = projectFileDAO.ReadAllFiles(projectId);
                //read file from .mdl or slx
               // List<FileContent> fileContentList = new List<FileContent>();
                id = ReadFile(filePath, fileExt, projectId, projectPath, systemLevel, description, files.Select(file => file.Name),fileContentList);
                return id;
            }
            catch (Exception ex)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                if (id > 0)
                {
                    projectFileDAO.DeleteProjectFile(id);
                }

                throw ex;
            }
           
        }

        public static long ReadFile(
            string filePath,
            string fileExt,
            long projectId,
            string projectPath,
            string systemLevel,
            string description,
            IEnumerable<string> filePaths,
            List<FileContent> fileContentList = null
        )
        {
            if (fileExt == ".mdl")
            {
                if (MdlExtendedReader.IsMdlExtendedFile(filePath))
                {
                    MdlExtendedReader readerExtended = new MdlExtendedReader();
                    return readerExtended.Read(filePath, projectId, projectPath, systemLevel, description, fileContentList, filePaths);
                }
                else
                {
                    MdlReader reader = new MdlReader();
                    return reader.Read(filePath, projectId, projectPath, systemLevel, description, fileContentList, filePaths);
                }
            }
            else
            {
                SlxReader reader = new SlxReader();
                return reader.Read(filePath, projectId, projectPath, systemLevel, description, fileContentList, filePaths);
            }
        }

        public static List<CalibrationDTO> ReadMFile(string filePath, long projectId)
        {
            Loggers.SVP.Info($"Reading {Path.GetFileName(filePath)}");
            string[] lines = File.ReadAllLines(filePath);

            List<CalibrationDTO> calibrations = new List<CalibrationDTO>();

            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains(Constants.SIMULINK_PARAM))
                {
                    string currentConstant = lines[i].Split('=')[0].Trim();

                    CalibrationDTO calibration = new CalibrationDTO {Name = currentConstant, FK_ProjectId = projectId, DataType = "Unknown"};
                    
                    while (lines[++i].StartsWith(currentConstant))
                    {
                        string val = lines[i].Split('=')[1].TrimEnd(';').Trim('\'');
                        if (lines[i].StartsWith($"{currentConstant}.Value"))
                        {
                            calibration.Value = Convert.ToDecimal(val);
                        }
                        else if (lines[i].StartsWith($"{currentConstant}.Description"))
                        {
                            calibration.Description = val;
                        }
                        else if (lines[i].StartsWith($"{currentConstant}.DataType"))
                        {
                            // NOTE: This should requires script analysis to keep track of parameter value assignment
                            calibration.DataType = val;
                        }
                    }

                    calibrations.Add(calibration);
                    i--;
                }
            }

            Loggers.SVP.Info($"Done reading {Path.GetFileName(filePath)}");

            return calibrations;
        }

        public static List<FilesRelationshipDTO> GetChildParentRelationships(long projectId, FileContent content, List<FileContent> fileContents)
        {
            List<FilesRelationshipDTO> relationships = new List<FilesRelationshipDTO>();
            Dictionary<long, ProjectFileDTO> subsystemIdToVirtProjectFileLookup = new Dictionary<long, ProjectFileDTO>();
            
            content.Systems.ForEach(system =>
            {
                if (
                    !system.BlockType.Equals(Constants.SUBSYSTEM) &&
                    !system.BlockType.Equals(Constants.MODEL_REF) &&
                    (!system.BlockType.Equals(Constants.REF) || system.SourceFile == null ||  system.SourceFile.Equals(Constants.MATLAB_LIB_REF))
                )
                {
                    return;
                }

                ProjectFileDTO dtoSubsystem = null;
                FileContent referencedFileContent = null;
                if (system.BlockType.Equals(Constants.SUBSYSTEM))
                {
                    if (subsystemIdToVirtProjectFileLookup.ContainsKey(system.Id))
                    {
                        dtoSubsystem = subsystemIdToVirtProjectFileLookup[system.Id];
                    }
                    else
                    {
                        dtoSubsystem = new ProjectFileDTO()
                        {
                            Name = system.Name,
                            Path = content.FileName + $":{system.Name}-{system.Id}",
                            Description = system.Id.ToString(),
                            FK_ProjectId = projectId,
                            SystemLevel = Constants.SUBSYSTEM,
                            Status = true
                        };
                        dtoSubsystem.Id = projectFileDAO.CreateProjectFile(dtoSubsystem);
                        system.FK_FakeProjectFileId = dtoSubsystem.Id;

                        subsystemIdToVirtProjectFileLookup.Add(system.Id, dtoSubsystem);
                    }
                }
                else
                {
                    referencedFileContent = fileContents.Find(fileContent => fileContent.FileName.Equals(system.SourceFile));
                    if (referencedFileContent == null)
                    {
                        Loggers.SVP.Error($"Can't find referenced file of name {system.SourceFile}");
                        return;
                    }
                }

                long parentFileId = 0;
                long realParentId = 0;
                
                if (system.FK_ParentSystemId == 1)
                {
                    parentFileId = content.FileId;
                }
                else
                {
                    SystemDTO realParent = content.Systems.Find(systemIte => systemIte.Id == system.FK_ParentSystemId - 1);
                    if (realParent == null)
                    {
                        Loggers.SVP.Error($"Can't find parent system of a system to initialize subsystem child-parent relationship! (parentId={system.FK_ParentSystemId}, id={system.Id})");
                        return;
                    }
                    realParentId = realParent.Id;

                    if (subsystemIdToVirtProjectFileLookup.ContainsKey(realParentId))
                    {
                        parentFileId = subsystemIdToVirtProjectFileLookup[realParentId].Id;
                    }
                    else
                    {
                        ProjectFileDTO dtoParentFile = new ProjectFileDTO()
                        {
                            Name = realParent.Name,
                            Path = content.FileName + $":{realParent.Name}-{realParent.Id}",
                            Description = realParent.Id.ToString(),
                            FK_ProjectId = projectId,
                            SystemLevel = Constants.SUBSYSTEM,
                            Status = true
                        };

                        dtoParentFile.Id = projectFileDAO.CreateProjectFile(dtoParentFile);
                        realParent.FK_FakeProjectFileId = dtoParentFile.Id;

                        subsystemIdToVirtProjectFileLookup.Add(realParentId, dtoParentFile);

                        parentFileId = dtoParentFile.Id;
                    }
                }

                FilesRelationshipDTO relationship = new FilesRelationshipDTO(
                    dtoSubsystem != null ? dtoSubsystem.Id : referencedFileContent.FileId,
                    parentFileId, 1, FileRelationship.Child_Parent, RelationshipType.In_Out
                )
                {
                    UniCount = 1
                };

                if (realParentId > 0)
                {
                    relationship.System2 = realParentId.ToString();
                }

                if (system.BlockType.Equals(Constants.SUBSYSTEM))
                {
                    relationship.System1 = system.Id.ToString();
                }

                relationships.Add(relationship);
            });

            return relationships;
        }
    }
}