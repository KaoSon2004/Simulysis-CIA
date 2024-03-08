using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAO.DAO.SqlServerDAO.User
{
    public class RoleDAO : AbstractSqlServerDAO, IRoleDAO
    {
        public List<RoleDTO> ReadAllRole()
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("ReadAllRole", CommandType.StoredProcedure);

                DataTable table = ExecuteReader(model);

                List<RoleDTO> roles = DatatableToListRole(table);

                return roles;
            }
            catch (Exception ex)
            {
                return new List<RoleDTO>();
            }
        }

        public static List<RoleDTO> DatatableToListRole(DataTable RoleDataTable)
        {
            List<RoleDTO> list = new List<RoleDTO>();
            try
            {
                if (RoleDataTable == null)
                {
                    return new List<RoleDTO>();
                }

                for (int i = 0; i < RoleDataTable.Rows.Count; i++)
                {
                    RoleDTO role = new RoleDTO();
                    role.Id = string.IsNullOrEmpty(RoleDataTable.Rows[i]["Id"].ToString()) ? 0 : long.Parse(RoleDataTable.Rows[i]["Id"].ToString());
                    role.Name = RoleDataTable.Rows[i]["Name"].ToString();
                    list.Add(role);
                }
            }
            catch (Exception ex)
            {
                //Loggers.WebService.Exception("Method ConverDatatabletoListResourceTypesModel Error: ", ex, Constants.EVENTLOG_EVENTID_WEBMANAGER);
            }

            return list;
        }
    }
}
