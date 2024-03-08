using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface ICalibrationDAO : IDAO
    {
        void CreateCalibrations(ICollection<CalibrationDTO> calibrations);
        List<CalibrationDTO> SearchCalibrationByName(string calibration, long projectId);
    }
}