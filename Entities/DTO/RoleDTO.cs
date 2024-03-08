using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Entities.DTO
{
    public class RoleDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public virtual ICollection<UserDTO> Users { get; set; }

        public RoleDTO(long roleId, string roleName)
        {
            Id = roleId;
            Name = roleName;
        }

        public RoleDTO()
        { }
    }
}
