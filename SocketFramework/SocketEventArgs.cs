using System;
using System.IO;
using System.Net.Sockets;

/// Author: https://github.com/zhaojunlike
namespace OeynetSocket.SocketFramework
{

    /// <summary>
    /// ΪOnConnect��OnDisconnect�¼��ṩ����
    /// </summary>
    public class SocketEventArgs : System.EventArgs
    {
        private string address = null;
        public ClientThread ClientThread
        {
            get;
            set;
        }

        /// <summary>
        /// Զ�̼����IP��ַ�Ͷ˿�
        /// </summary>
        public string RemoteAddress
        {
            set
            {
                this.address = value;
            }
            get
            {
                return address;
            }
        }
        public SocketEventArgs()
        {

        }
        public SocketEventArgs(ClientThread child)
        {
            this.ClientThread = child;
            this.address = child.RemoteAddress;
        }
    }
}
