using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAO.DAO.InterfaceDAO
{
    public interface IConfigurationDAO :IDAO
    {
        void SaveLoggingLevel(ConfigurationDTO configurationDTO);

        string ReadLogConfig(String key);
       
    }
}
