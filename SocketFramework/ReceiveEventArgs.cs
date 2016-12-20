using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace OeynetSocket.SocketFramework
{
    /// <summary>
    /// ΪOnReceive�¼��ṩ����
    /// </summary>
    public class ReceiveEventArgs : EventArgs
    {
        public Packet PacketData
        {
            get;
            set;
        }
        public ReceiveEventArgs(Packet packet)
        {
            this.PacketData = packet;
        }
    }
}
