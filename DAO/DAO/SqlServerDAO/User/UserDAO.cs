using Common;
using DAO.DAO.InterfaceDAO;
using Entities.DTO;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace DAO.DAO.SqlServerDAO.User
{
    public class UserDAO : AbstractSqlServerDAO, IUserDAO
    {
        public long CreateUser(UserDTO accountDTO)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("CreateUser", CommandType.StoredProcedure);
                model.AddParam("@Username", accountDTO.Username, MySqlDbType.VarChar, 255);
                model.AddParam("@Password", accountDTO.EncryptedPassword, MySqlDbType.VarChar, 255);
                model.AddParam("@FirstName", accountDTO.FirstName, MySqlDbType.VarChar, 255);
                model.AddParam("@LastName", accountDTO.LastName, MySqlDbType.VarChar, 255);
                model.AddParam("@FK_RoleId", accountDTO.FK_RoleId, MySqlDbType.Int64);
                //model.AddParam("@Email", DBNull.Value, MySqlDbType.VarChar, 255);
                //model.AddParam("@IsActive", DBNull.Value, MySqlDbType.Bit);
                //model.AddParam("@ActivationCode", DBNull.Value, MySqlDbType.VarChar, 255);

                object obj = ExecuteScalar(model);

                long id = Convert.ToInt64(obj.ToString());

                return id;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        public UserDTO ReadUser(string username)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("[dbo].[ReadUserByUserName]", CommandType.StoredProcedure);
                model.AddParam("@Username", username, MySqlDbType.VarChar, 255);

                DataTable table = ExecuteReader(model);

                List<UserDTO> users = DatatableToListUser(table);

                UserDTO userDTO = users.Count == 0? new UserDTO() : users[0];

                List<RoleDTO> roles = ReadRoleByUsername(username);

                userDTO.Roles = roles;

                return userDTO;
            }
            catch (Exception ex)
            {
                return new UserDTO();
            }
        }

        /// <summary>
        /// 0: SUCCESS, -1: FAILED
        /// </summary>
        /// <param name="activationCode"></param>
        /// <returns></returns>
        public long ActivateAccount(string activationCode)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("[dbo].[ActivateAccount]", CommandType.StoredProcedure);
                model.AddParam("@ActivationCode", activationCode, MySqlDbType.VarChar, 255);

                object obj = ExecuteScalar(model);

                long result = Convert.ToInt64(obj.ToString());

                return result;
            }
            catch (Exception ex)
            {
                return -1;
            }
        }


        public UserDTO ReadUserNameByEmail(string email)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("[dbo].[ReadUserNameByEmail]", CommandType.StoredProcedure);
                model.AddParam("@Email", email, MySqlDbType.VarChar, 255);

                DataTable table = ExecuteReader(model);

                List<UserDTO> users = DatatableToListUser(table);

                UserDTO userDTO = users.Count == 0 ? new UserDTO() : users[0];

                List<RoleDTO> roles = ReadRoleByUsername(userDTO.Username);

                userDTO.Roles = roles;

                return userDTO;
            }
            catch (Exception ex)
            {
                return new UserDTO();
            }
        }
        private List<RoleDTO> ReadRoleByUsername(string username)
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("[dbo].[ReadRoleByUsername]", CommandType.StoredProcedure);
                model.AddParam("@Username", username, MySqlDbType.VarChar, 255);

                DataTable table = ExecuteReader(model);

                List<RoleDTO> roles = RoleDAO.DatatableToListRole(table);

                return roles;
            }
            catch (Exception ex)
            {
                return new List<RoleDTO>();
            }
        }

        private List<UserDTO> DatatableToListUser(DataTable UserDataTable)
        {
            List<UserDTO> list = new List<UserDTO>();
            try
            {
                if (UserDataTable == null)
                {
                    return new List<UserDTO>();
                }

                for (int i = 0; i < UserDataTable.Rows.Count; i++)
                {
                    UserDTO resourceTypeModel = new UserDTO();
                    resourceTypeModel.Id = string.IsNullOrEmpty(UserDataTable.Rows[i]["Id"].ToString()) ? 0 : long.Parse(UserDataTable.Rows[i]["Id"].ToString());
                    resourceTypeModel.Username = UserDataTable.Rows[i]["Username"].ToString();
                    resourceTypeModel.FirstName = UserDataTable.Rows[i]["FirstName"].ToString();
                    resourceTypeModel.LastName = UserDataTable.Rows[i]["LastName"].ToString();
                    resourceTypeModel.EncryptedPassword = UserDataTable.Rows[i]["Password"].ToString();
                    resourceTypeModel.Password = EncDec.Decrypt(UserDataTable.Rows[i]["Password"].ToString());
                    resourceTypeModel.FK_RoleId = string.IsNullOrEmpty(UserDataTable.Rows[i]["FK_RoleId"].ToString()) ? 0 : long.Parse(UserDataTable.Rows[i]["FK_RoleId"].ToString());
                    //resourceTypeModel.Email = UserDataTable.Rows[i]["Email"].ToString();
                    //resourceTypeModel.IsActive = string.IsNullOrEmpty(UserDataTable.Rows[i]["IsActive"].ToString()) ? false : bool.Parse(UserDataTable.Rows[i]["IsActive"].ToString());


                    //resourceTypeModel.ActivationCode = new Guid(UserDataTable.Rows[i]["ActivationCode"].ToString());
                    list.Add(resourceTypeModel);
                }
            }
            catch (Exception ex)
            {
                //Loggers.WebService.Exception("Method ConverDatatabletoListResourceTypesModel Error: ", ex, Constants.EVENTLOG_EVENTID_WEBMANAGER);
            }

            return list;
        }

        public List<UserDTO> ReadAllUser()
        {
            try
            {
                CommandExecutionModel model = new CommandExecutionModel();
                model.SetCommand("[uet].[ReadAllUsers]", CommandType.StoredProcedure);
                DataTable table = ExecuteReader(model);
                List<UserDTO> users = DatatableToListUser(table);

                users.ForEach(user =>
                {
                    List<RoleDTO> roles = ReadRoleByUsername(user.Username);

                    user.Roles = roles;
                });

                return users;
            }
            catch (Exception ex)
            {
                return new List<UserDTO>();
            }
        }

    }
}
