using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface IProjectFileDAO : IDAO
    {
        long CreateProjectFile(ProjectFileDTO projectFile);
        List<ProjectFileDTO> ReadAllFiles(long projectId, bool includeVirtualHiddenFiles = false);

        long DeleteProjectFile(long Id);
        ProjectFileDTO ReadFileById(long id);

        void ChangeDescriptionAndSystemLevel(long id, string Description, string SystemLevel);
        List<long> GetSubFileId(long projectId, long projectFileId);

        string GetSystemLevel(long projectId, long projectFileId, int count);

        void UpdateStatus(ProjectFileDTO projectFile);

        List<long> GetFileIdList(long ProjectId);

        List<long> GetAllFileIDsOfLevel(long projectId, string level);

        ProjectFileDTO ReadFileByName(string fileName, long projectId);

        List<ProjectFileDTO> FindFilesByFileLevel(string fileLevel, long projectId);

        void UpdateSystemLevel(long projectId, long projectFileId, string systemLevel);

        void GetAndUpdateSystemLevel(long projectId, long projectFileId, int count);

        void UpdateSystemLevelVariant(long projectId, long projectFileId, string variant);
    }
}