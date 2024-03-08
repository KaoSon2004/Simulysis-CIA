using System;

namespace DAO.Config
{
    /// <summary>
    /// DAO configuration for the backend system.
    /// </summary>
    internal class DAOConfiguration
    {
        /// <summary>
        /// Get the type of DAO to be used from the specified identifier
        /// </summary>
        /// <param name="identifier">Identifier of DAO to use</param>
        /// <returns>Type of DAO to use.</returns>		
        public static Type GetDAOType(string identifier)
        {
            switch (identifier)
            {
                case "IUserDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.User.UserDAO"); 

                case "IRoleDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.User.RoleDAO");

                case "IProjectDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.ProjectFile.ProjectDAO");

                case "IProjectFileDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.ProjectFile.ProjectFileDAO");

                case "ISystemDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.FileContent.SystemDAO");
       
                case "ILineDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.FileContent.LineDAO");
           
                case "IBranchDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.FileContent.BranchDAO");

                case "ICalibrationDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.Calibration.CalibrationDAO");

                case "IPortDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.FileContent.PortDAO");
                
                case "IListDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.FileContent.ListDAO");
               
                case "IInstanceDataDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.FileContent.InstanceDataDAO");

                case "IConfigurationDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.Configuration.ConfigurationDAO");

                case "IFilesRelationshipDAO":
                    return Type.GetType("DAO.DAO.SqlServerDAO.ProjectFile.FilesRelationshipDAO");

                default:
                    return null;
            }
        }
    }
}
