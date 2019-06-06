using System;
using System.Collections.Generic;
using System.Text;

namespace WatsonFunction.Worker.Classes
{
    public class Settings
    {
        #region Public-Members

        public MessageQueueSettings MessageQueue = new MessageQueueSettings(); 

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

        #endregion
    }
}
