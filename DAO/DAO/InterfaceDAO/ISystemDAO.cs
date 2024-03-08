using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface ISystemDAO : IDAO
    {
        //long CreateSystem(SystemDTO system);

        void CreateSystems(ICollection<SystemDTO> systems);

        List<SystemDTO> ReadSystems(long FK_ProjectFileId);

        List<SystemDTO> SearchSystemByCalibrationInASetOfFiles(string calibration, List<long> idList, string dataType);

        List<SystemDTO> SearchSystemByCalibrationInAFile(string calibration, long projectFileId, string dataType);

        List<SystemDTO> SearchSystemByCalibrationInProject(string calibration, long projectId, string dataType);

        Queue<SystemDTO> GetSystemAndParentAndSubSysChildren(long fileId, long systemId, long parentId);
    }
}