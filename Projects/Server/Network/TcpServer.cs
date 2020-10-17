/*************************************************************************
 * ModernUO                                                              *
 * Copyright 2019-2020 - ModernUO Development Team                       *
 * Email: hi@modernuo.com                                                *
 * File: TcpServer.cs                                                    *
 *                                                                       *
 * This program is free software: you can redistribute it and/or modify  *
 * it under the terms of the GNU General Public License as published by  *
 * the Free Software Foundation, either version 3 of the License, or     *
 * (at your option) any later version.                                   *
 *                                                                       *
 * You should have received a copy of the GNU General Public License     *
 * along with this program.  If not, see <http://www.gnu.org/licenses/>. *
 *************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Server.Network
{
    public static class TcpServer
    {
        private static NetworkState m_NetworkState = NetworkState.ResumeState;

        // Sanity. 256 * 1024 * 5000 = ~1.3GB of ram
        public static int MaxConnections { get; set; } = 5000;

        // AccountLoginReject BadComm
        private static readonly byte[] socketRejected = { 0x82, 0xFF };

        public static IPEndPoint[] ListeningAddresses { get; private set; }
        public static TcpListener[] Listeners { get; private set; }
        public static List<NetState> ConnectedClients { get; } = new List<NetState>(128);

        public static ConcurrentQueue<NetState> m_ConnectedQueue = new ConcurrentQueue<NetState>();

        public static void Configure()
        {
            MaxConnections = ServerConfiguration.GetOrUpdateSetting("tcpServer.maxConnections", MaxConnections);
        }

        public static void Start()
        {
            HashSet<IPEndPoint> listeningAddresses = new HashSet<IPEndPoint>();
            List<TcpListener> listeners = new List<TcpListener>();

            foreach (var ipep in ServerConfiguration.Listeners)
            {
                var listener = CreateListener(ipep);
                if (listener == null)
                {
                    continue;
                }

                if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
                {
                    listeningAddresses.UnionWith(GetListeningAddresses(ipep));
                }
                else
                {
                    listeningAddresses.Add(ipep);
                }

                listeners.Add(listener);
                listener.BeginAcceptingSockets();
            }

            foreach (var ipep in listeningAddresses)
            {
                Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
            }

            ListeningAddresses = listeningAddresses.ToArray();
            Listeners = listeners.ToArray();
        }

        public static IEnumerable<IPEndPoint> GetListeningAddresses(IPEndPoint ipep) =>
            NetworkInterface.GetAllNetworkInterfaces().SelectMany(adapter =>
                adapter.GetIPProperties().UnicastAddresses
                    .Where(uip => ipep.AddressFamily == uip.Address.AddressFamily)
                    .Select(uip => new IPEndPoint(uip.Address, ipep.Port))
            );

        public static TcpListener CreateListener(IPEndPoint ipep)
        {
            var listener = new TcpListener(ipep);
            listener.Server.ExclusiveAddressUse = false;

            try
            {
                listener.Start(8);
                return listener;
            }
            catch (SocketException se)
            {
                // WSAEADDRINUSE
                if (se.ErrorCode == 10048)
                {
                    Console.WriteLine("Listener Failed: {0}:{1} (In Use)", ipep.Address, ipep.Port);
                }
                // WSAEADDRNOTAVAIL
                else if (se.ErrorCode == 10049)
                {
                    Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", ipep.Address, ipep.Port);
                }
                else
                {
                    Console.WriteLine("Listener Exception:");
                    Console.WriteLine(se);
                }
            }

            return null;
        }

        /**
         * Pauses the TcpServer and stops accepting new sockets.
         * This is thread-safe without using locks.
         */
        public static void Pause()
        {
            NetworkState.Pause(ref m_NetworkState);
        }

        /**
         * Resumes accepting sockets on the TcpServer.
         * This is thread-safe using a lock on the listeners
         */
        public static void Resume()
        {
            if (!NetworkState.Resume(ref m_NetworkState))
            {
                return;
            }

            lock (Listeners)
            {
                foreach (var listener in Listeners)
                {
                    listener.BeginAcceptingSockets();
                }
            }
        }

        public static void Slice()
        {
            int count = 0;

            while (count++ < 250)
            {
                if (!m_ConnectedQueue.TryDequeue(out var ns))
                {
                    break;
                }

                ConnectedClients.Add(ns);
                ns.WriteConsole("Connected. [{0} Online]", ConnectedClients.Count);
            }
        }

        private static async void BeginAcceptingSockets(this TcpListener listener)
        {
            while (true)
            {
                if (m_NetworkState.Paused)
                {
                    return;
                }

                Socket socket = null;

                try
                {
                    socket = await listener.AcceptSocketAsync();
                    if (ConnectedClients.Count >= MaxConnections)
                    {
                        socket.Send(socketRejected, SocketFlags.None);
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                        throw new MaxConnectionsException();
                    }

                    var args = new SocketConnectEventArgs(socket);
                    EventSink.InvokeSocketConnect(args);

                    if (!args.AllowConnection)
                    {
                        socket.Send(socketRejected, SocketFlags.None);
                        socket.Shutdown(SocketShutdown.Both);
                        socket.Close();
                    }
                }
                catch (MaxConnectionsException ex)
                {
                    var ipep = (IPEndPoint)socket!.RemoteEndPoint;
                    Console.WriteLine("Listener Failed: {2} ({0}:{1})", ipep.Address, ipep.Port, ex.Message);
                }
                catch
                {
                    // ignored
                }

                var ns = new NetState(socket);
                m_ConnectedQueue.Enqueue(ns);
                ns.Start();
            }
        }
    }

    internal class MaxConnectionsException : Exception
    {
        public MaxConnectionsException() : base("Maximum connections exceeded")
        {
        }
    }
}
