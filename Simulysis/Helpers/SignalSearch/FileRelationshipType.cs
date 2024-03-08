using Common;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using System.Collections.Generic;
using System.Linq;

namespace Simulysis.Helpers.SignalSearch
{
    public class FileRelationshipType
    {
        private static IFilesRelationshipDAO filesRelationshipDAO = DAOFactory.GetDAO("IFilesRelationshipDAO") as IFilesRelationshipDAO;
        private static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        public List<Signal> Search(long fileId1, long fileId2, RelationshipType type)
        {
            List<FilesRelationshipDTO> relationships = filesRelationshipDAO.ReadFilesRelationship(fileId1, fileId2, type);
            ProjectFileDTO file1 = projectFileDAO.ReadFileById(fileId1);
            ProjectFileDTO file2 = projectFileDAO.ReadFileById(fileId2);


            List<Signal> results = new List<Signal>();
            foreach (var relationship in relationships)
            {
                results.Add(new Signal(
                    relationship.Name,
                    $"{file1.Name}|{relationship.System1 ?? ""}",
                    $"{file2.Name}|{relationship.System2 ?? ""}"
                ));
            }

            return results.Distinct().ToList();
        }
    }
}