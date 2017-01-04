using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Data;
using System.Collections.Generic;

/// Author: https://github.com/zhaojunlike
namespace OeynetSocket.SocketFramework
{

    /// <summary>
    /// ���������
    /// </summary>
    public class SocketServer
    {
        private Socket _server = null;
        private string _address = null;
        private int _port = 0;
        private Thread listenThread = null;
        //����ͻ�����Ϣ
        private List<ClientThread> _clients = new List<ClientThread>();
        //�������¼�
        public event SocketConnectEvent OnClientConnected = null;
        //�ͻ��˹ر������¼�
        public event ClientThreadStopEvent OnClientThreadStop = null;
        //���һ���ͻ���
        public event SocketConnectEvent OnClientDisConnected = null;
        //���ܵ������¼�
        public event ReceiveEventHandler OnServerRecevied = null;

        private Thread daemonThread;

        #region Public

        /// <summary>
        /// ������ַ
        /// </summary>
        public string Address
        {
            get
            {
                return this._address;
            }
            set
            {
                this._address = value;
            }
        }

        /// <summary>
        /// �����˿�
        /// </summary>
        public int Port
        {
            get
            {
                return this._port;
            }
            set
            {
                this._port = value;
            }
        }

        /// <summary>
        /// ��ǰ���ӵ��������Ŀͻ�����
        /// </summary>
        public int OnlineCount
        {
            get
            {
                return this._clients.Count;
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
            this._address = _ip.ToString();
            this._port = _port;
            try
            {
                this._server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this._server.Bind(new IPEndPoint(IPAddress.Parse(this._address), this._port));
            }
            catch (Exception ex)
            {
                throw new Exception("�˿�����");
            }
        }

        /// <summary>
        /// ��ʼ����
        /// </summary>
        public void Start()
        {
            try
            {
                listenThread = new Thread(new ThreadStart(this._listen));
                listenThread.Name = "�����������߳�";
                listenThread.Start();
                Console.WriteLine("Server Start.\n");
                daemonThread = new Thread(new ThreadStart(this._sendHeartActive));
                daemonThread.Name = "�����߳�";
                daemonThread.IsBackground = true;
                daemonThread.Start();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void _sendHeartActive()
        {

        }

        /// <summary>
        /// ֹͣ����
        /// </summary>
        public void Stop()
        {
            //�Ͽ�ÿһ���ͻ����ֳ�����
            for (int i = 0; i < this._clients.Count; i++)
            {
                //�رջ�ر����Ӻ͹ر��߳�
                ((ClientThread)this._clients[i]).Stop();
            }
            this._clients.Clear();
            //�رռ����߳�
            if (listenThread != null)
                listenThread.Abort();
            //�رռ����߳�
            if (daemonThread != null)
                daemonThread.Abort();
            //�ر�serversocket
            if (this._server != null)
                this._server.Close();
            Console.WriteLine("Framework log��Server Close.\n");
        }

        /// <summary>
        /// ��ָ���Ŀͻ�������Ϣ
        /// </summary>
        /// <param name="remoteAddress">�ͻ��˵�ַ</param>
        /// <param name="msg">Ҫ���͵���Ϣ</param>
        public bool Write(string remoteAddress, Packet packet)
        {
            for (int i = 0; i < this._clients.Count; i++)
            {
                ClientThread clientThread = (ClientThread)this._clients[i];
                if (clientThread.RemoteAddress.Equals(remoteAddress))
                {
                    //����
                    return clientThread.WritePacket(packet);
                }
            }
            return false;
        }

        /// <summary>
        /// �����пͻ��˷������ݰ�
        /// </summary>
        /// <param name="packet"></param>
        /// <param name="noSendRemote"></param>
        public bool WriteAll(Packet packet, String[] noSendRemote)
        {
            foreach (ClientThread item in _clients)
            {
                #region �ų����������ݵÿͻ���
                if (noSendRemote != null)
                {
                    foreach (String noItem in noSendRemote)
                    {
                        if (noItem == item.RemoteAddress)
                        {
                            if (Common.SocketIsDebug) { Console.WriteLine(Common.Log_Prefix + "Skip Send To:" + item.RemoteAddress); }
                            continue;
                        }
                    }
                }
                #endregion
                if (Common.SocketIsDebug) { Console.WriteLine(Common.Log_Prefix + "Send To:" + item.RemoteAddress); }
                //������������쳣����ô���ȵ���Exception��Ȼ�����Stop�����Ի����1������ֻҪһ������ʧ���ˣ����Ǿ�����ѭ��
                if (!item.WritePacket(packet)) { return false; }
            }
            return true;
        }


        #region
        /// <summary>
        /// �Ƴ�ĳ���ͻ���
        /// </summary>
        /// <param name="client"></param>
        internal void RemoveClient(ClientThread client)
        {
            this._clients.Remove(client);
            //�Ƴ��ͻ��˺����¼�
            if (this.OnClientDisConnected != null)
            {
                //sender
                this.OnClientDisConnected(client, null);
            }
        }


        //����
        private void _listen()
        {
            this._server.Listen(20);
            while (true)
            {
                //ѭ��accept�ͻ��˵�����Ȼ�����µ��ֳɽ������ݽ���
                Socket client = this._server.Accept();
                ClientThread clientThread = new ClientThread(client);
                //�ͻ����߳��쳣�¼�
                clientThread.OnThreadException += clientThread_OnThreadException;
                //�ͻ����߳�ֹͣ�¼�
                clientThread.OnThreadStop += clientThread_OnThreadStop;
                clientThread.OnReceviedPacket += clientThread_OnReceviedPacket;
                this._clients.Add(clientThread);
                //���������¼�
                if (this.OnClientConnected != null)
                {
                    //���ӳɹ�
                    this.OnClientConnected(clientThread, null);
                }
                clientThread.Start();
            }
        }
        /// <summary>
        /// �ͻ����߳̽��ܵ�����
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void clientThread_OnReceviedPacket(object sender, ReceiveEventArgs data)
        {
            if (this.OnServerRecevied != null)
            {
                this.OnServerRecevied(sender, data);
            }
        }

        /// <summary>
        /// �Ͽ����쳣��ͬʱ����
        /// </summary>
        /// <param name="sender"></param>
        void clientThread_OnThreadStop(object sender)
        {
            if (this.OnClientThreadStop != null)
            {
                this.OnClientThreadStop(sender);
            }
            this.RemoveClient((ClientThread)sender);
        }

        /// <summary>
        /// �ͻ����쳣
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="data"></param>
        void clientThread_OnThreadException(object sender, ClientThreadEventArgs data)
        {

        }
        #endregion
    }

}
