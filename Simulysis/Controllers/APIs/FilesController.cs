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
    public class FilesController : ControllerBase
    {
        private static IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        [HttpGet]
        public IActionResult GetFileRelationships(long projId)
        {
            List<ProjectFileDTO> files = projectFileDAO.ReadAllFiles(projId, true);

            return Ok(files);
        }
    }
}
