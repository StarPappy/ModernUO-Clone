/***************************************************************************
 *                                Listener.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace Server.Network
{
  public class Listener : IDisposable
  {
    private Socket m_Listener;

    private Queue<Socket> m_Accepted;
    private object m_AcceptedSyncRoot;

    private SocketAsyncEventArgs m_EventArgs;

    private static Socket[] m_EmptySockets = new Socket[0];

    public static IPEndPoint[] EndPoints{ get; set; }

    public Listener(IPEndPoint ipep)
    {
      m_Accepted = new Queue<Socket>();
      m_AcceptedSyncRoot = ((ICollection)m_Accepted).SyncRoot;

      m_Listener = Bind(ipep);

      if (m_Listener == null)
        return;

      DisplayListener();

      m_EventArgs = new SocketAsyncEventArgs();
      m_EventArgs.Completed += Accept_Completion;
      Accept_Start();
    }

    private Socket Bind(IPEndPoint ipep)
    {
      Socket s = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

      try
      {
        s.LingerState.Enabled = false;
        s.ExclusiveAddressUse = false;

        s.Bind(ipep);
        s.Listen(8);

        return s;
      }
      catch (Exception e)
      {
        if (e is SocketException se)
        {
          if (se.ErrorCode == 10048)
          {
            // WSAEADDRINUSE
            Console.WriteLine("Listener Failed: {0}:{1} (In Use)", ipep.Address, ipep.Port);
          }
          else if (se.ErrorCode == 10049)
          {
            // WSAEADDRNOTAVAIL
            Console.WriteLine("Listener Failed: {0}:{1} (Unavailable)", ipep.Address, ipep.Port);
          }
          else
          {
            Console.WriteLine("Listener Exception:");
            Console.WriteLine(e);
          }
        }

        return null;
      }
    }

    private void DisplayListener()
    {
      if (!(m_Listener.LocalEndPoint is IPEndPoint ipep))
        return;

      if (ipep.Address.Equals(IPAddress.Any) || ipep.Address.Equals(IPAddress.IPv6Any))
      {
        NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
        foreach (NetworkInterface adapter in adapters)
        {
          IPInterfaceProperties properties = adapter.GetIPProperties();
          foreach (UnicastIPAddressInformation unicast in properties.UnicastAddresses)
            if (ipep.AddressFamily == unicast.Address.AddressFamily)
              Console.WriteLine("Listening: {0}:{1}", unicast.Address, ipep.Port);
        }

        /*
        try {
          Console.WriteLine( "Listening: {0}:{1}", IPAddress.Loopback, ipep.Port );
          IPHostEntry iphe = Dns.GetHostEntry( Dns.GetHostName() );
          IPAddress[] ip = iphe.AddressList;
          for ( int i = 0; i < ip.Length; ++i )
            Console.WriteLine( "Listening: {0}:{1}", ip[i], ipep.Port );
        }
        catch { }
        */
      }
      else
      {
        Console.WriteLine("Listening: {0}:{1}", ipep.Address, ipep.Port);
      }
    }

    private void Accept_Start()
    {
      bool result;

      do
      {
        try
        {
          result = !m_Listener.AcceptAsync(m_EventArgs);
        }
        catch (SocketException ex)
        {
          NetState.TraceException(ex);
          break;
        }
        catch (ObjectDisposedException)
        {
          break;
        }

        if (result)
          Accept_Process(m_EventArgs);
      } while (result);
    }

    private void Accept_Completion(object sender, SocketAsyncEventArgs e)
    {
      Accept_Process(e);

      Accept_Start();
    }

    private void Accept_Process(SocketAsyncEventArgs e)
    {
      if (e.SocketError == SocketError.Success && VerifySocket(e.AcceptSocket))
        Enqueue(e.AcceptSocket);
      else
        Release(e.AcceptSocket);

      e.AcceptSocket = null;
    }


    private bool VerifySocket(Socket socket)
    {
      try
      {
        SocketConnectEventArgs args = new SocketConnectEventArgs(socket);

        EventSink.InvokeSocketConnect(args);

        return args.AllowConnection;
      }
      catch (Exception ex)
      {
        NetState.TraceException(ex);

        return false;
      }
    }

    private void Enqueue(Socket socket)
    {
      lock (m_AcceptedSyncRoot)
      {
        m_Accepted.Enqueue(socket);
      }

      Core.Set();
    }

    private void Release(Socket socket)
    {
      try
      {
        socket.Shutdown(SocketShutdown.Both);
      }
      catch (SocketException ex)
      {
        NetState.TraceException(ex);
      }

      try
      {
        socket.Close();
      }
      catch (SocketException ex)
      {
        NetState.TraceException(ex);
      }
    }

    public Socket[] Slice()
    {
      Socket[] array;

      lock (m_AcceptedSyncRoot)
      {
        if (m_Accepted.Count == 0)
          return m_EmptySockets;

        array = m_Accepted.ToArray();
        m_Accepted.Clear();
      }

      return array;
    }

    public void Dispose(bool disposing)
    {
      if (disposing)
      {
        Socket socket = Interlocked.Exchange(ref m_Listener, null);

        socket?.Close();
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
