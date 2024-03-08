using DAO.DAO.SqlServerDAO.ProjectFile;
using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Common;

namespace Simulysis.Helpers
{
    public class FilesRelationshipHelper
    {
        DAO.DAO.InterfaceDAO.IFilesRelationshipDAO filesRelationshipDAO = new FilesRelationshipDAO();
        private const int PARENT_CHILD_RELATIONSHIP_COUNT = 1;

        public void SaveParentChildRelationship(long FK_ProjectFileId1, long FK_ProjectFileId2)
        {
            FilesRelationshipDTO filesRelationshipDTO = new FilesRelationshipDTO(
                FK_ProjectFileId1,
                FK_ProjectFileId2,
                PARENT_CHILD_RELATIONSHIP_COUNT,
                FileRelationship.Parent_Child,
                RelationshipType.In_Out
            );
            filesRelationshipDAO.CreateFilesRelationship(filesRelationshipDTO);
        }

        public void SaveChildParentRelationship(long FK_ProjectFileId1, long FK_ProjectFileId2)
        {
            FilesRelationshipDTO filesRelationshipDTO = new FilesRelationshipDTO(
                FK_ProjectFileId1,
                FK_ProjectFileId2,
                PARENT_CHILD_RELATIONSHIP_COUNT,
                FileRelationship.Child_Parent,
                RelationshipType.In_Out
            );
            filesRelationshipDAO.CreateFilesRelationship(filesRelationshipDTO);
        }
    }
}