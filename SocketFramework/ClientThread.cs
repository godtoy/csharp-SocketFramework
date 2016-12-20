using System;
using System.Collections.Generic;
using System.Data;
using System.Net.Sockets;
using System.Threading;

/// Author: https://github.com/zhaojunlike
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
        private Writer ClientWriter
        {
            get;
            set;
        }
        private Reader ClientReader
        {
            get;
            set;
        }

        public event ReceiveEventHandler OnReceviedPacket = null;
        //�ͻ��˶Ͽ�����
        public event SocketConnectEvent OnClientDisconnected = null;

        public event ClientThreadAbortingEvent OnAbortingEvent = null;

        private bool IsConnect = false;
        private String _remoteAddress;
        public string RemoteAddress
        {
            set
            {
                this._remoteAddress = value;
            }
            get
            {
                return this._remoteAddress;
            }
        }
        public ClientThread(Socket client)
        {
            this.IsConnect = true;
            this.ClientSocket = client;
            this.ClientWriter = new Writer(this.ClientSocket);
            this.ClientReader = new Reader(this.ClientSocket);
            this._remoteAddress = ClientSocket.RemoteEndPoint.ToString();
        }

        /// <summary>
        /// ��ʼ�ͻ��߳�
        /// </summary>
        public void Start()
        {
            thread = new Thread(new ThreadStart(_startThread));
            thread.Name = "�ͻ��߳�";
            thread.IsBackground = true;
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
                SocketEventArgs args = new SocketEventArgs();
                args.RemoteAddress = this.RemoteAddress;
                args.ClientThread = this;
                this.OnClientDisconnected(ConnectEventType.disconnected, args);
            }
            this.ClientReader.Close();
            this.ClientWriter.Close();
            this.IsConnect = false;
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
                    List<Packet> packets = this.ClientReader.ReadPackSync();
                    if (this.OnReceviedPacket != null)
                    {
                        ReceiveEventArgs args = new ReceiveEventArgs(packets);
                        args.RemoteAddress = RemoteAddress;
                        this.OnReceviedPacket(ClientSocket, args);
                    }
                }
                catch (Exception ex)
                {

                    //����쳣�͹ر�
                    Console.WriteLine("ERROR:" + ex.Message);
                    if (this.OnClientDisconnected != null)
                    {
                        SocketEventArgs args = new SocketEventArgs();
                        args.RemoteAddress = this.RemoteAddress;
                        args.ClientThread = this;
                        this.OnClientDisconnected(ConnectEventType.disconnected, args);
                    }
                    //�����ÿͻ��˰��Լ�ɾ����
                    this._abortThread();
                    this.ClientReader.Close();
                    this.ClientWriter.Close();
                    this.ClientSocket.Close();
                    this.IsConnect = false;
                    return;
                }
            }
            //�����������ҲҪ�����ӹر�
            this.Stop();
        }

        public void WritePacket(Packet packet)
        {
            try
            {
                this.ClientWriter.WritePacket(packet);
            }
            catch (Exception ex)
            {
                //����쳣�͹ر�
                Console.WriteLine("ERROR:" + ex.Message);
                if (this.OnClientDisconnected != null)
                {
                    SocketEventArgs args = new SocketEventArgs();
                    args.RemoteAddress = this.RemoteAddress;
                    args.ClientThread = this;
                    this.OnClientDisconnected(ConnectEventType.disconnected, args);
                }
                //�����ÿͻ��˰��Լ�ɾ����
                this._abortThread();
                this.ClientReader.Close();
                this.ClientWriter.Close();
                this.ClientSocket.Close();
                this.IsConnect = false;
            }
        }
    }
}
