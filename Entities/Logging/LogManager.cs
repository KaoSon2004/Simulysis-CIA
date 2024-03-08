using log4net.Config;
using System.IO;
using System.Xml;

namespace Entities.Logging
{
    public class LogManager
    {
        public static void CopyLog4NetConfig(string sourceFileLog4Net, string destFileLog4Net)
        {
            bool fileExist = File.Exists(destFileLog4Net);
            if (fileExist == false)
            {
                FileInfo file = new FileInfo(destFileLog4Net);
                bool sourceFileLog4NetExist = File.Exists(sourceFileLog4Net);
                if (sourceFileLog4NetExist)
                {
                    System.IO.File.Copy(sourceFileLog4Net, destFileLog4Net, true);
                    FileInfo fInfo = new FileInfo(destFileLog4Net);
                    fInfo.IsReadOnly = false;
                }
            }
        }
        
        public static void ApplyConfigSetting(string sourceFileLog4Net)
        {
            if (File.Exists(sourceFileLog4Net))
            {
                try
                {
                    XmlConfigurator.Configure(new System.IO.FileInfo(sourceFileLog4Net));
                    Loggers.SVP = new Log4netLogging("SVP");
                }
                catch (System.Exception)
                {
                    //Loggers.WebService = new Log4netLogging("DSSWebservice");
                    //Loggers.DigitalSignManager = new Log4netLogging("DigitalSignManager");
                    //Loggers.WinService = new Log4netLogging("DSSWindowsservice");
                }
            }
        }

        public static void SetLog4NetLevel(string logLevel, string filePath)
        {
            try
            {
                bool fileExist = File.Exists(filePath);
                //Loggers.DigitalSignManager.Info("fileExist = " + fileExist);
                if (fileExist == true)
                {
                    File.SetAttributes(filePath, File.GetAttributes(filePath) & ~FileAttributes.ReadOnly);
                    XmlDocument log4NetXmlDocument = new XmlDocument();
                    log4NetXmlDocument.Load(filePath);

                    var logLever = logLevel == "Verbose" ? "All" : logLevel;

                    XmlNode log4NetWindowsEventLog = log4NetXmlDocument.SelectSingleNode("/configuration/log4net/root[@name='WindowsEventLog']");
                    if (log4NetWindowsEventLog != null)
                    {
                       
                        //Loggers.DigitalSignManager.Info("log4NetWindowsEventLog is not null.");
                        if (log4NetXmlDocument.SelectSingleNode("/configuration/log4net/root[@name='WindowsEventLog']/level") != null)
                        {
                            log4NetXmlDocument.SelectSingleNode("/configuration/log4net/root[@name='WindowsEventLog']/level").Attributes["value"].Value = logLever;
                        }
                       
                    }
                    else
                    {
                        //Loggers.DigitalSignManager.Info("log4NetWindowsEventLog is null.");
                    }

                    XmlNode log4NetNodeEventLog = log4NetXmlDocument.SelectSingleNode("/configuration/log4net/appender[@name='Log_EventLogAppender']");
                    if (log4NetNodeEventLog != null)
                    {
                        if (log4NetXmlDocument.SelectSingleNode("/configuration/log4net/appender[@name='Log_EventLogAppender']/filter") != null)
                        {
                            XmlNode log4NetNodeEventLogFilter = log4NetXmlDocument.SelectSingleNode("/configuration/log4net/appender[@name='Log_EventLogAppender']/filter");
                            if (log4NetNodeEventLogFilter != null)
                            {
                                if (log4NetXmlDocument.SelectSingleNode("/configuration/log4net/appender[@name='Log_EventLogAppender']/filter/levelMin") != null)
                                {
                                    log4NetXmlDocument.SelectSingleNode("/configuration/log4net/appender[@name='Log_EventLogAppender']/filter/levelMin").Attributes["value"].Value = logLever;
                                }
                            }
                        }
                    }

                    XmlNode log4NetNodeDigitalSignManager = log4NetXmlDocument.SelectSingleNode("/configuration/log4net/logger[@name='SVP']");
                    if (log4NetWindowsEventLog != null)
                    {
                        if (log4NetXmlDocument.SelectSingleNode("/configuration/log4net/logger[@name='SVP']/level") != null)
                        {
       
                            log4NetXmlDocument.SelectSingleNode("/configuration/log4net/logger[@name='SVP']/level").Attributes["value"].Value = logLever;
                        }
                    }

                    log4NetXmlDocument.Save(filePath);
                    log4NetXmlDocument = null;
                }
            }
            catch (System.Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
                //Loggers.DigitalSignManager.Exception("Method error SetLog4NetLevel: ", ex);
            }

        }

        public static void SetLoggedFilesStorage(string filePath, string targetFolder)
        {
            try {

                bool fileExist = File.Exists(filePath);
                if (fileExist)
                {
                    XmlDocument log4NetXmlDocument = new XmlDocument();
                    log4NetXmlDocument.Load(filePath);
                    log4NetXmlDocument.SelectSingleNode("/configuration/log4net/appender[@name='Log_RollingFileAppender_SVP']/file").Attributes["value"].Value = targetFolder;
                    log4NetXmlDocument.Save(filePath);
                }
            }
            catch(System.Exception ex)
            {
                Loggers.SVP.Exception(ex.Message, ex);
            }

         
        }


    }

}
