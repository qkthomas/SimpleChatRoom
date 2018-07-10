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
        private static readonly int MAX_MESSAGE_SIZE = 255;

        private Socket m_clientsocket = null;

        public MainWinPrintMessage m_mwpm = null;

        private Thread myThread = null;

        private CancellationTokenSource m_cts = new CancellationTokenSource();

        private static String ToUnicodeString(String str)
        {
            byte[] bytes = Encoding.Unicode.GetBytes(str);
            return Encoding.Unicode.GetString(bytes);
        }

        public ClientAgent()
        {
        }

        public int Connect(String ipaddress, String port)
        {
            m_clientsocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress ip = IPAddress.Parse(ipaddress);

            try
            {
                m_clientsocket.Connect(new IPEndPoint(ip, Int32.Parse(port)));
            }
            catch
            {
                return -1;
            }

            if (null == myThread)
            {
                myThread = new Thread(ReceiveMessage) { IsBackground = true };
                myThread.Start();
            }

            return 0;
        }

        public int SendMessage(String message)
        {
            String sent_msg = ToUnicodeString(message);

            byte[] message_bytes = Encoding.Unicode.GetBytes(sent_msg);
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

            return 0;
        }

        public void ReceiveMessageClickTest()
        {
            byte[] msg_buf = new byte[MAX_MESSAGE_SIZE];
            try
            {
                int receiveNumber = m_clientsocket.Receive(msg_buf);
            }
            catch (Exception ex)
            {
            }
            String msg = Encoding.Unicode.GetString(msg_buf);
            m_mwpm(msg);
        }

        private void ReceiveMessage()
        {
            while (true)
            {
                if (true == m_cts.IsCancellationRequested)
                {
                    m_mwpm("ReceiveMessage() thread is being terminatd");
                    return;
                }

                try
                {
                    if (true == m_clientsocket.Poll(10, SelectMode.SelectRead))
                    {
                        byte[] header_buf = new byte[1];
                        byte[] msg_buf = new byte[MAX_MESSAGE_SIZE - 1];
                        int message_len = 0;

                        int ret = m_clientsocket.Receive(header_buf, 1, SocketFlags.None);

                        if (1 != ret)
                        {
                            m_mwpm("no header or connection lost");
                            ShutDown();
                            return;
                        }
                        message_len = Convert.ToInt32(header_buf[0]);
                        int msg_buf_offset = 0;
                        while (message_len > msg_buf_offset)
                        {
                            int count = m_clientsocket.Receive(msg_buf, msg_buf_offset, message_len, SocketFlags.None);
                            msg_buf_offset += count;
                        }
                        String msg = Encoding.Unicode.GetString(msg_buf, 0, message_len);
                        m_mwpm(msg);
                    }
                }
                catch (Exception ex)
                {
                    return ;
                }
            }
        }

        public void StopReceivingThread ()
        {
            m_cts.Cancel();
        }

        public void ShutDown()
        {
            if (null != m_clientsocket)
            {
                try
                {
                    m_clientsocket.Shutdown(SocketShutdown.Both);
                    m_clientsocket.Close();
                }
                catch (Exception ex)
                {
                    return;
                }
            }

            myThread = null;
            m_clientsocket = null;
        }
    }
}
