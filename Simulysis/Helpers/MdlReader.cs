using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using Entities.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using System.Text.Json;

namespace Simulysis.Helpers
{
    public class MdlReader : Reader
    {
        private static Dictionary<string, string> versionMap = new Dictionary<string, string>()
        {
            {"5.0.2", "R13"}, {"5.1", "R13SP1"}, {"5.2", "R13SP2"}, {"6.0", "R14"},
            {"6.1", "R14SP1"}, {"6.2", "R14SP2"}, {"6.3", "R14SP3"}, {"6.4", "R2006a"},
            {"6.5", "R2006b"}, {"6.6", "R2007a"}, {"7.0", "R2007b"}, {"7.1", "R2008a"},
            {"7.2", "R2008b"}, {"7.3", "R2009a"}, {"7.4", "R2009b"}, {"7.5", "R2010a"},
            {"7.6", "R2010b"}, {"7.7", "R2011a"}, {"7.8", "R2011b"}, {"7.9", "R2012a"},
            {"8.0", "R2012b"}, {"8.1", "R2013a"}, {"8.2", "R2013b"}, {"8.3", "R2014a"},
            {"8.4", "R2014b"}, {"8.5", "R2015a"}, {"8.6", "R2015b"}, {"8.7", "R2016a"},
            {"8.8", "R2016b"}, {"8.9", "R2017a"}, {"9.0", "R2017b"}, {"9.1", "R2018a"},
            {"9.2", "R2018b"}, {"9.3", "R2019a"}, {"10.0", "R2019b"}, {"10.1", "R2020a"},
            {"10.2", "R2020b"}, {"10.3", "R2021a"}
        };

        public long Read(
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
            string[] lines = File.ReadAllLines(filePath);
            long i = 0;

            ProjectFileDTO projectFile = new ProjectFileDTO()
            {
                Name = fileName,
                Path = filePath.Replace(projectFullPath, ""),
                Description = description,
                FK_ProjectId = projectId,
                MatlabVersion = GetVersionName(lines, ref i),
                SystemLevel = systemLevel,
                Status = true
            };

            Loggers.SVP.Info($"{fileName}: File {projectFile.Name}'s version: {projectFile.MatlabVersion}");
            //if (!projectFile.MatlabVersion.ToLower().Equals("r2011b") && !projectFile.MatlabVersion.ToLower().Equals("r2016b"))
            //{
            //    Exception ex =
            //        new Exception(
            //            $"{fileName}: Version {projectFile.MatlabVersion} is currently not supported. We only support R2011b and R2016b versions."
            //        );
            //    Loggers.SVP.Exception(ex.Message, ex);
            //}
            long fileId = projectFileDAO.CreateProjectFile(projectFile);

            try
            {
                string variant = "";

                for (int j = 0; j < lines.Length; j++)
                {
                    string parsedLn = lines[j].Trim().ToLower();
                    if (parsedLn.EndsWith("{"))
                    {
                        Loggers.SVP.Debug($"First block opening line found, line={parsedLn}");

                        if (parsedLn == "library {")
                        {
                            Loggers.SVP.Debug($"Library opening found, tagging project file as library variant!");
                            variant = "Library";
                        }

                        break;
                    }
                }
                bool testLocation = false;
                for (; i < lines.Length; i++)
                {
                    string parsedLn = lines[i].Trim().ToLower();

                    if (parsedLn.Equals("system {"))
                    {
                        testLocation = true;
                        continue;
                    }

                    if (testLocation)
                    {
                        ReadSystem(lines, ref i, fileId, filePaths);
                        break;
                    }
                }

              //  AddFromGotoConnectedSys();

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
                    FileLevelVariant = variant
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

        private void ReadSystem(string[] lines, ref long i, long fileId, IEnumerable<string> filePaths, long parentId = 0)
        {
            long systemId = nextSystemId;
            nextSystemId++;

            long initialI = i - 1;

            SystemDTO system = new SystemDTO()
            {
                Id = systemId,
                FK_ProjectFileId = fileId,
                FK_ParentSystemId = parentId,
                ContainingFile = fileName
            };

            try
            {
                var pair = SplitLine(lines[i++].Trim());
                if (HasBlockType(pair.Key))
                {
                    system.BlockType = pair.Value;

                    pair = SplitLine(lines[i++].Trim());
                    system.Name = pair.Value;

                    if (HasSID(lines[i].Trim()))
                    {
                        pair = SplitLine(lines[i++].Trim());
                        system.SID = pair.Value;
                    }
                    else
                    {
                        system.SID = "";
                    }
                }
                else if (HasBlockName(pair.Key))
                {
                    system.Name = pair.Value;
                    system.BlockType = "";
                    system.SID = "";
                }


                system.Properties = ReadProps(lines, ref i, filePaths, system);


                while (true)
                {
                    string parsedLn = lines[i++].Trim();

                    if (parsedLn.EndsWith("}"))
                    {
                        break;
                    }

                    switch (parsedLn)
                    {
                        case "Block {":
                        case "System {":
                            ReadSystem(lines, ref i, fileId, filePaths, systemId);
                            break;
                        case "Line {":
                            ReadLine(lines, ref i, fileId, systemId);
                            break;
                        case "InstanceData {":
                            ReadInstanceData(lines, ref i, fileId, systemId);
                            break;
                        case "Port {":
                            ReadPort(lines, ref i, fileId, systemId);
                            break;
                        case "List {":
                            ReadList(lines, ref i, fileId, systemId);
                            break;
                        default:
                            if (parsedLn.EndsWith("{"))
                            {
                                ReadOthers(lines, ref i);
                            }
                            else
                            {
                                i--;
                                system.Properties = CombineProps(system.Properties, ReadProps(lines, ref i, filePaths, system));
                            }

                            break;
                    }
                }

                systemDTOs.Add(system);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = "";
                    for (long j = initialI; j <= i; j++)
                    {
                        errMsg += $"{lines[j]}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n*********************************\n{errMsg}\n***********************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadLine(string[] lines, ref long i, long fileId, long systemId)
        {
            long lineId = nextLineId;
            nextLineId++;

            long initialI = i - 1;
            try
            {
                LineDTO line = new LineDTO()
                {
                    Id = lineId,
                    FK_SystemId = systemId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(lines, ref i)
                };


                while (true)
                {
                    string parsedLn = lines[i++].Trim();

                    if (parsedLn.EndsWith("}"))
                    {
                        break;
                    }

                    switch (parsedLn)
                    {
                        case "Branch {":
                            ReadBranch(lines, ref i, fileId, lineId, 0);
                            break;
                        default:
                            if (parsedLn.EndsWith("{"))
                            {
                                ReadOthers(lines, ref i);
                            }
                            else
                            {
                                i--;
                                line.Properties = CombineProps(line.Properties, ReadProps(lines, ref i));
                            }

                            break;
                    }
                }

                lineDTOs.Add(line);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = "";
                    for (long j = initialI; j <= i; j++)
                    {
                        errMsg += $"{lines[j]}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n*********************************\n{errMsg}\n***********************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadInstanceData(string[] lines, ref long i, long fileId, long systemId)
        {
            long instanceDataId = nextInstanceDataId;
            nextInstanceDataId++;

            long initialI = i - 1;

            try
            {
                InstanceDataDTO instanceData = new InstanceDataDTO()
                {
                    Id = instanceDataId,
                    FK_SystemId = systemId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(lines, ref i)
                };

                while (true)
                {
                    string parsedLn = lines[i++].Trim();

                    if (parsedLn.EndsWith("}"))
                    {
                        break;
                    }

                    switch (parsedLn)
                    {
                        default:
                            if (parsedLn.EndsWith("{"))
                            {
                                ReadOthers(lines, ref i);
                            }
                            else
                            {
                                i--;
                                instanceData.Properties = CombineProps(instanceData.Properties, ReadProps(lines, ref i));
                            }

                            break;
                    }
                }

                instanceDataDTOs.Add(instanceData);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = "";
                    for (long j = initialI; j <= i; j++)
                    {
                        errMsg += $"{lines[j]}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n*********************************\n{errMsg}\n***********************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadPort(string[] lines, ref long i, long fileId, long systemId)
        {
            long portId = nextPortId;
            nextPortId++;

            long initialI = i - 1;

            try
            {
                PortDTO port = new PortDTO()
                {
                    Id = portId,
                    FK_SystemId = systemId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(lines, ref i)
                };

                while (true)
                {
                    string parsedLn = lines[i++].Trim();

                    if (parsedLn.EndsWith("}"))
                    {
                        break;
                    }

                    switch (parsedLn)
                    {
                        default:
                            if (parsedLn.EndsWith("{"))
                            {
                                ReadOthers(lines, ref i);
                            }
                            else
                            {
                                i--;
                                port.Properties = CombineProps(port.Properties, ReadProps(lines, ref i));
                            }

                            break;
                    }
                }

                portDTOs.Add(port);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = "";
                    for (long j = initialI; j <= i; j++)
                    {
                        errMsg += $"{lines[j]}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n*********************************\n{errMsg}\n***********************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadBranch(string[] lines, ref long i, long fileId, long lineId, long parentBranchId)
        {
            long branchId = nextBranchId;
            nextBranchId++;

            long initialI = i - 1;

            try
            {
                BranchDTO branch = new BranchDTO()
                {
                    Id = branchId,
                    FK_LineId = lineId,
                    FK_BranchId = parentBranchId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(lines, ref i)
                };

                while (true)
                {
                    string parsedLn = lines[i++].Trim();

                    if (parsedLn.EndsWith("}"))
                    {
                        break;
                    }

                    switch (parsedLn)
                    {
                        case "Branch {":
                            ReadBranch(lines, ref i, fileId, 0, branchId);
                            break;
                        default:
                            if (parsedLn.EndsWith("{"))
                            {
                                ReadOthers(lines, ref i);
                            }
                            else
                            {
                                i--;
                                branch.Properties = CombineProps(branch.Properties, ReadProps(lines, ref i));
                            }

                            break;
                    }
                }

                branchDTOs.Add(branch);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = "";
                    for (long j = initialI; j <= i; j++)
                    {
                        errMsg += $"{lines[j]}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n*********************************\n{errMsg}\n***********************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadList(string[] lines, ref long i, long fileId, long systemId)
        {
            long listId = nextListId;
            nextListId++;

            long initialI = i - 1;

            try
            {
                ListDTO list = new ListDTO()
                {
                    Id = listId,
                    FK_SystemId = systemId,
                    FK_ProjectFileId = fileId,
                    Properties = ReadProps(lines, ref i)
                };

                while (true)
                {
                    string parsedLn = lines[i++].Trim();

                    if (parsedLn.EndsWith("}"))
                    {
                        break;
                    }

                    switch (parsedLn)
                    {
                        default:
                            if (parsedLn.EndsWith("{"))
                            {
                                ReadOthers(lines, ref i);
                            }
                            else
                            {
                                i--;
                                list.Properties = CombineProps(list.Properties, ReadProps(lines, ref i));
                            }

                            break;
                    }
                }

                listDTOs.Add(list);
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = "";
                    for (long j = initialI; j <= i; j++)
                    {
                        errMsg += $"{lines[j]}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n*********************************\n{errMsg}\n***********************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private void ReadOthers(string[] lines, ref long i)
        {
            long initialI = i - 1;
            try
            {
                while (true)
                {
                    string parsedLn = lines[i++].Trim();

                    if (parsedLn.EndsWith("}"))
                    {
                        break;
                    }

                    if (parsedLn.EndsWith("{"))
                    {
                        ReadOthers(lines, ref i);
                    }
                }
            }
            catch (Exception e)
            {
                if (!errIsLogged)
                {
                    string errMsg = "";
                    for (long j = initialI; j <= i; j++)
                    {
                        errMsg += $"{lines[j]}\n";
                    }

                    Loggers.SVP.Info($"{fileName}:\n*********************************\n{errMsg}\n***********************************");

                    errIsLogged = true;
                }

                throw e;
            }
        }

        private string ReadProps(string[] lines, ref long i, IEnumerable<string> filePaths = null, SystemDTO system = null)
        {
            var props = new Dictionary<string, string>();

            while (!(lines[i].EndsWith("{") || lines[i].EndsWith("}")))
            {
                var pair = SplitLine(lines[i++].Trim());

                string propVal = pair.Value;
                while (lines[i].Trim().StartsWith("\""))
                {
                    propVal = string.Concat(propVal, lines[i++].Trim().Trim('"'));
                }

                props.Add(pair.Key, propVal);

                if (system == null)
                {
                    continue;
                }
            
                if (system.BlockType.Equals(Constants.REF))
                {
                    if (pair.Key.Equals("SourceBlock"))
                    {
                        var fileNames = filePaths.Select(Path.GetFileName).ToList();

                        string[] temp = propVal.Split(new[] {'/'}, 2);
                        system.SourceFile = temp[0].Equals(Constants.MATLAB_LIB_REF)
                            ? temp[0]
                            : fileNames.Find(name => name.Equals($"{temp[0]}.slx") || name.Equals($"{temp[0]}.mdl"));
                        system.SourceBlock = temp[1];
                    }
                }

                if (system.BlockType.Equals(Constants.MODEL_REF))
                {
                    if (pair.Key.Equals("ModelNameDialog"))
                    {
                        var fileNames = filePaths.Select(Path.GetFileName).ToList();
                        try
                        {
                            var trimmedName = Path.GetFileNameWithoutExtension(propVal);
                            system.SourceFile = fileNames.Find(name =>
                                name.Equals($"{trimmedName}.slx") || name.Equals($"{trimmedName}.mdl")
                            );
                        }
                        catch (Exception ex)
                        {
                            system.SourceFile = propVal;
                            Loggers.SVP.Warning($"{ex.Message}. Filename: {propVal}");
                        }
                    }
                }

                if (system.BlockType.Equals("Goto") || system.BlockType.Equals("From"))
                {
                    if (pair.Key.Equals("GotoTag"))
                    {
                        system.GotoTag = propVal;
                    }
                }
            }

            return JsonSerializer.Serialize(props);
        }

        private string CombineProps(string props, string additionalProps)
        {
            return string.IsNullOrEmpty(props)
                ? additionalProps
                : string.Concat(
                    props.TrimEnd('}'),
                    ",",
                    additionalProps.TrimStart('{')
                );
        }

        private bool HasBlockType(string propKey)
        {
            return propKey == "BlockType";
        }

        private bool HasBlockName(string propKey)
        {
            return propKey == "Name";
        }

        private bool HasSID(string prop)
        {
            return prop.StartsWith("SID");
        }

        private string GetVersionName(string[] lines, ref long i)
        {
            while (true)
            {
                string parsedLn = lines[i++].Trim().ToLower();

                if (parsedLn.StartsWith("version"))
                {
                    var pair = SplitLine(parsedLn);
                    return versionMap.ContainsKey(pair.Value) ? versionMap[pair.Value] : pair.Value;
                }

                if (parsedLn.Equals("system {"))
                {
                    throw new Exception("Mdl file does not contains version");
                }
            }
        }

        private KeyValuePair<string, string> SplitLine(string line)
        {
            string[] pair = line.Split(new char[] {' ', '\t'}, 2);

            return new KeyValuePair<string, string>(pair[0], pair[1].Trim().Trim('"'));
        }
    }
}