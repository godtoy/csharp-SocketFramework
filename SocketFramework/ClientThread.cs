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
        private Socket ClientSocket
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

        //�ͻ����߳̽��������¼�
        public event ClientThreadReceivedEvent OnReceviedPacket = null;

        //�ͻ����̹߳ر��¼�
        public event ClientThreadStopEvent OnThreadStop = null;

        //�����쳣�¼�
        public event ClientThreadExceptionEvent OnThreadException = null;

        //�ͻ����߳�ֹͣ�¼�
        public event ClientThreadAborEvent OnThreadAbort = null;

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
            //�����¼�����װ����
            if (this.OnThreadStop != null)
            {
                SocketEventArgs args = new SocketEventArgs();
                args.RemoteAddress = this.RemoteAddress;
                args.ClientThread = this;
                //�����¼�����
                this.OnThreadStop(this);
            }
            this.ClientReader.Close();
            this.ClientWriter.Close();
            this.IsConnect = false;
            //�Զ��ر�socket����
            this.ClientSocket.Close();
            this._abortThread();
        }

        /// <summary>
        /// ֹͣ�ͻ��߳�
        /// </summary>
        private void _abortThread()
        {
            //�ر��߳�
            if (this.OnThreadAbort != null)
            {
                this.OnThreadAbort(this);
            }
            Console.WriteLine("Thread Exit .... Remote:" + this.RemoteAddress);
            thread.Abort();
        }

        /// <summary>
        /// �̺߳���
        /// </summary>
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
                        ReceiveEventArgs args = new ReceiveEventArgs(packets, this.RemoteAddress);
                        this.OnReceviedPacket(ClientSocket, args);
                    }
                }
                catch (Exception ex)
                {
                    //����쳣�͹ر�
                    Console.WriteLine("�ͻ��˶�ȡ�����쳣ERROR:" + ex.Message);
                    if (this.OnThreadException != null)
                    {
                        ClientThreadEventArgs args = new ClientThreadEventArgs();
                        args.EventType = ClientThreadEventType.read_error;
                        this.OnThreadException(this, args);
                    }
                    //�Զ��ر�
                    this.Stop();
                    return;
                }
            }
        }

        /// <summary>
        /// �������ݰ�
        /// </summary>
        /// <param name="packet"></param>
        /// <returns></returns>
        public bool WritePacket(Packet packet)
        {
            try
            {
                this.ClientWriter.WritePacket(packet);
                return true;
            }
            catch (Exception ex)
            {
                //����쳣�͹ر�
                if (Common.SocketIsDebug) { Console.WriteLine(Common.Log_Prefix + "д�����쳣����ERROR:" + ex.Message); }
                if (this.OnThreadException != null)
                {
                    ClientThreadEventArgs args = new ClientThreadEventArgs();
                    args.EventType = ClientThreadEventType.write_error;
                    this.OnThreadException(this, args);
                }
                //�����ÿͻ��˰��Լ�ɾ����
                this.Stop();
            }
            return false;
        }
    }
}
