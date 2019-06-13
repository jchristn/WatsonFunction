using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json;
using SyslogLogging;

using BigQ.Client;
using BigQ.Core;

using WatsonFunction;
using WatsonFunction.FunctionBase;
using WatsonFunction.Worker.Classes;

namespace WatsonFunction.Worker
{
    class Program
    {
        static Settings _Settings;
        static LoggingModule _Logging;
        static ClientConfiguration _BigQConfig;
        static Client _BigQClient;

        static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        static CancellationToken _Token = _TokenSource.Token;

        static bool _RunForever = true;

        static void Main(string[] args)
        {
            Console.WriteLine(Logo());
            Console.WriteLine("WatsonFunction Worker " + Version() + " starting");
            Console.WriteLine("Press ENTER to exit");

            InitializeSettings();
            InitializeLogging();
            InitializeBigQ();
            Task.Run(() => MaintainConnection(), _Token); 

            string userInput = null;
            while (_RunForever)
            {
                userInput = Common.InputString("Command [? for help]:", null, false);
                switch (userInput)
                {
                    case "?":
                        Menu();
                        break;

                    case "c":
                    case "cls":
                        Console.Clear();
                        break;

                    case "q":
                        _RunForever = false;
                        break;
                }
            }

            Console.WriteLine("WatsonFunction MessageBus exiting");
        }

        static void InitializeSettings()
        {
            if (!File.Exists("System.json"))
            {
                Console.WriteLine("Using default configuration");
                _Settings = new Settings();
            }
            else
            {
                _Settings = Common.DeserializeJson<Settings>(File.ReadAllBytes("System.json"));
            }
        }

        static void InitializeLogging()
        {
            _Logging = new LoggingModule(
                _Settings.Logging.SyslogServerIp,
                _Settings.Logging.SyslogServerPort,
                _Settings.Logging.ConsoleLogging,
                _Settings.Logging.MinimumSeverity,
                false,
                false,
                true,
                true,
                false,
                false);
        }

        #region Console

        static void Menu()
        {
            Console.WriteLine("--- Available Commands ---");
            Console.WriteLine("  ?                Help, this menu");
            Console.WriteLine("  cls              Clear the screen");
            Console.WriteLine("  q                Exit the application");
            Console.WriteLine("");
        }

        #endregion

        #region Misc-Methods

        static string Version()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;
            return version;
        }

        static string Logo()
        {
            // http://patorjk.com/software/taag/#p=display&f=Slant&t=watson

            string ret =
              @"                   __                   " + Environment.NewLine +
              @"   _      ______ _/ /__________  ____   " + Environment.NewLine +
              @"  | | /| / / __ `/ __/ ___/ __ \/ __ \  " + Environment.NewLine +
              @"  | |/ |/ / /_/ / /_(__  ) /_/ / / / /  " + Environment.NewLine +
              @"  |__/|__/\__,_/\__/____/\____/_/ /_/   ";

            return ret;
        }

        #endregion

        #region BigQ-Methods

        static void InitializeBigQ()
        {
            _BigQConfig = ClientConfiguration.Default();
            _BigQConfig.TcpServer.Enable = true;
            _BigQConfig.TcpServer.Port = _Settings.MessageQueue.TcpPort;
            _BigQConfig.TcpServer.Debug = false;

            _BigQConfig.ClientGUID = Guid.NewGuid().ToString();
            _BigQConfig.Email = _BigQConfig.ClientGUID + "@watsonfunction.local";
            _BigQConfig.Name = _BigQConfig.ClientGUID;
            _BigQConfig.Password = _BigQConfig.ClientGUID;
            _BigQConfig.ServerGUID = "00000000-0000-0000-0000-000000000000";
            _BigQConfig.SyncTimeoutMs = 15000;

            _BigQClient = new Client(_BigQConfig);

            _BigQClient.Callbacks.AsyncMessageReceived = AsyncMessageReceived;
            _BigQClient.Callbacks.ChannelCreated = ChannelCreated;
            _BigQClient.Callbacks.ChannelDestroyed = ChannelDestroyed;
            _BigQClient.Callbacks.ClientJoinedChannel = ClientJoinedChannel;
            _BigQClient.Callbacks.ClientJoinedServer = ClientJoinedServer;
            _BigQClient.Callbacks.ClientLeftChannel = ClientLeftChannel;
            _BigQClient.Callbacks.ClientLeftServer = ClientLeftServer;
            _BigQClient.Callbacks.ServerConnected = ServerConnected;
            _BigQClient.Callbacks.ServerDisconnected = ServerDisconnected;
            _BigQClient.Callbacks.SubscriberJoinedChannel = SubscriberJoinedChannel;
            _BigQClient.Callbacks.SubscriberLeftChannel = SubscriberLeftChannel;
            _BigQClient.Callbacks.SyncMessageReceived = SyncMessageReceived;
        }

        static void MaintainConnection()
        {
            while (true)
            {
                Task.Delay(1000).Wait();

                try
                {
                    if (!_BigQClient.Connected)
                    {
                        _Logging.Log(LoggingModule.Severity.Warn, "Worker MaintainConnection disconnected from message bus, attempting to reconnect");
                        InitializeBigQ();
                    }

                    if (!_BigQClient.LoggedIn)
                    {
                        _Logging.Log(LoggingModule.Severity.Debug, "Worker MaintainConnection attempting login to message bus");
                        Message loginResp = null;
                        if (!_BigQClient.Login(out loginResp))
                        {
                            _Logging.Log(LoggingModule.Severity.Warn, "Worker MaintainConnection unable to login to message bus");
                        }
                        else
                        {
                            _Logging.Log(LoggingModule.Severity.Debug, "Worker MaintainConnection logged into message bus, joining channels");
                            Thread.Sleep(1000);
                            JoinChannels();
                        }
                    }
                }
                catch (Exception e)
                {
                    _Logging.Log(LoggingModule.Severity.Alert, "Worker MaintainConnection exception while attempting to reconnect:");
                    _Logging.LogException("Worker", "MaintainConnection", e);
                }
            }
        }

        static void JoinChannels()
        {
            try
            {
                Message msg = null;
                if (_BigQClient.JoinChannel(_Settings.MessageQueue.Channels.MainChannel, out msg))
                    _Logging.Log(LoggingModule.Severity.Debug, "Worker JoinChannels joined channel '" + _Settings.MessageQueue.Channels.MainChannel + "'");
                else _Logging.Log(LoggingModule.Severity.Warn, "Worker JoinChannels failed to join channel '" + _Settings.MessageQueue.Channels.MainChannel + "'");

                if (_BigQClient.JoinChannel(_Settings.MessageQueue.Channels.HealthChannel, out msg))
                    _Logging.Log(LoggingModule.Severity.Debug, "Worker JoinChannels joined channel '" + _Settings.MessageQueue.Channels.HealthChannel + "'");
                else _Logging.Log(LoggingModule.Severity.Warn, "Worker JoinChannels failed to join channel '" + _Settings.MessageQueue.Channels.HealthChannel + "'");

                if (_BigQClient.SubscribeChannel(_Settings.MessageQueue.Channels.InvocationChannel, out msg))
                    _Logging.Log(LoggingModule.Severity.Debug, "Worker JoinChannels subscribed to channel '" + _Settings.MessageQueue.Channels.InvocationChannel + "'");
                else _Logging.Log(LoggingModule.Severity.Warn, "Worker JoinChannels failed to subscribe to channel '" + _Settings.MessageQueue.Channels.InvocationChannel + "'");
            }
            catch (Exception e)
            {
                _Logging.Log(LoggingModule.Severity.Alert, "Worker JoinChannels exception while attempting to join channels:");
                _Logging.LogException("Worker", "JoinChannels", e);
            }
        }

        static bool ChannelCreated(string channelGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ChannelCreated " + channelGuid);
            return true;
        }

        static bool ChannelDestroyed(string channelGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ChannelDestroyed " + channelGuid);
            return true;
        }

        static bool ClientJoinedChannel(string clientGuid, string channelGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ClientJoinedChannel client " + clientGuid + " joined channel " + channelGuid);
            return true;
        }

        static bool ClientJoinedServer(string clientGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ClientJoinedServer client " + clientGuid + " joined the server");
            return true;
        }

        static bool ClientLeftChannel(string clientGuid, string channelGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ClientLeftChannel client " + clientGuid + " left channel " + channelGuid);
            return true;
        }

        static bool ClientLeftServer(string clientGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ClientLeftServer client " + clientGuid + " left the server");
            return true;
        }

        static bool ServerConnected()
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ServerConnected message queue server connected");
            return true;
        }

        static bool ServerDisconnected()
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker ServerDisconnected message queue server disconnected");
            return true;
        }

        static bool SubscriberJoinedChannel(string clientGuid, string channelGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker SubscriberJoinedChannel client " + clientGuid + " subscribed to channel " + channelGuid);
            return true;
        }

        static bool SubscriberLeftChannel(string clientGuid, string channelGuid)
        {
            _Logging.Log(LoggingModule.Severity.Debug, "Worker SubscriberLeftChannel client " + clientGuid + " unsubscribed from channel " + channelGuid);
            return true;
        }

        static byte[] SyncMessageReceived(Message msg)
        {
            // _Logging.Log(LoggingModule.Severity.Debug, "Worker SyncMessageReceived sync message received: " + Environment.NewLine + msg.ToString());
            Request req = Common.DeserializeJson<Request>(msg.Data); 
            Invoker inv = new Invoker(_Logging, req); 
            Response resp = inv.Invoke(); 
            return Encoding.UTF8.GetBytes(Common.SerializeJson(resp));
        }

        static bool AsyncMessageReceived(Message msg)
        {
            // _Logging.Log(LoggingModule.Severity.Debug, "Worker AsyncMessageReceived message received: " + Environment.NewLine + msg.ToString());
            return true;
        }

        #endregion
    }
}
