using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using Entities.Logging;
using MySql.Data.MySqlClient;

namespace DAO.DAO.SqlServerDAO.Calibration
{
    public class CalibrationDAO : AbstractSqlServerDAO, ICalibrationDAO
    {
        public void CreateCalibrations(ICollection<CalibrationDTO> calibrations)
        {
            if (calibrations.Count == 0)
            {
                return;
            }

            Loggers.SVP.Info($"Create calibrations");

            using (MySqlConnection conn = new MySqlConnection())
            {
                conn.ConnectionString = Entities.Configuration.ConnectionString;
                conn.Open();
                StringBuilder stringBuilder =
                    new StringBuilder(
                        "INSERT INTO Calibration (Name, Value, Description, DataType, FK_ProjectId) VALUES");

                MySqlCommand cmd = new MySqlCommand();

                long counter = 0;

                foreach (CalibrationDTO calibration in calibrations)
                {
                    stringBuilder.Append(
                        $"(@Name{counter}, @Value{counter}, @Des{counter}, @DataT{counter}, @ProjId{counter}),");
                    cmd.Parameters.Add(new MySqlParameter($"id{counter}", calibration.Id));
                    cmd.Parameters.Add(new MySqlParameter($"Name{counter}", calibration.Name));
                    cmd.Parameters.Add(new MySqlParameter($"Value{counter}", calibration.Value));
                    cmd.Parameters.Add(new MySqlParameter($"Des{counter}", calibration.Description));
                    cmd.Parameters.Add(new MySqlParameter($"DataT{counter}", calibration.DataType));
                    cmd.Parameters.Add(new MySqlParameter($"ProjId{counter}", calibration.FK_ProjectId));
                    counter++;
                }

                cmd.CommandText = string.Concat(stringBuilder.ToString().TrimEnd(','), ";");
                cmd.Connection = conn;
                cmd.CommandTimeout = Entities.Configuration.SQLCommandTimeOut;

                MySqlCommand timeoutCmd = new MySqlCommand("set net_write_timeout=99999; set net_read_timeout=99999", conn);
                timeoutCmd.ExecuteNonQuery();
                Loggers.SVP.Info("net_write/read_timeout SET for calibrations");

                try
                {
                    cmd.ExecuteNonQuery();
                    Loggers.SVP.Info($"INSERTED {calibrations.Count} CALIBRATIONS");
                }
                catch (Exception e)
                {
                    string query = cmd.CommandText;

                    foreach (MySqlParameter parameter in cmd.Parameters)
                    {
                        query = Regex.Replace(
                            query,
                            $@"\b{parameter.ParameterName}\b",
                            parameter.Value != null ? parameter.Value.ToString() : "NULL"
                        );
                    }

                    query = query.Replace("@", "");

                    Loggers.SVP.Info($"FILE VERSION: {VersionUtils.GetFileVersion()}");
                    Loggers.SVP.Info(
                        $"****************CALIBRATION SQL COMMAND****************\n{query}\n"
                    );
                    Loggers.SVP.RecurException(e.Message, e);
                    throw e;
                }
            }
        }

        //public List<CalibrationDTO> ReadCalibrations(long projectId)
        //{
        //    try
        //    {
        //        CommandExecutionModel model = new CommandExecutionModel();

        //        model.SetCommand("read_calibrations", CommandType.StoredProcedure);
        //        model.AddParam("projectId", projectId, MySqlDbType.Int64);

        //        DataTable dataTable = ExecuteReader(model);

        //        List<CalibrationDTO> list = new List<CalibrationDTO>();
        //        list = DataTableToListCalibration(dataTable);

        //        return list;
        //    }
        //    catch (Exception ex)
        //    {
        //        Loggers.SVP.Exception(ex.Message, ex);
        //        return null;
        //    }
        //}

        public List<CalibrationDTO> SearchCalibrationByName(string calibration, long projectId)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();

                model.SetCommand("search_calibration_by_name", CommandType.StoredProcedure);
                model.AddParam("Name", calibration, MySqlDbType.VarChar);
                model.AddParam("FK_ProjectId", projectId, MySqlDbType.Int64);

                DataTable dataTable = ExecuteReader(model);

                List<CalibrationDTO> list = new List<CalibrationDTO>();
                list = DataTableToListCalibration(dataTable);

                return list;
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return null;
            }
        }

        public List<CalibrationDTO> DataTableToListCalibration(DataTable dataTable)
        {
            List<CalibrationDTO> list = new List<CalibrationDTO>();

            try
            {
                if (dataTable == null)
                {
                    Loggers.SVP.Error("calibration data table is null");
                    return new List<CalibrationDTO>();
                }

                for (int i = 0; i < dataTable.Rows.Count; i++)
                {
                    CalibrationDTO resourceTypeModel = new CalibrationDTO();
                    resourceTypeModel.Id = Convert.ToInt64(dataTable.Rows[i]["Id"]);
                    resourceTypeModel.Name = dataTable.Rows[i]["Name"].ToString();
                    resourceTypeModel.Value = Convert.ToDecimal(dataTable.Rows[i]["Value"]);
                    resourceTypeModel.Description = dataTable.Rows[i]["Description"].ToString();
                    resourceTypeModel.DataType = dataTable.Rows[i]["DataType"].ToString();
                    resourceTypeModel.FK_ProjectId = Convert.ToInt64(dataTable.Rows[i]["FK_ProjectId"].ToString());

                    list.Add(resourceTypeModel);
                }
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                return new List<CalibrationDTO>();
            }

            return list;
        }
    }
}