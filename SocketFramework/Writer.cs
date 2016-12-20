using System;
using System.Data;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Linq;

/// Author: https://github.com/zhaojunlike
namespace OeynetSocket.SocketFramework
{
    /// <summary>
    /// Writer ��ժҪ˵����
    /// </summary>
    public class Writer
    {

        private NetworkStream stream = null;
        private Socket socket;

        public Writer(Socket socket)
        {
            this.socket = socket;
            stream = new NetworkStream(socket);
        }

        /// <summary>
        /// ������Ϣ
        /// </summary>
        /// <param name="msg"></param>
        public void WriteString(string msg)
        {
            Console.WriteLine("Writeһ����Ϣ:" + msg);

            byte[] data = new byte[0];
            //���Լ����������
            byte[] md5 = BitConverter.GetBytes(19960615);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(msg);
            int totalLength = md5.Length + 4 + bodyBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(totalLength);//������
            Console.Write(String.Format("���ݰ����ȣ�{0},����:", totalLength, msg));

            //���԰�ͷ�Ͱ���ֿ���
            //socket.Send(data.Concat(md5).Concat(lengthBytes).ToArray());
            data = data.Concat(md5).Concat(lengthBytes).Concat(bodyBytes).ToArray();
            this.stream.Write(data, 0, data.Length);
            //flush
            //����
            //socket.Send(bodyBytes);
        }

        public void WriteBytes(byte[] msg)
        {
            this.stream.Write(msg, 0, msg.Length);
        }

        public void WriteInt(int msg)
        {

        }
        public void WriteFlot(float msg)
        {

        }
        public void Close()
        {
            stream.Close();
        }
        public void WritePacket(Packet packet)
        {
            byte[] data = new byte[0];
            //���Լ����������
            byte[] md5 = BitConverter.GetBytes(packet.Key);
            byte[] bodyBytes = Encoding.UTF8.GetBytes(packet.Body);
            int totalLength = md5.Length + 4 + bodyBytes.Length;
            byte[] lengthBytes = BitConverter.GetBytes(totalLength);//������
            data = data.Concat(md5).Concat(lengthBytes).Concat(bodyBytes).ToArray();

            //��ͷ��β�ֿ�������
            this.WriteBytes(data.Concat(md5).Concat(lengthBytes).ToArray());
            this.WriteBytes(bodyBytes);
        }
    }
}
