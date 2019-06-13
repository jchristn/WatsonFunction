using System;
using System.Collections.Generic;
using System.Text;

using SyslogLogging;

using WatsonFunction.FunctionBase;

namespace WatsonFunction.ApiGateway.Classes
{
    public class Settings
    {
        #region Public-Members

        public WebserverSettings Webserver = new WebserverSettings();
        public MessageQueueSettings MessageQueue = new MessageQueueSettings();
        public List<FunctionApplication> Applications = new List<FunctionApplication>();
        public LoggingSettings Logging = new LoggingSettings();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        public Settings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion

        #region Public-Embedded-Classes

        public class WebserverSettings
        {
            public string Hostname = "*";
            public int TcpPort = 9000;
            public bool Ssl = false;
            public bool Debug = false;

            public WebserverSettings()
            {

            }
        }

        public class MessageQueueSettings
        {
            public int TcpPort = 9000;
            public bool Debug = false;
            public ChannelsSettings Channels = new ChannelsSettings();

            public class ChannelsSettings
            {
                public string MainChannel;
                public string HealthChannel;
                public string InvocationChannel;

                public ChannelsSettings()
                {
                    MainChannel = "main";
                    HealthChannel = "health";
                    InvocationChannel = "invocation";
                }
            }

            public MessageQueueSettings()
            {

            }
        }

        public class LoggingSettings
        {
            public string SyslogServerIp = "127.0.0.1";
            public int SyslogServerPort = 514;
            public LoggingModule.Severity MinimumSeverity = LoggingModule.Severity.Info;
            public bool ConsoleLogging = true;

            public LoggingSettings()
            {

            }
        }

        #endregion 
    }
}
