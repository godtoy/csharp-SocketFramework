using System;
using System.Data;
using System.Net.Sockets;
using System.Threading;

namespace OeynetSocket.SocketFramework
{

    /// <summary>
    /// ���ڷ���������ÿ���ͻ��˵��߳�
    /// </summary>
    public class ClientThread
    {
        public Socket ClientSocket
        {
            get;
            set;
        }
        private Thread thread = null;
        public Writer ClientWriter
        {
            get;
            set;
        }
        public Reader ClientReader
        {
            get;
            set;
        }

        public event ReceiveEventHandler OnReceviedPacket = null;
        //�ͻ��˶Ͽ�����
        public event SocketConnectEvent OnClientDisconnected = null;

        public event ClientThreadAbortingEvent OnAbortingEvent = null;

        private bool IsConnect = false;
        public string RemoteAddress
        {
            get
            {
                return ClientSocket.RemoteEndPoint.ToString();
            }
        }

        public ClientThread(Socket client)
        {
            this.IsConnect = true;
            this.ClientSocket = client;
            this.ClientWriter = new Writer(this.ClientSocket);
            this.ClientReader = new Reader(this.ClientSocket);
        }

        /// <summary>
        /// ��ʼ�ͻ��߳�
        /// </summary>
        public void Start()
        {
            thread = new Thread(new ThreadStart(_startThread));
            thread.Name = "�ͻ��߳�";
            thread.Start();
        }
        /// <summary>
        /// �رտͻ���,��������Ȼû�йر�
        /// </summary>
        public void Stop()
        {
            this._abortThread();
            //�����¼�����װ����
            if (this.OnClientDisconnected != null)
            {
                this.OnClientDisconnected(ConnectEventType.disconnected, null);
            }
            this.ClientReader.Close();
            this.ClientWriter.Close();
        }


        /// <summary>
        /// ֹͣ�ͻ��߳�
        /// </summary>
        private void _abortThread()
        {
            //�ر��߳�
            if (this.OnAbortingEvent != null)
            {
                this.OnAbortingEvent(null);
            }
            Console.WriteLine("Thread One Exit.");
            thread.Abort();
        }


        private void _startThread()
        {
            while (ClientSocket.Connected && this.ClientReader.stream.CanRead)
            {
                try
                {
                    //ȥ����һ�����������ݰ�
                    Packet packet = this.ClientReader.ReadPackSync();
                    if (this.OnReceviedPacket != null)
                    {
                        this.OnReceviedPacket(ClientSocket, new ReceiveEventArgs(packet));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    //����쳣�͹ر�
                    this.Stop();
                    return;
                }
            }
            //�����������ҲҪ�����ӹر�
            this.Stop();
        }
    }
}
