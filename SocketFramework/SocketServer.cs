using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Data;
using System.Collections.Generic;

namespace OeynetSocket.SocketFramework
{

    /// <summary>
    /// ���������
    /// </summary>
    public class SocketServer
    {
        private Socket server = null;
        private string address = null;
        private int port = 0;
        private Thread listenThread = null;
        //����ͻ�����Ϣ
        private List<ClientThread> clients = new List<ClientThread>();

        //�������¼�
        public event SocketConnectEvent OnClientConnected = null;
        //�Ͽ������¼�
        public event SocketConnectEvent OnClientDisconnected = null;
        //���ܵ������¼�
        public event ReceiveEventHandler OnServerRecevied = null;


        #region File

        /// <summary>
        /// ������ַ
        /// </summary>
        public string Address
        {
            get
            {
                return address;
            }
            set
            {
                address = value;
            }
        }

        /// <summary>
        /// �����˿�
        /// </summary>
        public int Port
        {
            get
            {
                return port;
            }
            set
            {
                port = value;
            }
        }

        /// <summary>
        /// ��ǰ���ӵ��������Ŀͻ�����
        /// </summary>
        public int OnlineCount
        {
            get
            {
                return clients.Count;
            }
        }

        /// <summary>
        /// ���ӵ���������ȫ���ͻ�
        /// </summary>
        public List<ClientThread> Clients
        {
            get
            {
                return clients;
            }
        }
        #endregion

        #region ��ȡ����IP
        public static IPAddress getIpAddress()
        {
            //��ȡ���ص�IP��ַ
            string AddressIP = string.Empty;
            IPAddress ip = null;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                    ip = _IPAddress;
                }
            }
            return ip;
        }
        #endregion


        public SocketServer(IPAddress _ip, int _port)
        {
            address = _ip.ToString();
            port = _port;
            try
            {
                server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                server.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            }
            catch (Exception ex)
            {
                throw new Exception("�˿�����");
            }
        }

        /// <summary>
        /// ��ʼ����
        /// </summary>
        public void StartListen()
        {
            try
            {
                listenThread = new Thread(new ThreadStart(this._listen));
                listenThread.Name = "�����������߳�";
                listenThread.Start();
                Console.WriteLine("Server Start.\n");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public void StopListen()
        {
            //�Ͽ�ÿһ���ͻ����ֳ�����
            for (int i = 0; i < clients.Count; i++)
            {
                ((ClientThread)clients[i]).Stop();
                //�ر�connect
                clients[i].ClientSocket.Close();
            }
            clients.Clear();
            //�رռ����߳�
            if (listenThread != null)
                listenThread.Abort();
            //�ر�serversocket
            if (server != null)
                server.Close();
            Console.WriteLine("Server Close.\n");
        }

        /// <summary>
        /// ��ָ���Ŀͻ�������Ϣ
        /// </summary>
        /// <param name="remoteAddress">�ͻ��˵�ַ</param>
        /// <param name="msg">Ҫ���͵���Ϣ</param>
        public void Write(string remoteAddress, Packet packet)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                ClientThread clientThread = (ClientThread)clients[i];
                if (clientThread.RemoteAddress.Equals(remoteAddress))
                {
                    clientThread.ClientWriter.WritePacket(packet);
                    break;
                }
            }
        }

        public void WriteAll(Packet packet, String[] noSendRemote)
        {
            foreach (ClientThread item in clients)
            {
                item.ClientWriter.WritePacket(packet);
            }
        }

        public byte[] ReadData(IPAddress remoteAddress)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                ClientThread clientThread = (ClientThread)clients[i];
                if (clientThread.RemoteAddress.Equals(remoteAddress))
                {

                    return System.Text.Encoding.Default.GetBytes("123");
                }

            }
            return System.Text.Encoding.Default.GetBytes("123");
        }

        #region
        /// <summary>
        /// �Ƴ�ĳ���ͻ���
        /// </summary>
        /// <param name="client"></param>
        internal void RemoveClient(ClientThread client)
        {
            clients.Remove(client);
        }

        //����
        private void _listen()
        {
            server.Listen(20);
            while (true)
            {
                //ѭ��accept�ͻ��˵�����Ȼ�����µ��ֳɽ������ݽ���
                Socket client = server.Accept();
                ClientThread clientThread = new ClientThread(client);

                //�ͻ����յ����ݰ�
                clientThread.OnReceviedPacket += clientThread_OnServerReceive;
                //���ӶϿ�
                clientThread.OnClientDisconnected += clientThread_OnClientDisconnected;
                clientThread.OnAbortingEvent += clientThread_OnAbortingEvent;
                clients.Add(clientThread);
                //���������¼�
                if (this.OnClientConnected != null)
                {
                    //���ӳɹ�
                    this.OnClientConnected(ConnectEventType.connected, new SocketEventArgs(client));
                }
                clientThread.Start();
            }
        }

        void clientThread_OnAbortingEvent(object sender)
        {
            //����ɾ���ͻ���
        }

        void clientThread_OnClientDisconnected(ConnectEventType type, SocketEventArgs args)
        {
            Console.WriteLine("disconnect test");
            if (this.OnClientDisconnected != null)
            {
                this.OnClientDisconnected(type, args);
            }

        }

        void clientThread_OnServerReceive(object sender, ReceiveEventArgs e)
        {
            if (this.OnServerRecevied != null)
            {
                this.OnServerRecevied(sender, e);
            }
        }

        #endregion
    }

}
