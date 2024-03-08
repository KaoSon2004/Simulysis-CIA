using Common;
using DAO.DAO.SqlServerDAO.FileContent;
using Entities.DTO;
using Entities.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simulysis.Helpers.SignalSearch
{
    public class FromGoToType : SignalSearchStrategy
    {
        private bool inSetOfFiles = true;

        public FromGoToType(bool inSetOfFiles)
        {
            this.inSetOfFiles = inSetOfFiles;
        }

        public override List<Signal> Search(SearchInput input)
        {
            SystemDAO systemDAO = new SystemDAO();
            Loggers.SVP.Info(inSetOfFiles ? "SearchSystemsByGotoTagInASetOfFiles" : "SearchSystemsByGotoTagInAProject");
            List<SystemDTO> foundList = inSetOfFiles ? systemDAO.SearchSystemsByGotoTagInASetOfFiles(input.Name, input.ProjectFileIdsSet)
                : systemDAO.SearchSystemByGoToTagInProject(input.Name, input.ProjectId);
            List<Signal> results = new List<Signal>();
            List<SystemDTO> matchedSystem = new List<SystemDTO>();
            Loggers.SVP.Info("get candidateParents");
            List<SystemDTO> candidateParents = systemDAO.GetAllEmptyBlockTypeSystemsInAProject(input.ProjectId);
            Loggers.SVP.Info("Set parent system");
            for (int i = 0; i < foundList.Count; i++)
            {
                foreach (SystemDTO parent in candidateParents)
                {
                    if (foundList[i].FK_ParentSystemId == parent.Id && foundList[i].ContainingFile == parent.ContainingFile)
                    {  //TODO
                        foundList[i].ParentSystemName = parent.Name;
                        break;
                    }
                }
            }
            foreach (SystemDTO system in foundList)
            {
                if (String.IsNullOrEmpty(system.ParentSystemName))
                {
                    Loggers.SVP.Warning("cannot find parent for system : " + system);
                    //consider :
                    // system.ParentSystemName = systemDAO.SearchParentSystem(system).Name;

                }
            }
            foreach (SystemDTO sys1 in foundList)
            {
                Loggers.SVP.Info("before search matched system");
                matchedSystem = SearchMatchedSystems(sys1, foundList);
                Loggers.SVP.Info("after search matched system");
                SystemDTO parent_sys1 = systemDAO.SearchParentSystem(sys1);

                bool isSystem1From = (sys1.BlockType == Constants.GOTO);
                string system1Info = $"{sys1.ConnectedRefSrcFile}|{sys1.ParentSystemName}|{parent_sys1.Id}|{parent_sys1.FK_ProjectFileId}|{parent_sys1.FK_FakeProjectFileId}|{parent_sys1.ContainingFile}";

                if (matchedSystem.Count == 0)
                {
                    Signal signal = new Signal((sys1.GotoTag == "" ? sys1.Name : sys1.GotoTag), isSystem1From ? system1Info : "", isSystem1From ? "" : system1Info);
                    if (String.IsNullOrEmpty(signal.From) && String.IsNullOrEmpty(signal.To))
                    {
                        Loggers.SVP.Warning("Empty signal with system1 : " + sys1 + "/n" + " and not match");
                    }
                    else
                    {
                        results.Add(signal);
                    }
                }
                else
                {
                    foreach (var sys2 in matchedSystem)
                    {
                        SystemDTO parent_sys2 = systemDAO.SearchParentSystem(sys2);
                        string system2Info = $"{sys2.ConnectedRefSrcFile}|{sys2.ParentSystemName}|{parent_sys2.Id}|{parent_sys2.FK_ProjectFileId}|{parent_sys2.FK_FakeProjectFileId}|{parent_sys2.ContainingFile}";

                        Signal signal = new Signal(
                            sys1.GotoTag == "" ? sys1.Name : sys1.GotoTag,
                            isSystem1From ? system1Info : system2Info,
                            isSystem1From ? system2Info : system1Info
                        );

                        if (String.IsNullOrEmpty(signal.From) && String.IsNullOrEmpty(signal.To))
                        {
                            Loggers.SVP.Warning("Empty signal with system1 : " + sys1 + "/n" + " system2 :" + sys2);
                        }
                        else
                        {
                            results.Add(signal);
                        }
                    }
                }
            }
            Loggers.SVP.Info("List of signal for FROMGOTOINASETOFFILES before distinct");
            foreach (Signal signal in results)
            {
                Loggers.SVP.Info(signal + "\n");
            }
            Loggers.SVP.Info("List of signal for FROMGOTOINASETOFFILES after distinct");
            results = results.Distinct().ToList();
            foreach (Signal signal in results)
            {
                Loggers.SVP.Info(signal + "\n");
            }
            return results;

        }

        public List<SystemDTO> SearchMatchedSystems(SystemDTO sys1, List<SystemDTO> foundList)
        {
            List<SystemDTO> matchedSystem = new List<SystemDTO>();
            foreach (SystemDTO sys2 in foundList)
            {
                if (sys2.Id != sys1.Id && sys2.FK_ProjectFileId == sys1.FK_ProjectFileId && sys2.BlockType != sys1.BlockType && sys1.GotoTag == sys2.GotoTag)
                {
                    if (sys1.BlockType == Constants.FROM)
                    {
                        //find GOTO
                        if (sys2.BlockType == Constants.GOTO)
                        {
                            matchedSystem.Add(sys2);
                        }
                    }
                    else if (sys1.BlockType == Constants.GOTO)
                    {
                        //find FROM
                        if (sys2.BlockType == Constants.FROM)
                        {
                            matchedSystem.Add((sys2));
                        }
                    }
                }
            }
            return matchedSystem;
        }
    }
}