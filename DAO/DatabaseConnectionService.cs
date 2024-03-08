using System;
using System.Collections.Generic;
using System.Data;
using MySql.Data.MySqlClient;
using System.Text;
using System.Xml;

namespace DAO
{
    public class CommandExecutionModel
    {
        string _commandText;
        CommandType _commandType;

        List<MySqlParam> paramList = new List<MySqlParam>();

        public string CommandText { get => _commandText; set => _commandText = value; }
        public CommandType CommandType { get => _commandType; set => _commandType = value; }
        public List<MySqlParam> ParamList { get => paramList; set => paramList = value; }

        public void AddParam(string paramName, object paramValue, MySqlDbType paramType)
        {
            MySqlParam sqlParam = new MySqlParam(paramName, paramValue, paramType);
            ParamList.Add(sqlParam);
        }
        public void AddParam(string paramName, object paramValue, MySqlDbType paramType, int paramLength)
        {
            MySqlParam sqlParam = new MySqlParam(paramName, paramValue, paramType, paramLength);
            ParamList.Add(sqlParam);
        }

        public void SetCommand(string commandText, CommandType commandType)
        {
            CommandText = commandText;
            CommandType = commandType;
        }

        public void AddParam(MySqlParam sqlParam)
        {
            ParamList.Add(sqlParam);
        }

        public CommandExecutionModel()
        {

        }
    }
    public class MySqlParam
    {
        string _paramName = "";
        object _paramValue = null;
        MySqlDbType _paramType;
        int _paramLength = 0;

        public MySqlParam(string paramName, object paramValue, MySqlDbType paramType)
        {
            _paramName = paramName;
            _paramValue = paramValue;
            _paramType = paramType;
            _paramLength = 0;
        }

        public MySqlParam(string paramName, object paramValue, MySqlDbType paramType, int paramLength)
        {
            _paramName = paramName;
            _paramValue = paramValue;
            _paramType = paramType;
            _paramLength = paramLength;
        }

        public string ParamName { get => _paramName; set => _paramName = value; }
        public object ParamValue { get => _paramValue; set => _paramValue = value; }
        public MySqlDbType ParamType { get => _paramType; set => _paramType = value; }
        public int ParamLength { get => _paramLength; set => _paramLength = value; }
    }

}
