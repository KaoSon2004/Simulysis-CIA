using Common;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Simulysis.Helpers
{
    public class Reader
    {
        protected static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        protected List<SystemDTO> systemDTOs = new List<SystemDTO>((int)InitialCapacity.System);
        protected List<PortDTO> portDTOs = new List<PortDTO>((int)InitialCapacity.Other);
        protected List<LineDTO> lineDTOs = new List<LineDTO>((int)InitialCapacity.Line);
        protected List<BranchDTO> branchDTOs = new List<BranchDTO>((int)InitialCapacity.Other);
        protected List<ListDTO> listDTOs = new List<ListDTO>((int)InitialCapacity.List);
        protected List<InstanceDataDTO> instanceDataDTOs = new List<InstanceDataDTO>((int)InitialCapacity.Other);

        protected long nextSystemId = 1;
        protected long nextLineId = 1;
        protected long nextBranchId = 1;
        protected long nextPortId = 1;
        protected long nextInstanceDataId = 1;
        protected long nextListId = 1;

        protected bool errIsLogged;
        protected string fileName;

        protected string discoveredFileVariant;

      
        public static void AddFromGotoConnectedSys(List<SystemDTO> systemDTOs,List<LineDTO> lineDTOs)
        {
            systemDTOs.ForEach(system =>
            {
                if (!system.BlockType.Equals(Constants.FROM) && !system.BlockType.Equals(Constants.GOTO)) return;

                var connectedSystem = GetSystemConnectedTo(systemDTOs, lineDTOs, system);

                system.ConnectedRefSys = connectedSystem;

                if (connectedSystem == null)
                {
                    system.ConnectedRefSrcFile = "";
                    return;
                }

                bool isRef = connectedSystem.BlockType.Equals(Constants.REF);

                system.ConnectedRefSrcFile = isRef ? connectedSystem.SourceFile : system.ContainingFile;
            });
        }


        private static SystemDTO GetSystemConnectedTo(List<SystemDTO> fileSystems, List<LineDTO> fileLines, SystemDTO block)
        {
            string lineSearch = block.BlockType.Equals(Constants.FROM) ? "out" : "in";

            LineDTO connectedLine = fileLines.Find(
                line =>
                    line.FK_SystemId == block.FK_ParentSystemId &&
                    (line.Properties.Contains(block.Name) || line.Properties.Contains($"{block.SID}#{lineSearch}"))
            );

            if (connectedLine == null)
            {
                Loggers.SVP.Warning($"Cannot find the line connected with {block.Name} block");

                return null;
            }

            string newSystemSearch = block.BlockType.Equals(Constants.FROM) ? "in" : "out";
            string oldSystemSearch = block.BlockType.Equals(Constants.FROM) ? "DstBlock" : "SrcBlock";

            var refSys = fileSystems.Find(
                system =>
                    system.FK_ParentSystemId == block.FK_ParentSystemId &&
                    (
                        connectedLine.Properties.Contains($"\"{oldSystemSearch}\":\"{system.Name}\"") ||
                        connectedLine.Properties.Contains($"{system.SID}#{newSystemSearch}")
                    )
            );

            if (refSys == null)
            {
                Loggers.SVP.Warning($"Cannot find the block connected with {block.Name} block");

                return null;
            }

            return refSys;
        }
    }
}