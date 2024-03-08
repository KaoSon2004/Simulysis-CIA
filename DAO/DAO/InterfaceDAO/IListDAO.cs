using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface IListDAO : IDAO
    {
        long CreateList(ListDTO list);

        void CreateLists(ICollection<ListDTO> lists);

        List<ListDTO> ReadLists(long FK_SystemId);
    }
}