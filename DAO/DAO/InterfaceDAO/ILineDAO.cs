using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface ILineDAO : IDAO
    {
        long CreateLine(LineDTO line);

        void CreateLines(ICollection<LineDTO> lines);

        List<LineDTO> ReadLines(long FK_SystemId);
    }
}