using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.DTO
{
    public class ProjectDTO : AbstractDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }

        public string Description { get; set; }

        public string Path { get; set; }

        public long BaseProjectId { get; set; }

        public string Version { get; set; }

        public string SourceLink { get; set; }

        public object RouteValue()
        {
            return new { projectId = Id };
        }
    }
}
