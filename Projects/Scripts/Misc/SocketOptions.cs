using System.Net;
using Server.Network;

namespace Server
{
  public class SocketOptions
  {
    private static IPEndPoint[] m_ListenerEndPoints =
    {
      new IPEndPoint(IPAddress.Any, 2593) // Default: Listen on port 2593 on all IP addresses

      // Examples:
      // new IPEndPoint( IPAddress.Any, 80 ), // Listen on port 80 on all IP addresses
      // new IPEndPoint( IPAddress.Parse( "1.2.3.4" ), 2593 ), // Listen on port 2593 on IP address 1.2.3.4
    };

    public static void RegisterListeners()
    {
      TcpServer.Listeners.AddRange(m_ListenerEndPoints);
    }
  }
}
