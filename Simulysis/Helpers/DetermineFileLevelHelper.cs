using DAO.DAO.SqlServerDAO.ProjectFile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities;
using Entities.Logging;
using Entities.Types;

namespace Simulysis.Helpers
{
    public class DetermineFileLevelHelper
    {
        private static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        public class FileNode
        {
            public FileNode Parent { get; set; }
            public long Id { get; set; }

            public FileNode(FileNode parent, long Id)
            {
                this.Parent = parent;
                this.Id = Id;
            }

            public FileNode(long Id)
            {
                this.Id = Id;
            }

        }

        /**
         * We want a Map to manage all filenodes
         * Map<id,FileNode>
         * For each file we will have a corresponding filenode 
         */
        public static void DetermineFileLevel(long projectId, List<FileContent> fileContents)
        {
            Loggers.SVP.Info("DETERMINE FILE LEVEL");

            Dictionary<long, FileNode> fileNodeDictionary = new Dictionary<long, FileNode>();
            FilesRelationshipHelper filesRelationshipHelper = new FilesRelationshipHelper();

            Loggers.SVP.Info("start foreach");

            foreach (var fileContent in fileContents)
            {
                long fileId = fileContent.FileId;

                Loggers.SVP.Info("create filenode for file id : " + fileId);

                FileNode ParentfileNode = new FileNode(fileId); //create a filenode from fileId

                if (!fileNodeDictionary.ContainsKey(fileId))
                {
                    Loggers.SVP.Info("Add parent of " + fileId + "to map");
                    fileNodeDictionary.Add(ParentfileNode.Id, ParentfileNode); //add parent to map
                }

                Loggers.SVP.Info("Find subfile id of " + fileId);

                List<long> subfileId = projectFileDAO.GetSubFileId(projectId, fileId);

                //because we add parent first then we already have parent in the map
                foreach (long sfileId in subfileId)
                {
                    //avoid self-reference file
                    if (sfileId == fileId)
                    {
                        Loggers.SVP.Info("Encounter self ref file => ignore");
                        continue;
                    }

                    //set parent and add to map
                    Loggers.SVP.Info("Set parent and add to map");

                    FileNode childFileNode = new FileNode(fileNodeDictionary[ParentfileNode.Id], sfileId);

                    if (!fileNodeDictionary.ContainsKey(sfileId))
                    {
                        fileNodeDictionary.Add(childFileNode.Id, childFileNode);
                    }
                    else
                    {
                        fileNodeDictionary[sfileId].Parent = fileNodeDictionary[ParentfileNode.Id];
                    }
                }
            }

            Loggers.SVP.Info("Log all files in map");

            foreach (var item in fileNodeDictionary)
            {
                string s = item.Value.Parent == null ? " none (root file) " : item.Value.Parent.Id.ToString();
                Loggers.SVP.Info("file id : " + item.Key + " with parent : " + s);
            }
            /* we've added all filenodes to the map as well as their parents
             * now determine the level
             */
            Loggers.SVP.Info("Start determine level ");
   
            Parallel.ForEach(fileContents,
                new ParallelOptions { MaxDegreeOfParallelism = Configuration.MaxThreadNumber },
                fileContent =>
                {
                    long fileId = fileContent.FileId;

                    FileNode currentFile = fileNodeDictionary[fileId];
                    if (currentFile.Parent != null)
                    {
                        Loggers.SVP.Info("SaveChildParentRelationship of " + fileId + "and" + currentFile.Parent.Id);
                       // filesRelationshipHelper.SaveChildParentRelationship(fileId, currentFile.Parent.Id);
                    }

                    int count = 0;
                    FileNode meetingNode = isCyclic(currentFile);

                    if (meetingNode != null) // then it's a cyclic linked list
                    {
                        Loggers.SVP.Error("Cyclic linked list");
                        //cut off circle at the meeting node
                        FileNode pointer = currentFile;
                        while (pointer.Parent != meetingNode)
                        {
                            pointer = pointer.Parent;
                        }
                        pointer.Parent = null;
                    }

                    while (currentFile.Parent != null)
                    {
                        count++;
                        currentFile = currentFile.Parent;
                    }

                    Loggers.SVP.Info("Update level of " + fileId + " count is " + count);

                    fileContent.FileLevel = projectFileDAO.GetSystemLevel(projectId, fileId, count);
                }
            );

            int ECU_Count = 0;
            // we will count the number of ECU file, if there is only one then we have to decrease the level of each file by one level

            foreach (FileContent fileContent in fileContents)
            {
                if (fileContent.FileLevel.Equals("ECU"))
                {
                    ECU_Count++;
                }
            }

            if (ECU_Count <= 1) fileContents.ForEach(LevelDown);

            foreach (var fileContent in fileContents)
            {
                projectFileDAO.UpdateSystemLevel(projectId, fileContent.FileId, fileContent.FileLevel);
                projectFileDAO.UpdateSystemLevelVariant(projectId, fileContent.FileId, fileContent.FileLevelVariant);
            }
        }

        //isCyClic will return null if the  linked list doesn't contain a cycle otherwise it will return the meeting node
        private static FileNode isCyclic(FileNode head)
        {
            FileNode fast = head;
            FileNode slow = head;

            while (fast != null && fast.Parent != null)
            {
                fast = fast.Parent.Parent;
                slow = slow.Parent;

                //if fast and slow pointers are meeting then linked list is cyclic
                if (fast == slow)
                {
                    return fast;
                }
            }
            return null;
        }

        public static int LevelToCount(string level)
        {
            Dictionary<string,int> fileLevelDict = new Dictionary<string,int>();
            fileLevelDict.Add("ECU", 0);
            fileLevelDict.Add("MFMdl", 1);
            fileLevelDict.Add("Function", 2);
            fileLevelDict.Add("Logic", 3);
            fileLevelDict.Add("Block", 4);
            fileLevelDict.Add("Block Level 5", 5);
            fileLevelDict.Add("Block Level 6", 6);
            fileLevelDict.Add("Block Level 7", 7);
            fileLevelDict.Add("Block Level 8", 8);
            fileLevelDict.Add("Block Level 9", 9);

            return fileLevelDict[level];
        }

        public static string CountToLevel(int count)
        {
            Dictionary<int, string> fileLevelDict = new Dictionary<int, string>();
            fileLevelDict.Add(0, "ECU");
            fileLevelDict.Add(1, "MFMdl");
            fileLevelDict.Add(2, "Function");
            fileLevelDict.Add(3, "Logic");
            fileLevelDict.Add(4, "Block");
            fileLevelDict.Add(5, "Block Level 5");
            fileLevelDict.Add(6, "Block Level 6");
            fileLevelDict.Add(7, "Block Level 7");
            fileLevelDict.Add(8, "Block Level 8");
            fileLevelDict.Add(9, "Block Level 9");

            return fileLevelDict[count];
        }

        private static void LevelDown(FileContent fileContent)
        {
            fileContent.FileLevel = CountToLevel(LevelToCount(fileContent.FileLevel) + 1);
        }
    }
}