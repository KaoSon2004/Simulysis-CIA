using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Entities.Types
{
    public class FileContent
    {
        //public Dictionary<string, string> ModelRefs { get; set; }
        public string FileName { get; set; }
        public long FileId { get; set; }
        public string FileLevel { get; set; }

        // NOTE: Saved when FileLevel is save (DetermineFileHelper). Reason: convenience
        public string FileLevelVariant { get; set; }
        public List<SystemDTO> Systems { get; set; }
        public List<LineDTO> Lines { get; set; }
        public List<PortDTO> Ports { get; set; }
        public List<BranchDTO> Branches { get; set; }
        public List<InstanceDataDTO> InstanceDatas { get; set; }
        public List<ListDTO> Lists { get; set; }

        public FileContent()
        {
            Systems = new List<SystemDTO>();
            Lines = new List<LineDTO>();
            Ports = new List<PortDTO>();
            Branches = new List<BranchDTO>();
            Lists = new List<ListDTO>();
            InstanceDatas = new List<InstanceDataDTO>();

        }
        
        public static List<FileContent> fileContentList = new List<FileContent>();

       
    }
}