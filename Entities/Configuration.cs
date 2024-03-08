using Microsoft.Extensions.Configuration;

namespace Entities
{
    public static class Configuration
    {
        #region Constructors
        private static IConfigurationRoot configuration;
        
        static Configuration()
        {
            configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();
        }
        #endregion

        public static string WWWRoot
        {
            get => configuration.GetValue<string>("WWWRoot");
        }

        public static int MaxThreadNumber
        {
            get => configuration.GetValue("MaxThreadNumber", 16);
        }

        public static int MaxInsertThreadNumber
        {
            get => configuration.GetValue("MaxInsertThreadNumber", 16);
        }

        public static int MaxRowsPerInsert
        {
            get => configuration.GetValue("MaxRowsPerInsert", 300);
        }

        public static string ConnectionString
        {
            get
            {
                string connString = configuration.GetConnectionString("Default");
                if (connString == null || connString.Length == 0)
                {
                    ThrowMissingAppSettingConfigurationException("ConnectionString");
                }

                return connString;
            }
        }
        public static string PAT
        {
            get => configuration.GetValue<string>("pat");
        }
        public static int SQLCommandTimeOut
        {
            get => configuration.GetValue("SQLCommandTimeOut", 600);
        }

        private static void ThrowMissingAppSettingConfigurationException(string nameOfMissingAppSetting)
        {
            string message = "ConfigurationSetting " + nameOfMissingAppSetting + " is missing or blank.";

            //throw new ConfigurationException(message);
        }
    }
}