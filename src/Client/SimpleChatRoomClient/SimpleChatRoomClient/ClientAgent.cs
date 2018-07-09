using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace SimpleChatRoomClient
{
    class ClientAgent
    {
        private Socket m_clientsocket = null;

        public ClientAgent()
        {
            m_clientsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public int Connect(String ipaddress, String port)
        {
            IPAddress ip = IPAddress.Parse(ipaddress);

            try
            {
                m_clientsocket.Connect(new IPEndPoint(ip, Int32.Parse(port)));
            }
            catch
            {
                return -1;
            }

            return 0;
        }
    }
}
