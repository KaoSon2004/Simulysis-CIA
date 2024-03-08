using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface IPortDAO : IDAO
    {
        long CreatePort(PortDTO port);

        void CreatePorts(ICollection<PortDTO> ports);

        List<PortDTO> ReadPorts(long FK_ProjectFileId);
    }
}