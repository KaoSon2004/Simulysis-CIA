using System;
using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;

namespace Entities.Logging
{
    #region Common Log

    public static class Loggers
    {
        public static Log4netLogging SVP = new Log4netLogging("SVP");
    }

    public class Log4netLogging
    {
        private log4net.ILog _log;

        public Log4netLogging(string appIdentifier)
        {
            _log = log4net.LogManager.GetLogger(appIdentifier);
        }

        public void Info(string message, int EventLogEventID = 10000)
        {
            log4net.ThreadContext.Properties["EventID"] = EventLogEventID;
            _log.Info(message.Trim());
        }

        public void Error(string message, int EventLogEventID = 10000)
        {
            log4net.ThreadContext.Properties["EventID"] = EventLogEventID;
            _log.Error(message.Trim());
        }

        public void Debug(string message, int EventLogEventID = 10000)
        {
            log4net.ThreadContext.Properties["EventID"] = EventLogEventID;
            _log.Debug(message.Trim());
        }

        public void Warning(string message, int EventLogEventID = 10000)
        {
            log4net.ThreadContext.Properties["EventID"] = EventLogEventID;
            _log.Warn(message.Trim());
        }

        public void Exception(Exception exception, int EventLogEventID = 10000)
        {
            log4net.ThreadContext.Properties["EventID"] = EventLogEventID;
            _log.Error(ExceptionInfo(exception));
        }

        public void Exception(string message, Exception exception, int EventLogEventID = 10000)
        {
            log4net.ThreadContext.Properties["EventID"] = EventLogEventID;
            _log.Error(ExceptionInfo(exception, message));
        }

        public void RecurException(string message, Exception exception, int level = 0, int EventLogEventID = 10000)
        {
            log4net.ThreadContext.Properties["EventID"] = EventLogEventID;
            _log.Info($"=========Exception level: {level}=========");
            _log.Error(ExceptionInfo(exception, message));
            if (exception.InnerException != null)
            {
                RecurException(exception.InnerException.Message, exception.InnerException, level + 1);
            }
        }

        private string ExceptionInfo(Exception exception, string message = "")
        {
            StringBuilder logInfo = new StringBuilder();
            try
            {
                System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.FileVersion;

                // Get Windows Info:
                var windowVersion = GetWindowsInfo();
                var bitnessVersion = "(x64)";
                if (Environment.Is64BitOperatingSystem)
                {
                    bitnessVersion = "(x64)";
                }
                else
                {
                    bitnessVersion = "(x86)";
                }

                /*
                // Get browser info:
                var userAgent = "";
                try
                {
                    if (HttpContext.Current != null && HttpContext.Current.Request != null)
                    {
                        userAgent = HttpContext.Current.Request.UserAgent;
                    }
                }
                catch (Exception)
                {
                }

                var userBrowser = new HttpBrowserCapabilities {Capabilities = new Hashtable {{string.Empty, userAgent}}};


                var factory = new BrowserCapabilitiesFactory();
                factory.ConfigureBrowserCapabilities(new NameValueCollection(), userBrowser);

                //Set User browser Properties
                var browserBrand = userBrowser.Browser;
                var browserVersion = userBrowser.Version;
                */

                logInfo.AppendLine("=====================  EXCEPTION STARTS  =========================");
                logInfo.AppendLine("\t\t Product version: " + version);
                logInfo.AppendLine(string.Format("\t\t Windows version: {0} {1}", windowVersion, bitnessVersion));
               // logInfo.AppendLine(string.Format("\t\t Browser name: {0} (v{1})", browserBrand, browserVersion));
                if (!string.IsNullOrEmpty(message))
                {
                    logInfo.AppendLine("\r\n" + message.Trim());
                }

                logInfo.AppendLine("\r\nException: " + exception.Message + "; StackTrace: " + exception.StackTrace);
                logInfo.AppendLine("=====================  EXCEPTION ENDS  =========================");
            }
            catch (Exception ex)
            {
                logInfo.AppendLine("\r\nException: " + ex.Message + "; StackTrace: " + ex.StackTrace);
            }

            return logInfo.ToString();
        }

        private string GetWindowsInfo()
        {
            try
            {
                //Get Operating system information.
                OperatingSystem os = Environment.OSVersion;
                //Get version information about the os.
                Version vs = os.Version;

                //Variable to hold our return value
                string operatingSystem = "";

                if (os.Platform == PlatformID.Win32Windows)
                {
                    //This is a pre-NT version of Windows
                    switch (vs.Minor)
                    {
                        case 0:
                            operatingSystem = "95";
                            break;
                        case 10:
                            if (vs.Revision.ToString() == "2222A")
                                operatingSystem = "98SE";
                            else
                                operatingSystem = "98";
                            break;
                        case 90:
                            operatingSystem = "Me";
                            break;
                        default:
                            break;
                    }
                }
                else if (os.Platform == PlatformID.Win32NT)
                {
                    switch (vs.Major)
                    {
                        case 3:
                            operatingSystem = "NT 3.51";
                            break;
                        case 4:
                            operatingSystem = "NT 4.0";
                            break;
                        case 5:
                            if (vs.Minor == 0)
                                operatingSystem = "2000";
                            else
                                operatingSystem = "XP";
                            break;
                        case 6:
                            if (vs.Minor == 0)
                                operatingSystem = "Vista";
                            else if (vs.Minor == 1)
                                operatingSystem = "7";
                            else if (vs.Minor == 2)
                                operatingSystem = "8";
                            else
                                operatingSystem = "8.1";
                            break;
                        case 10:
                            operatingSystem = "10";
                            break;
                        default:
                            break;
                    }
                }

                //Make sure we actually got something in our OS check
                //We don't want to just return " Service Pack 2" or " 32-bit"
                //That information is useless without the OS version.
                if (operatingSystem != "")
                {
                    //Got something.  Let's prepend "Windows" and get more info.
                    operatingSystem = "Windows " + operatingSystem;
                    //See if there's a service pack installed.
                    if (os.ServicePack != "")
                    {
                        //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                        operatingSystem += " " + os.ServicePack;
                    }
                    //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
                    //operatingSystem += " " + getOSArchitecture().ToString() + "-bit";
                }

                //Return the information we've gathered.
                return operatingSystem;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }

    #endregion
}