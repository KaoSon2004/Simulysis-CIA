using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface IProjectDAO : IDAO
    {
        long CreateProject(ProjectDTO project);

        ProjectDTO ReadProjectByName(string name);

        List<ProjectDTO> ReadAllProjects();

        List<ProjectDTO> GetProjectVersions(long projectId);

        long DeleteProject(long id);

        ProjectDTO ReadProjectById(long id);
        void UpdateProjectDescription(long projectId, string description);

        long DeleteProject_V2(long id);
        void SetProjectGitLink(long projectId, string gitLink);
    }
}
