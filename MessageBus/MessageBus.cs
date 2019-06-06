﻿using System;
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

using BigQ.Core;
using BigQ.Server;

using WatsonFunction.MessageBus.Classes;

namespace WatsonFunction.MessageBus
{
    class Program
    {
        static Settings _Settings;
        static ServerConfiguration _BigQConfig = null;
        static List<Channel> _BigQChannels = new List<Channel>();
        static Server _BigQServer = null;

        static CancellationTokenSource _TokenSource = new CancellationTokenSource();
        static CancellationToken _Token = _TokenSource.Token;

        static bool _RunForever = true;

        static void Main(string[] args)
        {
            Console.WriteLine("WatsonFunction MessageBus starting");
            Console.WriteLine("Press ENTER to exit");

            InitializeSettings();
            InitializeBigQ();

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

                    case "clients":
                        ShowClients();
                        break;

                    case "channels":
                        ShowChannels();
                        break;

                    case "members":
                        ShowMembers();
                        break;

                    case "subscribers":
                        ShowSubscribers();
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
            Console.WriteLine("  clients          Show connected clients");
            Console.WriteLine("  channels         Show channels");
            Console.WriteLine("  members          Show channel members");
            Console.WriteLine("  subscribers      Show channel subscribers");
            Console.WriteLine("");
        } 

        static void ShowClients()
        {
            List<ServerClient> clients = _BigQServer.ListClients();
            if (clients != null && clients.Count > 0)
            {
                Console.WriteLine("Clients:");
                foreach (ServerClient curr in clients)
                {
                    Console.WriteLine("  " + curr.IpPort + " " + curr.Name + " [" + curr.ClientGUID + "]");
                }
                Console.WriteLine(clients.Count + " connected client(s)");
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("(none)");
            }
        }

        static void ShowChannels()
        {
            List<Channel> channels = _BigQServer.ListChannels();
            if (channels != null && channels.Count > 0)
            {
                Console.WriteLine("Channels:");
                foreach (Channel curr in channels)
                {
                    Console.WriteLine("  " + curr.ChannelName + " [" + curr.ChannelGUID + "]");
                }
                Console.WriteLine(channels.Count + " channel(s)");
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("(none)");
            }
        }

        static void ShowMembers()
        {
            string guid = Common.InputString("Channel GUID:", null, false);
            List<ServerClient> clients = _BigQServer.ListChannelMembers(guid);
            if (clients != null && clients.Count > 0)
            {
                Console.WriteLine("Members:");
                foreach (ServerClient curr in clients)
                {
                    Console.WriteLine("  " + curr.IpPort + " " + curr.Name + " [" + curr.ClientGUID + "]");
                }
                Console.WriteLine(clients.Count + " member(s)");
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("(none)");
            }
        }

        static void ShowSubscribers()
        {
            string guid = Common.InputString("Channel GUID:", null, false);
            List<ServerClient> clients = _BigQServer.ListChannelSubscribers(guid);
            if (clients != null && clients.Count > 0)
            {
                Console.WriteLine("Subscribers:");
                foreach (ServerClient curr in clients)
                {
                    Console.WriteLine("  " + curr.IpPort + " " + curr.Name + " [" + curr.ClientGUID + "]");
                }
                Console.WriteLine(clients.Count + " subscriber(s)");
                Console.WriteLine("");
            }
            else
            {
                Console.WriteLine("(none)");
            }
        }

        #endregion

        #region BigQ-Methods

        static void InitializeBigQ()
        {
            _BigQConfig = ServerConfiguration.Default();
            _BigQConfig.GUID = "00000000-0000-0000-0000-000000000000";
            _BigQConfig.TcpServer.Enable = true;
            _BigQConfig.TcpServer.Port = _Settings.MessageQueue.TcpPort;
            _BigQConfig.TcpServer.Debug = false;

            Channel mainChannel = new Channel();
            mainChannel.Broadcast = true;
            mainChannel.ChannelGUID = _Settings.MessageQueue.Channels.MainChannel;
            mainChannel.ChannelName = "Main Channel";
            mainChannel.Private = false;
            _BigQConfig.ServerChannels.Add(mainChannel);

            Channel healthChannel = new Channel();
            healthChannel.Broadcast = true;
            healthChannel.ChannelGUID = _Settings.MessageQueue.Channels.HealthChannel;
            healthChannel.ChannelName = "Health Channel";
            healthChannel.Private = false;
            _BigQConfig.ServerChannels.Add(healthChannel);

            Channel invokeChannel = new Channel();
            invokeChannel.Broadcast = false;
            invokeChannel.Unicast = true;
            invokeChannel.ChannelGUID = _Settings.MessageQueue.Channels.InvocationChannel;
            invokeChannel.ChannelName = "Invocation Channel";
            invokeChannel.Private = false;
            _BigQConfig.ServerChannels.Add(invokeChannel);

            _BigQServer = new Server(_BigQConfig);

            _BigQServer.Callbacks.ClientConnected = ClientConnected;
            _BigQServer.Callbacks.ClientDisconnected = ClientDisconnected;
            _BigQServer.Callbacks.ClientLogin = ClientLogin;
            _BigQServer.Callbacks.MessageReceived = MessageReceived;
            _BigQServer.Callbacks.ServerStopped = ServerStopped;
        }

        static bool ClientConnected(ServerClient client)
        {
            Console.WriteLine("Client " + client.ClientGUID + " [" + client.IpPort + "] connected");
            return true;
        }

        static bool ClientDisconnected(ServerClient client)
        {
            Console.WriteLine("Client " + client.ClientGUID + " [" + client.IpPort + "] disconnected");
            return true;
        }

        static bool ClientLogin(ServerClient client)
        {
            Console.WriteLine("Client " + client.ClientGUID + " [" + client.IpPort + "] logged in");
            return true;
        }

        static bool MessageReceived(Message msg)
        {
            // Console.WriteLine("Message received:" + Environment.NewLine + msg.ToString());
            return true;
        }

        static bool ServerStopped()
        {
            Console.WriteLine("Server stopped");
            return true;
        }

        #endregion
    }
}
