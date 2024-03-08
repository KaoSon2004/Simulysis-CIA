using Common;
using Entities.DTO;
using System.Collections.Generic;

namespace DAO.DAO.InterfaceDAO
{
    public interface IFilesRelationshipDAO : IDAO
    {
        long CreateFilesRelationship(FilesRelationshipDTO filesRelationshipDTO);
        void CreateFilesRelationships(ICollection<FilesRelationshipDTO> fileRels);
        List<FilesRelationshipDTO> ReadFilesRelationship(long fileId1, long fileId2, RelationshipType type);
        List<FilesRelationshipDTO> ReadFileRelationships(long fileId);
    }
}