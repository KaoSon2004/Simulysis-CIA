using DAO.DAO.InterfaceDAO;
using DAO.DAO.SqlServerDAO.ProjectFile;
using Entities.DTO;
using Entities.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace DAO.DAO.SqlServerDAO.FileContent
{
    public class FileContentDAO
    {
        public List<Entities.Types.FileContent> GetFileContentOfAProject(long projectId)
        {
            BranchDAO branchDAO = new BranchDAO();
            InstanceDataDAO instanceDataDAO = new InstanceDataDAO();
            LineDAO lineDAO = new LineDAO();
            ListDAO listDAO = new ListDAO();
            PortDAO portDAO = new PortDAO();
            SystemDAO systemDAO = new SystemDAO();


            List<BranchDTO> branches = new List<BranchDTO>();
            branches.AddRange(branchDAO.GetAllBranchesInAProject(projectId));
            
            List<InstanceDataDTO> instanceDatas = new List<InstanceDataDTO>();
            instanceDatas.AddRange(instanceDataDAO.GetAllInstanceDatasInAProject(projectId));

            List<LineDTO> lines = new List<LineDTO>();
            lines.AddRange(lineDAO.GetAllLinesInAProject(projectId));
            
            List<ListDTO> lists = new List<ListDTO>();
            lists.AddRange(listDAO.GetAllListsInAProject(projectId));

            List<PortDTO> ports = new List<PortDTO>();
            ports.AddRange(portDAO.GetAllPortsInProject(projectId));

            List<SystemDTO> systems = new List<SystemDTO>();
            systems.AddRange(systemDAO.GetAllSystemsInAProject(projectId));

            ProjectFileDAO projectFileDAO = new ProjectFileDAO();
            List<ProjectFileDTO> projectFileList = new List<ProjectFileDTO>();
            projectFileList = projectFileDAO.ReadAllFiles(projectId);

            //TODO: create file content list from DTOS above

            var fileIdList = projectFileList.Select(file => file.Id);
            string fileIdsToLog = "list of file id";
            foreach(long id in fileIdList )
            {
                fileIdsToLog += id + "/t";
                Loggers.SVP.Info(fileIdsToLog);
            }

            //we can refer a fileDTO from an ID from this map
            Dictionary<long, ProjectFileDTO> projectFileDict = new Dictionary<long, ProjectFileDTO>();

            foreach(var file in projectFileList)
            {
                projectFileDict.Add(file.Id, file);
            }

            Dictionary<long, Entities.Types.FileContent> fileContentDict = new Dictionary<long, Entities.Types.FileContent>();

            foreach (long fileId in fileIdList)
            {
                fileContentDict.Add(fileId, new Entities.Types.FileContent());
            }

            //add lines
            foreach (LineDTO lineDTO in lines)
            {
                try
                {
                    fileContentDict[lineDTO.FK_ProjectFileId].Lines.Add(lineDTO);
                }
                catch(Exception ex)
                {
                    string exist = " n ";
                    if (fileIdList.Contains(lineDTO.FK_ProjectFileId))
                    {
                        exist =  " y ";
                    }

                    Loggers.SVP.Exception("-e = " +exist + "lineDTO with fileId"+lineDTO.FK_ProjectFileId +" has exception \n"+ex.Message,ex );
                    continue;

                }
            }

            //add ports
            foreach(PortDTO portDTO in ports)
            {
                try
                {
                    fileContentDict[portDTO.FK_ProjectFileId].Ports.Add(portDTO);
                }
                catch(Exception ex)
                {
                    string exist = " n ";
                    if (fileIdList.Contains(portDTO.FK_ProjectFileId))
                    {
                        exist = " y ";
                    }

                    Loggers.SVP.Exception("-e = " + exist +"portDTO with fileId" + portDTO.FK_ProjectFileId + " has exception \n" + ex.Message,ex);
                    continue;
                }
            }
            //add systems
            foreach(SystemDTO systemDTO in systems)
            {
                try
                {
           
                    fileContentDict[systemDTO.FK_ProjectFileId].Systems.Add(systemDTO);
                }
                catch(Exception ex)
                {
                    string exist = " n ";
                    if (fileIdList.Contains(systemDTO.FK_ProjectFileId))
                    {
                        exist = " y ";
                    }
                    Loggers.SVP.Exception("-e = " + exist+ "systemDTO with fileId" + systemDTO.FK_ProjectFileId + " has exception \n" + ex.Message,ex);
                    continue;
                }
            }
            //add branches
            foreach(BranchDTO branchDTO in branches)
            {
                try
                {
                    fileContentDict[(branchDTO.FK_ProjectFileId)].Branches.Add(branchDTO);
                }
                catch(Exception ex)
                {
                    string exist = " n ";
                    if (fileIdList.Contains(branchDTO.FK_ProjectFileId))
                    {
                        exist = " y ";
                    }
                    Loggers.SVP.Exception("-e = " + exist + "branchDTO with fileId" + branchDTO.FK_ProjectFileId + " has exception \n" + ex.Message,ex);
                    continue;
                }
            }
            //add instancedatas
            foreach(InstanceDataDTO instanceDataDTO in instanceDatas)
            {
                try
                {
                    fileContentDict[(instanceDataDTO.FK_ProjectFileId)].InstanceDatas.Add(instanceDataDTO);
                }
                catch (Exception ex)
                {
                    string exist = " n ";
                    if (fileIdList.Contains(instanceDataDTO.FK_ProjectFileId))
                    {
                        exist = " y ";
                    }
                    Loggers.SVP.Exception("-e = " + exist + "instanceDataDTO with fileId" + instanceDataDTO.FK_ProjectFileId + " has exception \n" + ex.Message,ex);
                    continue;
                }
            }
            //add lists
            foreach(ListDTO listDTO in lists)
            {
                try
                {
                    fileContentDict[listDTO.FK_ProjectFileId].Lists.Add(listDTO);
                }
                catch(Exception ex)
                {
                    string exist = " n ";
                    if (fileIdList.Contains(listDTO.FK_ProjectFileId))
                    {
                        exist = " y ";
                    }
                    Loggers.SVP.Exception("-e = " + exist + "listDTO with fileId" + listDTO.FK_ProjectFileId + " has exception \n" + ex.Message,ex);
                    continue;
                }
            }

            foreach(ProjectFileDTO projectFileDTO in projectFileList)
            {
                try
                {
                    fileContentDict[projectFileDTO.Id].FileId = projectFileDTO.Id;
                    fileContentDict[projectFileDTO.Id].FileLevel = projectFileDTO.SystemLevel;
                    fileContentDict[projectFileDTO.Id].FileName = projectFileDTO.Name;
                }
                catch(Exception ex)
                {
                    Loggers.SVP.Exception(ex.Message, ex);
                    continue;
                }
            }

            List<Entities.Types.FileContent> fileContents = new List<Entities.Types.FileContent>();
            fileContents.AddRange(fileContentDict.Values);
          
            return fileContents;
        }
    }
}
