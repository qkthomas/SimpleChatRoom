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
    public delegate void MainWinPrintMessage(String message);

    class ClientAgent
    {
        private static readonly int MAX_MESSAGE_SIZE = 512;

        private Socket m_clientsocket = null;

        public MainWinPrintMessage m_mwpm = null;

        private Thread myThread = null;

        private static String ToUTF8String(String str)
        {
            byte[] bytes = Encoding.Default.GetBytes(str);
            return Encoding.UTF8.GetString(bytes);
        }

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

        public int SendMessage(String message)
        {
            String sent_msg = ToUTF8String(message);

            byte[] message_bytes = Encoding.UTF8.GetBytes(sent_msg);
            byte[] sent_bytes = new byte[message_bytes.Length + 1];
            sent_bytes[0] = (byte)message_bytes.Length;
            Buffer.BlockCopy(message_bytes, 0, sent_bytes, 1, message_bytes.Length);

            try
            {
                m_clientsocket.Send(sent_bytes);
            }
            catch
            {
                return -1;
            }

            if (null == myThread)
            {
                myThread = new Thread(ReceiveMessage);
                myThread.Start();
            }

            return 0;
        }

        void ReceiveMessage()
        {
            while (true)
            {
                if (true == m_clientsocket.Poll(10, SelectMode.SelectRead))
                {
                    byte[] header_buf = new byte[1];
                    if (1 != m_clientsocket.Receive(header_buf, 1, SocketFlags.None))
                    {
                        m_mwpm("no header error");
                    }
                    int remaining_message_len = Convert.ToInt32(header_buf[0]);
                    byte[] msg_buf = new byte[MAX_MESSAGE_SIZE-1];
                    int msg_buf_offset = 0;
                    while (remaining_message_len > 0)
                    {
                        int count = m_clientsocket.Receive(msg_buf, msg_buf_offset, remaining_message_len, SocketFlags.None);
                        msg_buf_offset += count;
                        remaining_message_len -= count;
                    }
                    String msg = Encoding.UTF8.GetString(msg_buf);
                    m_mwpm(msg);
                }
            }
        }
    }
}
