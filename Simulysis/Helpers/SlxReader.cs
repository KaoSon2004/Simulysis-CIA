using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using Entities.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Web;
using System.Xml.Linq;
using Common;
using System.Text.Json;
using Microsoft.CodeAnalysis;

namespace Simulysis.Helpers
{
    public class SlxReader : Reader
    {
        public virtual long Read(
            string filePath,
            long projectId,
            string projectFullPath,
            string systemLevel,
            string description,
            List<FileContent> fileContentList,
            IEnumerable<string> filePaths
        )
        {
            fileName = Path.GetFileName(filePath);
            Loggers.SVP.Info($"{fileName}: Reading {fileName}");

            var extractFolder = Directory.CreateDirectory(Path.Combine(projectFullPath, Constants.SLX_EXTRACT_FOLDER));

            string zipPath = Path.Combine(extractFolder.FullName, Path.ChangeExtension(fileName, ".zip"));

            File.Copy(filePath, zipPath);

            string desPath = Path.Combine(extractFolder.FullName, Path.GetFileNameWithoutExtension(fileName));
            
            ZipFile.ExtractToDirectory(zipPath, desPath);

            return ReadCore(filePath, projectId, projectFullPath, systemLevel, description, desPath, fileContentList, filePaths);
        }

        protected long ReadCore(string filePath, long projectId, string projectFullPath, string systemLevel, string description,
            string desPath, List<FileContent> fileContentList, IEnumerable<string> filePaths, string versionName = null)
        {
            ProjectFileDTO projectFile = new ProjectFileDTO()
            {
                Name = fileName,
                Path = filePath.Replace(projectFullPath, ""),
                Description = description,
                FK_ProjectId = projectId,
                SystemLevel = systemLevel,
                LevelVariant = discoveredFileVariant,
                Status = true
            };

            if (versionName != null)
            {
                projectFile.MatlabVersion = versionName;
            }

            //if (!projectFile.MatlabVersion.ToLower().Equals("r2011b") && !projectFile.MatlabVersion.ToLower().Equals("r2016b"))
            //{
            //    Exception ex =
            //        new Exception(
            //            $"{fileName}: Version {projectFile.MatlabVersion} is currently not supported. We only support R2011b and R2016b versions."
            //        );
            //    Loggers.SVP.Exception(ex.Message, ex);
            //}

            try
            {
                projectFile.MatlabVersion = GetVersion(Path.Combine(desPath, "metadata/coreProperties.xml"));
                Loggers.SVP.Info($"{fileName}: File {projectFile.Name}'s version: {projectFile.MatlabVersion}");
            }
            catch (Exception e)
            {
                projectFile.Status = false;
                projectFile.ErrorMsg = e.Message;
                projectFileDAO.CreateProjectFile(projectFile);

                throw new Exception($"{fileName}: {e.Message}");
            }

            long fileId = projectFileDAO.CreateProjectFile(projectFile);

            try
            {
                if (IsNewerThan2016(projectFile.MatlabVersion) && File.Exists(Path.Combine(desPath, "simulink/systems/system_root.xml")))
                {
                    ReadNewXml(Path.Combine(desPath, "simulink/systems"), "system_root.xml", fileId, filePaths);
                }
                else
                {
                    ReadOldXml(Path.Combine(desPath, "simulink/blockdiagram.xml"), fileId, filePaths);
                }

                // AddFromGotoConnectedSys();

                fileContentList.Add(new FileContent()
                {
                    FileName = projectFile.Name,
                    FileId = fileId,
                    Systems = systemDTOs,
                    Lines = lineDTOs,
                    Branches = branchDTOs,
                    Ports = portDTOs,
                    InstanceDatas = instanceDataDTOs,
                    Lists = listDTOs,
                    FileLevelVariant = discoveredFileVariant
                });

                Loggers.SVP.Info($"{fileName}: Done reading {projectFile.Name}");
                return fileId;
            }
            catch (Exception e)
            {
                projectFile.Id = fileId;
                projectFile.Status = false;
                projectFile.ErrorMsg = e.Message;
                projectFileDAO.UpdateStatus(projectFile);

                throw new Exception($"{fileName}: {e.Message}");
            }
        }

        private void ReadOldXml(string filePath, long fileId, IEnumerable<string> filePaths)
        {
            XDocument document = XDocument.Load(filePath);
            if (document.Element("Library") != null)
            {
                Loggers.SVP.Info($"Discover direct child with element tag Library in {fileName}. Tagging file as Library"); ;
                discoveredFileVariant = "Library";
            } else
            {
                string tagLine = "";
                foreach (var elem in document.Elements())
                {
                    tagLine += elem.Name + ", ";
                }
                Loggers.SVP.Info($"Unable to find direct children with Library tag in {fileName}. Available children tags: {tagLine}");
            }
            XElement root = document.Descendants("System").First();

            ReadSystem(root, string.Empty, fileId, 0, filePaths);
        }

        private void ReadNewXml(string dirPath, string fileName, long fileId, IEnumerable<string> filePaths, long parentId = 0)
        {
            XDocument document = XDocument.Load(Path.Combine(dirPath, fileName));
            if (document.Element("Library") != null)
            {
                discoveredFileVariant = "Library";
            }
            XElement root = document.Element("System");

            ReadSystem(root, dirPath, fileId, parentId, filePaths);
        }

        private void ReadSystem(XElement element, string dirPath, long fileId, long parentId, IEnumerable<string> filePaths)
        {
            long systemId = nextSystemId;
            nextSystemId++;

            try
            {
                XAttribute SID = element.Attribute("SID");
                XAttribute blockType = element.Attribute("BlockType");
                XAttribute name = element.Attribute("Name");

                SystemDTO system = new SystemDTO()
                {
                    Id = systemId,
                    FK_ProjectFileId = fileId,
                    FK_ParentSystemId = parentId,
                    Name = name != null ? name.Value : "",
                    SID = SID != null ? SID.Value : "",
                    BlockType = blockType != null ? blockType.Value : "",
                    ContainingFile = fileName
                };
                system.Properties = ReadProps(element, filePaths, system);

                foreach (XElement child in element.Elements().Where(e => e.Name != "P"))
                {
                    switch (child.Name.ToString())
                    {
                        case "Block":
                            ReadSystem(child, dirPath, fileId, systemId, filePaths);
                            break;
                        case "Line":
                            ReadLine(child, fileId, systemId);
                            break;
                        case "Port":
                            ReadPort(child, fileId, systemId);
                            break;
                        case "List":
                            ReadList(child, fileId, systemId);
                            break;
                        case "InstanceData":
                            ReadInstanceData(child, fileId, systemId);
                            break;
                        case "System":
                            if (string.IsNullOrEmpty(dirPath))
                            {
                                ReadSystem(child, string.Empty, fileId, systemId, filePaths);
                            }
                            else
                            {
                                ReadNewXml(dirPath, $"{child.Attribute("Ref").Value}.xml", fileId, filePaths, systemId);
                            }

                            break;
                        default:
                            continue;
                    }
                }

                systemDTOs.Add(system);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = $"{element.Name}\n";
                    foreach (var attribute in element.Attributes())
                    {
                        errMsg += $"{attribute.Name}: {attribute.Value}\n";
                    }

                    foreach (var child in element.Elements("P"))
                    {
                        errMsg += $"{child.Attribute("Name").Value}: {child.Value}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n****************************\n{errMsg}\n*****************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadPort(XElement element, long fileId, long systemId)
        {
            long portId = nextPortId;
            nextPortId++;

            try
            {
                PortDTO port = new PortDTO()
                {
                    Id = portId,
                    FK_ProjectFileId = fileId,
                    FK_SystemId = systemId,
                    Properties = ReadProps(element)
                };

                portDTOs.Add(port);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = $"{element.Name}\n";
                    foreach (var attribute in element.Attributes())
                    {
                        errMsg += $"{attribute.Name}: {attribute.Value}\n";
                    }

                    foreach (var child in element.Elements("P"))
                    {
                        errMsg += $"{child.Attribute("Name").Value}: {child.Value}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n****************************\n{errMsg}\n*****************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadList(XElement element, long fileId, long systemId)
        {
            long listId = nextListId;
            nextListId++;
            try
            {
                ListDTO list = new ListDTO()
                {
                    Id = listId,
                    FK_SystemId = systemId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(element)
                };

                listDTOs.Add(list);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = $"{element.Name}\n";
                    foreach (var attribute in element.Attributes())
                    {
                        errMsg += $"{attribute.Name}: {attribute.Value}\n";
                    }

                    foreach (var child in element.Elements("P"))
                    {
                        errMsg += $"{child.Attribute("Name").Value}: {child.Value}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n****************************\n{errMsg}\n*****************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadInstanceData(XElement element, long fileId, long systemId)
        {
            long instanceDataId = nextInstanceDataId;
            nextInstanceDataId++;

            try
            {
                InstanceDataDTO instanceData = new InstanceDataDTO()
                {
                    Id = instanceDataId,
                    FK_SystemId = systemId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(element)
                };

                instanceDataDTOs.Add(instanceData);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = $"{element.Name}\n";
                    foreach (var attribute in element.Attributes())
                    {
                        errMsg += $"{attribute.Name}: {attribute.Value}\n";
                    }

                    foreach (var child in element.Elements("P"))
                    {
                        errMsg += $"{child.Attribute("Name").Value}: {child.Value}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n****************************\n{errMsg}\n*****************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadLine(XElement element, long fileId, long systemId)
        {
            long lineId = nextLineId;
            nextLineId++;

            try
            {
                LineDTO line = new LineDTO()
                {
                    Id = lineId,
                    FK_ProjectFileId = fileId,
                    FK_SystemId = systemId,
                    Properties = ReadProps(element)
                };

                foreach (XElement child in element.Elements("Branch"))
                {
                    ReadBranch(child, fileId, lineId, 0);
                }

                lineDTOs.Add(line);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = $"{element.Name}\n";
                    foreach (var attribute in element.Attributes())
                    {
                        errMsg += $"{attribute.Name}: {attribute.Value}\n";
                    }

                    foreach (var child in element.Elements("P"))
                    {
                        errMsg += $"{child.Attribute("Name").Value}: {child.Value}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n****************************\n{errMsg}\n*****************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadBranch(XElement element, long fileId, long lineId, long parentBranchId)
        {
            long branchId = nextBranchId;
            nextBranchId++;

            try
            {
                BranchDTO branch = new BranchDTO()
                {
                    Id = branchId,
                    FK_LineId = lineId,
                    FK_BranchId = parentBranchId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(element)
                };

                foreach (XElement child in element.Elements("Branch"))
                {
                    ReadBranch(child, fileId, 0, branchId);
                }

                branchDTOs.Add(branch);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = $"{element.Name}\n";
                    foreach (var attribute in element.Attributes())
                    {
                        errMsg += $"{attribute.Name}: {attribute.Value}\n";
                    }

                    foreach (var child in element.Elements("P"))
                    {
                        errMsg += $"{child.Attribute("Name").Value}: {child.Value}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n****************************\n{errMsg}\n*****************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private string ReadProps(XElement element, IEnumerable<string> filePaths = null, SystemDTO system = null)
        {
            var props = new Dictionary<string, string>();

            foreach (XElement child in element.Elements("P"))
            {
                string key = child.Attribute("Name").Value;
                string value = child.Value;
                props.Add(key, value);

                if (system == null)
                {
                    continue;
                }

                if (system.BlockType.Equals(Constants.REF))
                {
                    if (key.Equals("SourceBlock"))
                    {
                        string[] temp = value.Split(new[] {'/'}, 2);

                        var fileNames = filePaths.Select(Path.GetFileName).ToList();

                        system.SourceFile = temp[0].Equals(Constants.MATLAB_LIB_REF)
                            ? temp[0]
                            : fileNames.Find(name => name.Equals($"{temp[0]}.slx") || name.Equals($"{temp[0]}.mdl"));
                        system.SourceBlock = temp[1];
                    }
                }

                if (system.BlockType.Equals(Constants.MODEL_REF))
                {
                    if (key.Equals("ModelNameDialog"))
                    {
                        var fileNames = filePaths.Select(Path.GetFileName).ToList();
                        try
                        {
                            var trimmedName = Path.GetFileNameWithoutExtension(value);
                            system.SourceFile = fileNames.Find(name =>
                                name.Equals($"{trimmedName}.slx") || name.Equals($"{trimmedName}.mdl")
                            );
                        }
                        catch (Exception ex)
                        {
                            system.SourceFile = value;
                            Loggers.SVP.Warning($"{ex.Message}. Filename: {value}");
                        }
                    }
                }

                if (system.BlockType.Equals("Goto") || system.BlockType.Equals("From"))
                {
                    if (key.Equals("GotoTag"))
                    {
                        system.GotoTag = value;
                    }
                }
            }

            if (system != null && system.BlockType.Equals(Constants.REF))
            {
                var instanceDataEle = element.Element("InstanceData");
                foreach (XElement child in instanceDataEle.Elements("P"))
                {
                    string key = child.Attribute("Name").Value;
                    string value = child.Value;
                    props.Add(key, value);

                    if (system == null)
                    {
                        continue;
                    }
                }

            }

            return JsonSerializer.Serialize(props);
        }

        private string GetVersion(string filePath)
        {
            XNamespace cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
            return XDocument.Load(filePath).Descendants(cp + "version").FirstOrDefault().Value;
        }

        private bool IsNewerThan2016(string version)
        {
            return version.Length >= 6 && Convert.ToInt32(version.Substring(1, 4)) > 2016;
        }
    }
}