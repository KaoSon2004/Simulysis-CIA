using CsvHelper;
using CsvHelper.Configuration;
using DAO.DAO.SqlServerDAO.FileContent;
using Entities.ClassMapping;
using Entities.DTO;
using Entities.Logging;
using Google.Protobuf.Collections;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web;


namespace Simulysis.Helpers.CSVBulkLoader
{
    public class CsvGenerator
    {
       public string genCSVForSystem(List<SystemDTO> systemDTOs, string projectPath)
        {
            Loggers.SVP.Info("Gen csv for system table");
            var csvFolderPath = Path.Combine(projectPath, "CSV");

            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }

            string csvPath = Path.Combine(csvFolderPath, "system.csv");

            using (var streamWriter = new StreamWriter(csvPath))
            {
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.Context.RegisterClassMap<SystemMap>();
                    csvWriter.WriteRecords(systemDTOs);
                }
            }
            Loggers.SVP.Info("Done gen csv for system table, "+ "total lines is "+ systemDTOs.Count());
            return csvPath;
       }

        public string genCSVForBranch(List<BranchDTO> branches, string projectPath)
        {
            Loggers.SVP.Info("Gen csv for branch table");
            var csvFolderPath = Path.Combine(projectPath, "CSV");

            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }

            string csvPath = Path.Combine(csvFolderPath, "branch.csv");

            using (var streamWriter = new StreamWriter(csvPath))
            {
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(branches);
                }
            }
            Loggers.SVP.Info("Done gen csv for branch table, " + "total lines is " + branches.Count());
            return csvPath;
        }

        public string genCSVForList(List<ListDTO> lists, string projectPath)
        {
            Loggers.SVP.Info("Gen csv for list table");
            var csvFolderPath = Path.Combine(projectPath, "CSV");

            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }

            string csvPath = Path.Combine(csvFolderPath, "list.csv");

            using (var streamWriter = new StreamWriter(csvPath))
            {
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.Context.RegisterClassMap<SystemMap>();
                    csvWriter.WriteRecords(lists);
                }
            }
            Loggers.SVP.Info("Done gen csv for list table, " + "total lines is " + lists.Count());
            return csvPath;
        }

        public string genCSVForLine(List<LineDTO> lineDTOs, string projectPath)
        {
            Loggers.SVP.Info("Gen csv for line table");
            var csvFolderPath = Path.Combine(projectPath, "CSV");

            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }

            string csvPath = Path.Combine(csvFolderPath, "line.csv");

            using (var streamWriter = new StreamWriter(csvPath))
            {
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(lineDTOs);
                }
            }
            Loggers.SVP.Info("Done gen csv for line table, " + "total lines is " + lineDTOs.Count());
            return csvPath;
        }

        public string genCSVForPort(List<PortDTO> portDTOs, string projectPath)
        {
            Loggers.SVP.Info("Gen csv for port table");
            var csvFolderPath = Path.Combine(projectPath, "CSV");

            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }

            string csvPath = Path.Combine(csvFolderPath, "port.csv");

            using (var streamWriter = new StreamWriter(csvPath))
            {
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(portDTOs);
                }
            }
            Loggers.SVP.Info("Done gen csv for port table, " + "total lines is " + portDTOs.Count());
            return csvPath;
        }

        public string genCSVForInstanceData(List<InstanceDataDTO> instanceDataDTOs, string projectPath)
        {
            Loggers.SVP.Info("Gen csv for instancedata table");
            var csvFolderPath = Path.Combine(projectPath, "CSV");

            if (!Directory.Exists(csvFolderPath))
            {
                Directory.CreateDirectory(csvFolderPath);
            }

            string csvPath = Path.Combine(csvFolderPath, "instancedata.csv");

            using (var streamWriter = new StreamWriter(csvPath))
            {
                using (var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture))
                {
                    csvWriter.WriteRecords(instanceDataDTOs);
                }
            }
            Loggers.SVP.Info("Done gen csv for instance data table, " + "total lines is " + instanceDataDTOs.Count());
            return csvPath;
        }


    }
}