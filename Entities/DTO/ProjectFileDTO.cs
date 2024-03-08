using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class ProjectFileDTO : AbstractDTO
    {
        public long Id { get; set; }

        public string Name { get; set; }

        public string Path { get; set; }

        public string Description { get; set; }

        public long FK_ProjectId { get; set; }

        public string MatlabVersion { get; set; }

        public string SystemLevel { get; set; }
        public string LevelVariant { get; set; }
        public bool Status { get; set; }

        public string StatusDisplay => Status ? "Succeeded" : "Failed";

        public string ErrorMsg { get; set; }

        public object RouteValue()
        {
            return new {projectId = FK_ProjectId, fileId = Id};
        }

        public ProjectFileDTO()
        {
        }
    }
}