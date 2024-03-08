using Common;
using DAO.DAO.Factory;
using DAO.DAO.InterfaceDAO;
using Entities.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Simulysis.Helpers.SignalSearch;

namespace Simulysis.Controllers.APIs
{
    [AllowAnonymous]
    [ApiController]
    [Route("api/[controller]")]
    public class SearchController : ControllerBase
    {
        private static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        [HttpGet]
        public IActionResult GetSearchView(
            string name,
            string scope,
            string type,
            long projectId,
            string fileList
        )
        {
            Loggers.SVP.Info("Begin GetSearchView");
            List<long> fileIdList = JsonConvert.DeserializeObject<List<long>>(fileList);

            if (!scope.Equals(Constants.IN_VIEW_SCOPE) && !scope.Equals(Constants.IN_PROJECT_SCOPE))
            {
                return BadRequest($"Unknown search scope: {scope}");
            }
            Loggers.SVP.Info("Before switch (type)");
            switch (type)
            {
                case Constants.INOUT_VIEW_TYPE:
                    SignalSearcher signalSearcherInOut = new SignalSearcher(new InportOutportType(scope.Equals(Constants.IN_VIEW_SCOPE)));
                    Loggers.SVP.Info("Before signalSearcherInOut.Search");
                    List<Signal> signalInOut = signalSearcherInOut.Search(
                        new SearchInput
                        {
                            Name = name ?? "",
                            ProjectId = projectId,
                            ProjectFileIdsSet = fileIdList
                        }
                    );
                    Loggers.SVP.Info("After signalSearcherInOut.Search");

                    SignalSearcher signalSearcherFromGoto = new SignalSearcher(new FromGoToType(scope.Equals(Constants.IN_VIEW_SCOPE)));
                    Loggers.SVP.Info("Before signalSearcherFromGoto.Search");
                    List<Signal> signalFromGoto = signalSearcherFromGoto.Search(
                        new SearchInput
                        {
                            Name = name ?? "",
                            ProjectId = projectId,
                            ProjectFileIdsSet = fileIdList
                        }
                    );

                    Loggers.SVP.Info("After signalSearcherFromGoto.Search");
                    Loggers.SVP.Info("After Parallel.ForEach(filesToSearch,");
                    Loggers.SVP.Info("End GetSearchView 1");

                    return Ok(new { signalInOut, signalFromGoto });
                case Constants.CALI_VIEW_TYPE:
                    SignalSearcher signalSearcherCali = new SignalSearcher(new CalibrationType(scope.Equals(Constants.IN_VIEW_SCOPE)));

                    List<Signal> signalCali = signalSearcherCali.Search(
                        new SearchInput
                        {
                            Name = name ?? "",
                            ProjectId = projectId,
                            ProjectFileIdsSet = fileIdList
                        }
                    );
                    Loggers.SVP.Info("End GetSearchView 2");
                    return Ok(new {signalCali});
                default:
                    Loggers.SVP.Info("End GetSearchView 3");
                    return BadRequest($"Unknown search type: {type}");
            }
            
        }

        public IActionResult GetRelationshipBetweenTwoFiles(string type, long fileId1, long fileId2)
        {
            switch (type)
            {
                case Constants.INOUT_VIEW_TYPE:
                    List<Signal> signalInOut = new FileRelationshipType().Search(fileId1, fileId2, RelationshipType.In_Out);
                    Loggers.SVP.Info("End In-Out Relationship Signal search");


                    List<Signal> signalFromGoto = new FileRelationshipType().Search(fileId1, fileId2, RelationshipType.From_Go_To);

                    Loggers.SVP.Info("End From-Goto Relationship Signal search");

                    return Ok(new { signalInOut, signalFromGoto });
                case Constants.CALI_VIEW_TYPE:
                    List<Signal> signalCali = new FileRelationshipType().Search(fileId1, fileId2, RelationshipType.Calibration);

                    Loggers.SVP.Info("End Calibration Relationship Signal search");
                    return Ok(new { signalCali });
                default:
                    Loggers.SVP.Info($"Unknown search type: {type}");
                    return BadRequest($"Unknown search type: {type}");
            }
        }
    }
}