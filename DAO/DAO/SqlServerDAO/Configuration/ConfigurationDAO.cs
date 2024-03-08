using Entities.DTO;
using Entities.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAO.DAO.SqlServerDAO.Configuration
{
    public class ConfigurationDAO : AbstractSqlServerDAO, InterfaceDAO.IConfigurationDAO
    {
        public string ReadLogConfig(string key)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("read_log_level", CommandType.StoredProcedure);
                model.AddParam("i_key", key, MySql.Data.MySqlClient.MySqlDbType.String);

                DataTable dataTable = ExecuteReader(model);
                List<ConfigurationDTO> list = DataTableToListConfig(dataTable);
                ConfigurationDTO configurationDTO = list.Count == 0 ? new ConfigurationDTO() : list[0];

                return configurationDTO.Value;


            }
            catch(Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return null;
            }
            
        }

        public static string ReadLogCongfig(string v)
        {
            throw new NotImplementedException();
        }

        public void SaveLoggingLevel(ConfigurationDTO configurationDTO)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("save_log_level", System.Data.CommandType.StoredProcedure);
                model.AddParam("i_key", configurationDTO.Key, MySql.Data.MySqlClient.MySqlDbType.String,255);
                model.AddParam("i_value", configurationDTO.Value, MySql.Data.MySqlClient.MySqlDbType.String);

                ExecuteNonQuery(model);
               
            }
            catch(Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }
        }

        public List<ConfigurationDTO> DataTableToListConfig(DataTable dataTable)
        {
            List<ConfigurationDTO> list = new List<ConfigurationDTO>();
            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Error("System datatable is null");
                    return new List<ConfigurationDTO>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    ConfigurationDTO resourceTypeModel = new ConfigurationDTO();
                    resourceTypeModel.Key = dataTable.Rows[i]["Key"].ToString();
                    resourceTypeModel.Value = dataTable.Rows[i]["Value"].ToString();

                    list.Add(resourceTypeModel);
                }

            }
            catch(Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<ConfigurationDTO>();
            }
            return list;
        }
    }
}
