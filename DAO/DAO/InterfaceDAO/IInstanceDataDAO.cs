using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Entities.DTO;

namespace DAO.DAO.InterfaceDAO
{
    public interface IInstanceDataDAO : IDAO
    {
        long CreateInstanceData(InstanceDataDTO instanceData);
        void CreateInstanceDatas(ICollection<InstanceDataDTO> instanceDatas);

        List<InstanceDataDTO> ReadInstanceDatas(long i_FK_ProjectFileId);
    }
}