using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.DTO
{
    public class UserDTO : AbstractDTO
    {
        string _username = "";
        string _encryptedPassword = "";

        public string Username { get => _username; set => _username = value; }
        public string EncryptedPassword { get => _encryptedPassword; set => _encryptedPassword = value; }

        public long Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        //public string Email { get; set; }
        public string Password { get; set; }
        //public bool IsActive { get; set; }
        public virtual ICollection<RoleDTO> Roles { get; set; }
        
        //public Guid ActivationCode { get; set; }

        public long FK_RoleId { get; set; }

        public UserDTO()
        {

        }
    }
}
