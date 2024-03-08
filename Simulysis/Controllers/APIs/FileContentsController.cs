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
    public class FileContentsController : ControllerBase
    {
        IProjectFileDAO projectFileDAO = DAOFactory.GetDAO("IProjectFileDAO") as IProjectFileDAO;

        ISystemDAO systemDAO = DAOFactory.GetDAO("ISystemDAO") as ISystemDAO;
        ILineDAO lineDAO = DAOFactory.GetDAO("ILineDAO") as ILineDAO;
        IListDAO listDAO = DAOFactory.GetDAO("IListDAO") as IListDAO;
        IInstanceDataDAO instanceDataDAO = DAOFactory.GetDAO("IInstanceDataDAO") as IInstanceDataDAO;
        IPortDAO portDAO = DAOFactory.GetDAO("IPortDAO") as IPortDAO;
        IBranchDAO branchDAO = DAOFactory.GetDAO("IBranchDAO") as IBranchDAO;

        [HttpGet]
        [Route("{id}")]
        public IActionResult GetFileContents(long id)
        {
            List<SystemDTO> systems = null;
            List<LineDTO> lines = null;
            List<ListDTO> lists = null;
            List<InstanceDataDTO> instanceDatas = null;
            List<PortDTO> ports = null;
            List<BranchDTO> branches = null;

            Task getSystems = Task.Factory.StartNew(() => systems = systemDAO.ReadSystems(id));
            Task getLines = Task.Factory.StartNew(() => lines = lineDAO.ReadLines(id));
            Task getLists = Task.Factory.StartNew(() => lists = listDAO.ReadLists(id));
            Task getInstanceDatas = Task.Factory.StartNew(() => instanceDatas = instanceDataDAO.ReadInstanceDatas(id));
            Task getPorts = Task.Factory.StartNew(() => ports = portDAO.ReadPorts(id));
            Task getBranches = Task.Factory.StartNew(() => branches = branchDAO.ReadBranchs(id));

            Task.WaitAll(getSystems, getLines, getLists, getInstanceDatas, getPorts, getBranches);

            return Ok(
               new
               {
                   systems,
                   lines,
                   lists,
                   instanceDatas,
                   ports,
                   branches,
                   fileId = id
               }
           );
        }

        [HttpGet]
        public IActionResult GetFileContentsByName(long projId, string fileName)
        {
            ProjectFileDTO file = projectFileDAO.ReadFileByName(fileName, projId);

            List<SystemDTO> systems = null;
            List<LineDTO> lines = null;
            List<ListDTO> lists = null;
            List<InstanceDataDTO> instanceDatas = null;
            List<PortDTO> ports = null;
            List<BranchDTO> branches = null;

            Task getSystems = Task.Factory.StartNew(() => systems = systemDAO.ReadSystems(file.Id));
            Task getLines = Task.Factory.StartNew(() => lines = lineDAO.ReadLines(file.Id));
            Task getLists = Task.Factory.StartNew(() => lists = listDAO.ReadLists(file.Id));
            Task getInstanceDatas = Task.Factory.StartNew(() => instanceDatas = instanceDataDAO.ReadInstanceDatas(file.Id));
            Task getPorts = Task.Factory.StartNew(() => ports = portDAO.ReadPorts(file.Id));
            Task getBranches = Task.Factory.StartNew(() => branches = branchDAO.ReadBranchs(file.Id));

            Task.WaitAll(getSystems, getLines, getLists, getInstanceDatas, getPorts, getBranches);

            return Ok(
                new
                {
                    systems,
                    lines,
                    lists,
                    instanceDatas,
                    ports,
                    branches,
                    fileId = file.Id
                }
            );
        }
    }
}