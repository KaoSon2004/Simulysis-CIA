using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAO.DAO.InterfaceDAO
{
    public interface IRoleDAO : IDAO
    {
        List<RoleDTO> ReadAllRole();
    }
}
