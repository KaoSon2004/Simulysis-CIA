using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface IBranchDAO : IDAO
    {
        long CreateBranch(BranchDTO branch);

        void CreateBranches(ICollection<BranchDTO> branches);

        List<BranchDTO> ReadBranchs(long i_FK_ProjectFileId);
    }
}