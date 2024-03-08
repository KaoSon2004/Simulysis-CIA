using Common;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Simulysis.Controllers.APIs
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class FileRelationshipsController : ControllerBase
    {
        private static IFilesRelationshipDAO fileRelationshipDAO = DAOFactory.GetDAO("IFilesRelationshipDAO") as IFilesRelationshipDAO;
        private static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;
        private static int maxFileListCountArrSize = 20;

        [HttpGet]
        public IActionResult GetFileRelationships(long projectId, long fileId)
        {
            // Find relationships for current file
            List<FilesRelationshipDTO> relationships;
            
            if (fileId != 0)
            {
                relationships = fileRelationshipDAO.ReadFileRelationships(fileId);
            }
            else
            {
                List<long> directChildren = new();

                // Maximum 8 level trace
                for (int i = 0; i < 8; i++)
                {
                    directChildren = projectFileDAO.GetAllFileIDsOfLevel(projectId, projectFileDAO.GetSystemLevel(0, 0, i));
                    if (directChildren.Count > 0)
                    {
                        break;
                    }
                }

                relationships = directChildren.Select(id => new FilesRelationshipDTO(id, 0, 1, FileRelationship.Child_Parent, RelationshipType.From_Go_To)).ToList();
            }

            List<FilesRelationshipDTO> inOutRelationships = relationships.FindAll(relationship =>
                relationship.RelationshipType == RelationshipType.In_Out ||
                relationship.RelationshipType == RelationshipType.From_Go_To
            );

            List<FilesRelationshipDTO> calibrationRelationships =
               relationships.FindAll(relationship => relationship.RelationshipType == RelationshipType.Calibration);


            // Find relationships for other files in dependency view
            IEnumerable<FilesRelationshipDTO> distinctInOuts = inOutRelationships
                .GroupBy(fileRel => new { fileRel.FK_ProjectFileId1, fileRel.FK_ProjectFileId2 })
                .Select(g => g.First());

            var otherInOutRelationships = new Dictionary<string, List<FilesRelationshipDTO>>();
            var pendingInOutChildrenLookup = new Queue<Tuple<long, int>>();

            Parallel.ForEach(
                distinctInOuts,
                new ParallelOptions { MaxDegreeOfParallelism = Entities.Configuration.MaxThreadNumber },
                relationship =>
                {
                    long currentFileId = relationship.FK_ProjectFileId1 == fileId
                        ? relationship.FK_ProjectFileId2
                        : relationship.FK_ProjectFileId1;

                    List<FilesRelationshipDTO> currentRelationships = fileRelationshipDAO.ReadFileRelationships(currentFileId);

                    lock (otherInOutRelationships)
                    {
                        if (otherInOutRelationships.ContainsKey(currentFileId.ToString()))
                        {
                            return;
                        }

                        otherInOutRelationships.Add(
                            currentFileId.ToString(),
                            currentRelationships.FindAll(rel =>
                                rel.RelationshipType == RelationshipType.In_Out ||
                                rel.RelationshipType == RelationshipType.From_Go_To
                            )
                        );
                    }

                    // If the file position in the relationship is not the child!
                    if (((relationship.Type == FileRelationship.Child_Parent) && (relationship.FK_ProjectFileId2 == fileId)) ||
                        (relationship.Type == FileRelationship.Equal))
                    {
                        lock (pendingInOutChildrenLookup)
                        {
                            foreach (FilesRelationshipDTO currentRel in currentRelationships)
                            {
                                if ((currentRel.Type == FileRelationship.Child_Parent) && (currentRel.FK_ProjectFileId2 == currentFileId))
                                {
                                    pendingInOutChildrenLookup.Enqueue(new Tuple<long, int>(currentRel.FK_ProjectFileId1, 0));
                                }
                            }
                        }
                    }
                }
            );

            IEnumerable<FilesRelationshipDTO> distinctCaliRelationships = calibrationRelationships
                .GroupBy(fileRel => new { fileRel.FK_ProjectFileId1, fileRel.FK_ProjectFileId2 })
                .Select(g => g.First());

            var otherCalibrationRelationships = new Dictionary<string, List<FilesRelationshipDTO>>();

            Parallel.ForEach(
                distinctCaliRelationships,
                new ParallelOptions { MaxDegreeOfParallelism = Entities.Configuration.MaxThreadNumber },
                relationship =>
                {
                    long currentFileId = relationship.FK_ProjectFileId1 == fileId
                        ? relationship.FK_ProjectFileId2
                        : relationship.FK_ProjectFileId1;

                    List<FilesRelationshipDTO> currentRelationships = fileRelationshipDAO.ReadFileRelationships(currentFileId);

                    lock (otherCalibrationRelationships)
                    {
                        if (otherCalibrationRelationships.ContainsKey(currentFileId.ToString()))
                        {
                            return;
                        }

                        otherCalibrationRelationships.Add(
                            currentFileId.ToString(),
                            currentRelationships.FindAll(rel => rel.RelationshipType == RelationshipType.Calibration)
                        );
                    }
                }
            );

            // Calculate how many files/rectangles there will be in a system level
            // 0 is for level 2 onwards. Level 0 is main file, level 1 is children of main (main is the one passed to this controller API call)
            var fileCountInOut = Enumerable.Repeat(0, maxFileListCountArrSize).ToList();
            fileCountInOut[0] = pendingInOutChildrenLookup.Count;

            // Search children's children relationship (keep going going going gogogogogogo)
            while (pendingInOutChildrenLookup.Any())
            {
                Tuple<long, int> toLookup = pendingInOutChildrenLookup.Dequeue();
                if (otherInOutRelationships.ContainsKey(toLookup.Item1.ToString()))
                {
                    continue;
                }
                List<FilesRelationshipDTO> currentRelationships = fileRelationshipDAO.ReadFileRelationships(toLookup.Item1);
                List<FilesRelationshipDTO> filteredRelationship = new List<FilesRelationshipDTO>();

                foreach (FilesRelationshipDTO dto in currentRelationships)
                {
                    if ((dto.RelationshipType == RelationshipType.From_Go_To) || (dto.RelationshipType == RelationshipType.In_Out)) {
                        if ((dto.Type == FileRelationship.Child_Parent) && (dto.FK_ProjectFileId2 == toLookup.Item1))
                        {
                            filteredRelationship.Add(dto);
                            pendingInOutChildrenLookup.Enqueue(new Tuple<long, int>(dto.FK_ProjectFileId1, toLookup.Item2 + 1));
                        }
                    }
                }

                fileCountInOut[toLookup.Item2 + 1] += filteredRelationship.Count;
                otherInOutRelationships.Add(toLookup.Item1.ToString(), filteredRelationship);
            }

            var fileCountCali = Enumerable.Repeat<long>(0, maxFileListCountArrSize).ToList();

            return Ok(new
                {
                    mainRelaObj = new
                    {
                        inOutRelationships,
                        calibrationRelationships
                    },
                    subRelaObj = new
                    {
                        inOutRelationships = otherInOutRelationships,
                        calibrationRelationships = otherCalibrationRelationships
                    },
                    fileCountPerLevelObj = new
                    {
                        inOutRelationships = fileCountInOut,
                        calibrationRelationships = fileCountCali
                    }
                }
            );
        }
    }
}