using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;

namespace Simulysis.Helpers.SignalSearch
{
    public class CalibrationType : SignalSearchStrategy
    {
        protected static ISystemDAO systemDAO = DAOFactory.GetDAO("ISystemDAO") as ISystemDAO;
        protected static ICalibrationDAO calibrationDAO = DAOFactory.GetDAO("ICalibrationDAO") as ICalibrationDAO;

        private bool inSetOfFiles = true;

        public CalibrationType(bool inSetOfFiles)
        {
            this.inSetOfFiles = inSetOfFiles;
        }

        public override List<Signal> Search(SearchInput input)
        {
            input.ProjectFileIdsSet = input.ProjectFileIdsSet.Distinct().ToList();
            List<CalibrationDTO> calibrations = calibrationDAO.SearchCalibrationByName(input.Name, input.ProjectId);

            List<Signal> results = new List<Signal>();
            foreach (var calibration in calibrations)
            {
                var foundList = inSetOfFiles ? systemDAO.SearchSystemByCalibrationInASetOfFiles(calibration.Name, input.ProjectFileIdsSet, calibration.DataType)
                        : systemDAO.SearchSystemByCalibrationInProject(calibration.Name, input.ProjectId, calibration.DataType);

                results.AddRange(ProcessResult(foundList, calibration.Name));
            }

            if (inSetOfFiles)
            {
                Loggers.SVP.Info("Searched Calibration In View: ");
            } else
            {
                Loggers.SVP.Info("Searched Calibration In Project: ");
            }

            results.Distinct().ToList().ForEach(p =>
            {
                Loggers.SVP.Info("From: " + p.From + " " + "To: " + p.To + " " + "Name: " + p.Name);
            });
            return results.Distinct().ToList();
        }

        protected static List<Signal> ProcessResult(List<SystemDTO> foundList, string calibrationName)
        {
            List<Signal> results = new List<Signal>();

            if (foundList.Count == 0)
            {
                return new List<Signal>();
            }

            if (foundList.Count == 1)
            {
                results.Add(new Signal(calibrationName, foundList[0].ContainingFile, ""));
            }

            foreach (var sys1 in foundList)
            {
                foreach (var sys2 in foundList)
                {
                    if (sys1.Id == sys2.Id && sys1.FK_ProjectFileId == sys2.FK_ProjectFileId)
                    {
                        continue;
                    }

                    // check for reverse duplicate
                    if (!IsReverseDuplicate(results, calibrationName, sys1.ContainingFile, sys2.ContainingFile))
                    {
                        results.Add(new Signal(calibrationName, sys1.ContainingFile, sys2.ContainingFile));
                    }
                }
            }

            return results;
        }

        private static bool IsReverseDuplicate(List<Signal> results, string caliName, string file1, string file2)
        {
            return results.Exists(
                result => result.Name.Equals(caliName) && result.From.Equals(file2) && result.To.Equals(file1)
            );
        }
    }
}