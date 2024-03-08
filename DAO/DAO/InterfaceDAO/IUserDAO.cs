using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAO.DAO.InterfaceDAO
{
    public interface IUserDAO : IDAO
    {
        long CreateUser(UserDTO accountDTO);

        UserDTO ReadUser(string username);

        UserDTO ReadUserNameByEmail(string email);

        long ActivateAccount(string activationCode);

        List<UserDTO> ReadAllUser();
    }
}
