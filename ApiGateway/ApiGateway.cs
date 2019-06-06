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

using WatsonFunction;
using Newtonsoft.Json;
using WatsonWebserver;

using BigQ.Client;
using BigQ.Core;

using WatsonFunction.ApiGateway.Classes;
using WatsonFunction.FunctionBase;

namespace WatsonFunction.ApiGateway
{
    class Program
    {
        static Settings _Settings;
        static Server _Webserver;
        static ClientConfiguration _BigQConfig;
        static Client _BigQClient;
        static FunctionMatcher _Matcher;

        static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        static CancellationToken _Token = _TokenSource.Token;

        static bool _RunForever = true;

        static void Main(string[] args)
        {
            Console.WriteLine("WatsonFunction ApiGateway starting");
            Console.WriteLine("Press ENTER to exit");

            InitializeSettings();
            InitializeBigQ();
            Task.Run(() => MaintainConnection(), _Token);
            // JoinChannels();

            InitializeWebserver();

            _Matcher = new FunctionMatcher(_Settings.Applications);

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
                        Console.WriteLine("Disconnected from message bus, attempting to reconnect");
                        InitializeBigQ();
                    }

                    if (!_BigQClient.LoggedIn)
                    {
                        Console.WriteLine("Attempting login to message bus");
                        Message loginResp = null;
                        if (!_BigQClient.Login(out loginResp))
                        {
                            Console.WriteLine("Unable to login to message bus");
                        }
                        else
                        {
                            Console.WriteLine("Logged into message bus, joining channels");
                            Thread.Sleep(1000);
                            JoinChannels();
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception while attempting to reconnect:");
                    Console.WriteLine(Common.SerializeJson(e));
                }
            }
        }

        static void JoinChannels()
        {
            try
            {
                Message msg = null;
                if (_BigQClient.JoinChannel(_Settings.MessageQueue.Channels.MainChannel, out msg))
                    Console.WriteLine("Joined channel '" + _Settings.MessageQueue.Channels.MainChannel + "'");
                else Console.WriteLine("Failed to join channel '" + _Settings.MessageQueue.Channels.MainChannel + "'");

                if (_BigQClient.JoinChannel(_Settings.MessageQueue.Channels.HealthChannel, out msg))
                    Console.WriteLine("Joined channel '" + _Settings.MessageQueue.Channels.HealthChannel + "'");
                else Console.WriteLine("Failed to join channel '" + _Settings.MessageQueue.Channels.HealthChannel + "'");

                if (_BigQClient.JoinChannel(_Settings.MessageQueue.Channels.InvocationChannel, out msg))
                    Console.WriteLine("Joined channel '" + _Settings.MessageQueue.Channels.InvocationChannel + "'");
                else Console.WriteLine("Failed to join channel '" + _Settings.MessageQueue.Channels.InvocationChannel + "'");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while attempting to join channels:");
                Console.WriteLine(Common.SerializeJson(e));
            }
        }

        static bool AsyncMessageReceived(Message msg)
        {
            Console.WriteLine("Message received: " + Environment.NewLine + msg.ToString());
            return true;
        }

        static bool ChannelCreated(string channelGuid)
        {
            Console.WriteLine("Channel created: " + channelGuid);
            return true;
        }

        static bool ChannelDestroyed(string channelGuid)
        {
            Console.WriteLine("Channel destroyed: " + channelGuid);
            return true;
        }

        static bool ClientJoinedChannel(string clientGuid, string channelGuid)
        {
            Console.WriteLine("Client " + clientGuid + " joined channel " + channelGuid);
            return true;
        }

        static bool ClientJoinedServer(string clientGuid)
        {
            Console.WriteLine("Client " + clientGuid + " joined the server");
            return true;
        }

        static bool ClientLeftChannel(string clientGuid, string channelGuid)
        {
            Console.WriteLine("Client " + clientGuid + " left channel " + channelGuid);
            return true;
        }

        static bool ClientLeftServer(string clientGuid)
        {
            Console.WriteLine("Client " + clientGuid + " left the server");
            return true;
        }

        static bool ServerConnected()
        {
            Console.WriteLine("Message queue server connected");
            return true;
        }

        static bool ServerDisconnected()
        {
            Console.WriteLine("Message queue server disconnected");
            return true;
        }

        static bool SubscriberJoinedChannel(string clientGuid, string channelGuid)
        {
            Console.WriteLine("Client " + clientGuid + " subscribed to channel " + channelGuid);
            return true;
        }

        static bool SubscriberLeftChannel(string clientGuid, string channelGuid)
        {
            Console.WriteLine("Client " + clientGuid + " unsubscribed from channel " + channelGuid);
            return true;
        }

        static byte[] SyncMessageReceived(Message msg)
        {
            Console.WriteLine("Sync message received: " + Environment.NewLine + msg.ToString());
            return null;
        }

        #endregion

        #region Webserver-Methods

        static void InitializeWebserver()
        {
            _Webserver = new Server(
                _Settings.Webserver.Hostname,
                _Settings.Webserver.TcpPort,
                _Settings.Webserver.Ssl,
                RequestHandler);
        }

        static HttpResponse RequestHandler(HttpRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            HttpResponse resp = new HttpResponse(req, 500, null, "text/plain", Encoding.UTF8.GetBytes("Internal server error"));

            try
            {
                string userGuid = null;
                string functionName = null;
                bool adminApi = false;
                Definition currDefinition = null;
                Trigger currTrigger = null;
                Request fcnRequest = null;
                Response fcnResponse = null;
                FunctionMatcher.ErrorCode error = FunctionMatcher.ErrorCode.FunctionNotFound;

                if (req.RawUrlEntries.Count == 0)
                {
                    resp = new HttpResponse(req, 200, null, "text/html", Encoding.UTF8.GetBytes(LandingPage()));
                    return resp;
                }
                else if (req.RawUrlEntries.Count == 1 && req.RawUrlEntries[0].Equals("_directory"))
                {
                    resp = new HttpResponse(req, 200, null, "application/json", Encoding.UTF8.GetBytes(Common.SerializeJson(_Settings.Applications)));
                    return resp;
                }
                else if (req.RawUrlEntries.Count >= 1 && req.RawUrlEntries[0].Equals("admin"))
                {
                    adminApi = true;
                }
                else if (req.RawUrlEntries.Count >= 2 && !req.RawUrlEntries[0].Equals("admin"))
                {
                    userGuid = req.RawUrlEntries[0];
                    functionName = req.RawUrlEntries[1];
                }
                else
                {
                    resp.StatusCode = 400;
                    resp.Data = Encoding.UTF8.GetBytes("URL must be of the form /[userguid]/[functionname]/");
                    return resp;
                }

                Console.WriteLine(req.Method.ToString() + " " + req.RawUrlWithoutQuery + ": user " + userGuid + " function " + functionName);

                currDefinition = _Matcher.Match(req, userGuid, functionName, out currTrigger, out fcnRequest, out error);
                if (currDefinition == null)
                {
                    resp.StatusCode = 404;
                    resp.Data = Encoding.UTF8.GetBytes("Function not found");
                    return resp;
                }

                Message bigqResp = null;
                if (!_BigQClient.SendChannelMessageSync(
                    _Settings.MessageQueue.Channels.InvocationChannel,
                    Encoding.UTF8.GetBytes(Common.SerializeJson(fcnRequest)),
                    out bigqResp))
                {
                    Console.WriteLine("Unable to retrieve response from worker");
                    return resp;
                }

                if (bigqResp == null || bigqResp.Data == null || bigqResp.Data.Length < 1)
                {
                    Console.WriteLine("No response returned from worker");
                    return resp;
                }

                fcnResponse = Common.DeserializeJson<Response>(bigqResp.Data);
                Console.WriteLine("Function response: " + Common.SerializeJson(fcnResponse));
                Console.WriteLine("Finished executing " + fcnRequest.UserGUID + "/" + fcnRequest.FunctionName + " on " + bigqResp.SenderGUID + " [" + fcnResponse.RuntimeMs + "ms]");

                resp = new HttpResponse(req, fcnResponse.HttpStatus, fcnResponse.Headers, fcnResponse.ContentType, fcnResponse.Data); 
                return resp; 
            }
            catch (Exception e)
            {
                Console.WriteLine(Common.SerializeJson(e));
                resp = new HttpResponse(req, 500, null, "application/json", Encoding.UTF8.GetBytes(Common.SerializeJson(e)));
                return resp;
            } 
        }

        static string LandingPage()
        {
            string version = Version();
            string ret =
                "<html>" + Environment.NewLine +
                "  <head>" + Environment.NewLine +
                "    <title>WatsonFunction :: Version " + version + "</title>" + Environment.NewLine +
                "  </head>" + Environment.NewLine +
                "  <body>" + Environment.NewLine +
                "    <font face='arial'>" + Environment.NewLine +
                "      <pre>" + Environment.NewLine + Logo() + Environment.NewLine +
                "      </pre>" + Environment.NewLine +
                "      <h2>WatsonFunction</h2>" + Environment.NewLine +
                "      <h3>Version " + version + "</h3>" + Environment.NewLine +
                "      <p>Your WatsonFunction API gateway is running.</p>" + Environment.NewLine +
                "      <p>Execute a function by calling /[userguid]/[functionname] using the appropriate HTTP method.</p>" + Environment.NewLine +
                "    </font>" + Environment.NewLine +
                "  </body>" + Environment.NewLine +
                "</html>";
            return ret;
        }

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
    }
}
