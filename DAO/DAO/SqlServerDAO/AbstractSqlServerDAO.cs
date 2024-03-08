using System;
using System.Data;
//using System.Data.SqlClient;
using MySql.Data.MySqlClient;
using Entities;
using Entities.Logging;

namespace DAO.DAO
{
    /// <summary>
    /// AbstractMySqlServerDAO.
    /// </summary>
    public abstract class AbstractSqlServerDAO
    {
        private const string COULD_NOT_CREATE_COMMAND_OR_CONNECTION = "Error creating connection or command.";
        private const string COULD_NOT_CLOSE_CONNECTION = "Error closing connection.";
        private const string ERROR_EXECUTING_COMMAND = "Error executing command.";


        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractSqlServerConfigDAO"/> class.
        /// </summary>
        public AbstractSqlServerDAO()
        {
        }

        protected MySqlConnection CreateConnection()
        {
            try
            {
                MySqlConnection connectionSQLSever = null;
                connectionSQLSever = new MySqlConnection(Configuration.ConnectionString);

                return connectionSQLSever;
            }
            catch (MySqlException se)
            {
                //Loggers.DigitalSignManager.Exception("Call AbstractMySqlServerDAO() Exception:", se);
                throw new Exception(COULD_NOT_CREATE_COMMAND_OR_CONNECTION, se);
            }
        }

        protected DataTable ExecuteReader(CommandExecutionModel model)
        {
            if (model == null) { throw new ArgumentNullException("CommandExecutionModel cannot be null"); }

            MySqlConnection connectionSQLSever = CreateConnection();
            MySqlCommand commandSQLSever = null;

            OpenConnectionIfNotAllreadyOpen(connectionSQLSever);

            commandSQLSever = connectionSQLSever.CreateCommand();
            commandSQLSever.CommandTimeout = GetSQLCommandTimeOut();

            DataTable result = new DataTable();

            MySqlDataReader reader = null;
            try
            {
                commandSQLSever.CommandText = model.CommandText;
                commandSQLSever.CommandType = model.CommandType;

                for (int i = 0; i < model.ParamList.Count; i++)
                {
                    if (model.ParamList[i].ParamLength == 0)
                    {
                        MySqlParameter param = commandSQLSever.Parameters.Add(model.ParamList[i].ParamName, model.ParamList[i].ParamType);

                        param.Value = model.ParamList[i].ParamValue;
                    }
                    else
                    {
                        MySqlParameter param = commandSQLSever.Parameters.Add(model.ParamList[i].ParamName, model.ParamList[i].ParamType, model.ParamList[i].ParamLength);

                        param.Value = model.ParamList[i].ParamValue;
                    }
                }

                reader = commandSQLSever.ExecuteReader(CommandBehavior.Default);
                result.Load(reader);
            }
            catch (MySqlException se)
            {
                reader = null;
                throw new Exception(ERROR_EXECUTING_COMMAND, se);
            }
            finally
            {
                if (reader != null)
                    reader.Close();
                CloseConnection(connectionSQLSever);
            }

            return result;
        }
        protected int ExecuteNonQuery(CommandExecutionModel model)
        {
            if (model == null) { throw new ArgumentNullException("CommandExecutionModel cannot be null"); }

            MySqlConnection connectionSQLSever = CreateConnection();
            MySqlCommand commandSQLSever = null;

            OpenConnectionIfNotAllreadyOpen(connectionSQLSever);

            commandSQLSever = connectionSQLSever.CreateCommand();
            commandSQLSever.CommandTimeout = GetSQLCommandTimeOut();

            MySqlTransaction transactionSQLSever = null;
            try
            {
                commandSQLSever.CommandText = model.CommandText;
                commandSQLSever.CommandType = model.CommandType;

                for (int i = 0; i < model.ParamList.Count; i++)
                {
                    if (model.ParamList[i].ParamLength == 0)
                    {
                        MySqlParameter param = commandSQLSever.Parameters.Add(model.ParamList[i].ParamName, model.ParamList[i].ParamType);

                        param.Value = model.ParamList[i].ParamValue;
                    }
                    else
                    {
                        MySqlParameter param = commandSQLSever.Parameters.Add(model.ParamList[i].ParamName, model.ParamList[i].ParamType, model.ParamList[i].ParamLength);

                        param.Value = model.ParamList[i].ParamValue;
                    }
                }

                transactionSQLSever = connectionSQLSever.BeginTransaction();
                commandSQLSever.Transaction = transactionSQLSever;
                int rowsAffected = commandSQLSever.ExecuteNonQuery();
                transactionSQLSever.Commit();
                return rowsAffected;
            }
            catch (MySqlException se)
            {
                transactionSQLSever.Rollback();
                transactionSQLSever = null;
                //Loggers.DigitalSignManager.Exception("Call ExecuteNonQuery() Exception:", se);
                throw new Exception(ERROR_EXECUTING_COMMAND, se);
            }
            finally
            {
                transactionSQLSever = null;
                CloseConnection(connectionSQLSever);
            }

        }
        protected object ExecuteScalar(CommandExecutionModel model)
        {
            if (model == null) { throw new ArgumentNullException("CommandExecutionModel cannot be null"); }

            MySqlConnection connectionSQLSever = CreateConnection();
            MySqlCommand commandSQLSever = null;

            OpenConnectionIfNotAllreadyOpen(connectionSQLSever);

            commandSQLSever = connectionSQLSever.CreateCommand();
            commandSQLSever.CommandTimeout = GetSQLCommandTimeOut();

            MySqlTransaction transactionSQLSever = null;

            try
            {
                commandSQLSever.CommandText = model.CommandText;
                commandSQLSever.CommandType = model.CommandType;

                for(int i = 0; i < model.ParamList.Count; i++)
                {
                    if (model.ParamList[i].ParamLength == 0)
                    {
                        MySqlParameter param = commandSQLSever.Parameters.Add(model.ParamList[i].ParamName, model.ParamList[i].ParamType);

                        param.Value = model.ParamList[i].ParamValue;
                    }
                    else
                    {
                        MySqlParameter param = commandSQLSever.Parameters.Add(model.ParamList[i].ParamName, model.ParamList[i].ParamType, model.ParamList[i].ParamLength);

                        param.Value = model.ParamList[i].ParamValue;
                    }
                }

                // Declare transaction            
                transactionSQLSever = connectionSQLSever.BeginTransaction();
                commandSQLSever.Transaction = transactionSQLSever;
                var exec = commandSQLSever.ExecuteScalar();
                // Commit the changes to disk if everything above succeeded                
                transactionSQLSever.Commit();
                return exec;
            }
            catch (MySqlException se)
            {
                transactionSQLSever.Rollback();
                transactionSQLSever = null;
                //Loggers.DigitalSignManager.Exception("Call ExecuteScalar() Exception:", se);
                throw new Exception(ERROR_EXECUTING_COMMAND, se);
            }
            finally
            {
                transactionSQLSever = null;
                CloseConnection(connectionSQLSever);
            }

        }
        protected void CloseConnection(MySqlConnection connectionSQLSever)
        {
            if (connectionSQLSever != null)
            {
                try
                {
                    connectionSQLSever.Close();
                }
                catch (MySqlException se)
                {
                    throw new Exception(COULD_NOT_CLOSE_CONNECTION, se);
                }
            }
        }
        private void OpenConnectionIfNotAllreadyOpen(MySqlConnection connectionSQLSever)
        {
            switch (connectionSQLSever.State)
            {
                case ConnectionState.Broken:
                case ConnectionState.Closed:
                    try
                    {
                        connectionSQLSever.Open();
                    }
                    catch (MySqlException se)
                    {
                        connectionSQLSever.Close();

                        //Loggers.DigitalSignManager.Exception("Call OpenConnectionIfNotAllreadyOpen() Exception:", se);
                        throw new Exception(COULD_NOT_CREATE_COMMAND_OR_CONNECTION, se);
                    }

                    break;
                default:
                    break;
            }
        }

        private int GetSQLCommandTimeOut()
        {
            var res = 30;
            try
            {
                res = Convert.ToInt32(System.Configuration.ConfigurationManager.AppSettings.Get("SQLCommandTimeOut"));
            }
            catch (Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                //Loggers.DigitalSignManager.Exception("exception GetConnectionTimeout ex =" + ex.ToString(), ex);
            }
            return res;
        }
    }
}
