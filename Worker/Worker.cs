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
using WatsonFunction.FunctionBase;
using Newtonsoft.Json; 

using BigQ.Client;
using BigQ.Core;

using WatsonFunction.Worker.Classes;

namespace WatsonFunction.Worker
{
    class Program
    {
        static Settings _Settings;
        static ClientConfiguration _BigQConfig;
        static Client _BigQClient;

        static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        static CancellationToken _Token = _TokenSource.Token;

        static bool _RunForever = true;

        static void Main(string[] args)
        {
            Console.WriteLine("WatsonFunction Worker starting"); 

            InitializeSettings();
            InitializeBigQ();
            Task.Run(() => MaintainConnection(), _Token);
            // JoinChannels();

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

                if (_BigQClient.SubscribeChannel(_Settings.MessageQueue.Channels.InvocationChannel, out msg))
                    Console.WriteLine("Subscribed to channel '" + _Settings.MessageQueue.Channels.InvocationChannel + "'");
                else Console.WriteLine("Failed to subscribe to channel '" + _Settings.MessageQueue.Channels.InvocationChannel + "'");
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while attempting to join channels:");
                Console.WriteLine(Common.SerializeJson(e));
            }
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
            Request req = Common.DeserializeJson<Request>(msg.Data); 
            Invoker inv = new Invoker(req); 
            Response resp = inv.Invoke(); 
            return Encoding.UTF8.GetBytes(Common.SerializeJson(resp));
        }

        static bool AsyncMessageReceived(Message msg)
        {
            Console.WriteLine("Message received: " + Environment.NewLine + msg.ToString());
            return true;
        }

        #endregion
    }
}
