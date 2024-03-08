using Common;
using DAO.DAO.SqlServerDAO.FileContent;
using Entities.DTO;
using Entities.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Simulysis.Helpers.SignalSearch
{
    public class InportOutportType : SignalSearchStrategy
    {
        private bool inSetOfFiles = true;

        public InportOutportType(bool inSetOfFiles)
        {
            this.inSetOfFiles = inSetOfFiles;
        }

        public override List<Signal> Search(SearchInput input)
        {
            SystemDAO systemDAO = new SystemDAO();
            List<SystemDTO> foundList = new List<SystemDTO>();
            Loggers.SVP.Info("before " + (inSetOfFiles ? "SearchSystemByNameInView" : "SearchSystemByNameInProject"));
            foundList = inSetOfFiles ? systemDAO.SearchInPortOutPortSystemsByNameInASetOfFiles(input.Name, input.ProjectFileIdsSet)
                : systemDAO.SearchInPortOutPortSystemByNameInProject(input.Name, input.ProjectId);
            Loggers.SVP.Info("size of foundList :" + foundList.Count);
            //result is basically a pair of systems
            List<Signal> results = new List<Signal>();

            List<SystemDTO> matchedSystem = new List<SystemDTO>();
            Loggers.SVP.Info("before foreach (SystemDTO sys1 in foundList) ");
            foreach (SystemDTO sys1 in foundList)
            {
                matchedSystem = SearchMatchedSystems(sys1, foundList);

                SystemDTO parent_sys1 = systemDAO.SearchParentSystem(sys1);
                string system1Info = $"{sys1.ContainingFile}|{sys1.ParentSystemName}|{parent_sys1.Id}|{parent_sys1.FK_ProjectFileId}|{parent_sys1.FK_FakeProjectFileId}|{parent_sys1.ContainingFile}";

                bool isSystem1From = (sys1.BlockType == Constants.OUTPORT);

                if (matchedSystem.Count == 0)
                {
                    results.Add(new Signal(sys1.Name, isSystem1From ? system1Info : "", isSystem1From ? "" : system1Info));
                    continue;
                }
                foreach (var sys2 in matchedSystem)
                {
                    SystemDTO parent_sys2 = systemDAO.SearchParentSystem(sys2);
                    string system2Info = $"{sys2.ContainingFile}|{sys2.ParentSystemName}|{parent_sys2.Id}|{parent_sys2.FK_ProjectFileId}|{parent_sys2.FK_FakeProjectFileId}|{parent_sys1.ContainingFile}";

                    results.Add(new Signal(sys1.Name, isSystem1From ? system1Info : system2Info, isSystem1From ? system2Info : system1Info));
                }
            }

            Loggers.SVP.Info("List of signal for INPORTOUTPORTINASETOFFILES before distinct");
            foreach (Signal signal in results)
            {
                Loggers.SVP.Info(signal + "\n");
            }
            Loggers.SVP.Info("List of signal for INPORTOUTPORTINASETOFFILES after distinct");
            results = results.Distinct().ToList();
            foreach (Signal signal in results)
            {
                Loggers.SVP.Info(signal + "\n");
            }
            return results;

        }

        List<SystemDTO> SearchMatchedSystems(SystemDTO sys1, List<SystemDTO> foundList)
        {
            List<SystemDTO> matchedSystem = new List<SystemDTO>();
            foreach (SystemDTO sys2 in foundList)
            {
                if (sys2.BlockType != sys1.BlockType && sys2.ContainingFile != sys1.ContainingFile && sys1.Name == sys2.Name)
                {
                    if (sys1.BlockType == Constants.OUTPORT)
                    {
                        //find INPORT
                        if (sys2.BlockType == Constants.INPORT)
                        {
                            matchedSystem.Add(sys2);
                        }
                    }
                    else if (sys1.BlockType == Constants.INPORT)
                    {
                        //find OUTPORT
                        if (sys2.BlockType == Constants.OUTPORT)
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