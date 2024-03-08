using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class FilesRelationshipDTO : AbstractDTO
    {
        public long Id { get; set; }
        public long FK_ProjectFileId1 { get; set; }
        public long FK_ProjectFileId2 { get; set; }
        public string System1 { get; set; }
        public string System2 { get; set; }
        public int Count { get; set; }
        public int UniCount { get; set; }
        public FileRelationship Type { get; set; }
        public RelationshipType RelationshipType { get; set; }

        public string Name { get; set; }

        public FilesRelationshipDTO(long fileId1, long fileId2, int count, FileRelationship type, RelationshipType relationshipType)
        {
            FK_ProjectFileId1 = fileId1;
            FK_ProjectFileId2 = fileId2;
            Count = count;
            Type = type;
            RelationshipType = relationshipType;
        }

        public FilesRelationshipDTO(FilesRelationshipDTO other)
        {
            Id = other.Id;
            FK_ProjectFileId1 = other.FK_ProjectFileId1;
            FK_ProjectFileId2 = other.FK_ProjectFileId2;
            System1 = other.System1;
            System2 = other.System2;
            Count = other.Count;
            UniCount = other.UniCount;
            Type = other.Type;
            RelationshipType = other.RelationshipType;
            Name = other.Name;
        }

        public FilesRelationshipDTO()
        {
        }
    }
}