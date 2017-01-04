using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
/// Author: https://github.com/zhaojunlike
namespace OeynetSocket.SocketFramework
{
    /// <summary>
    /// Reader ��ժҪ˵����
    /// </summary>
    public class Reader
    {
        public NetworkStream stream
        {
            get;
            set;
        }

        public Reader(Socket socket)
        {
            stream = new NetworkStream(socket);
        }

        public void Close()
        {
            stream.Close();
        }

        //�����ж�ȡbytes
        public byte[] ReadBytes()
        {
            return null;
        }

        //��¼��һ�εİ�
        private byte[] totalBytes = new byte[0];
        private List<Packet> nowPackets = new List<Packet>();
        //ͬ��������ȡһ�����߶�����������ݰ�
        public List<Packet> ReadPackSync()
        {
            this.nowPackets.Clear();
            Packet packet = new Packet();
            //��ʼѭ��ȥ��ȡ���ݵ�������
            byte[] buffer = new byte[1024];
            //key ��֤�ͻ������� 32λ
            int md5KeyLength = 32;
            //header length  4λ
            int recSize = this.stream.Read(buffer, 0, buffer.Length);
            if (recSize != 0)
            {
                //�ŵ���������
                byte[] realBuffer = new byte[recSize];
                //��ȥû�õ�һ��
                Array.Copy(buffer, 0, realBuffer, 0, recSize);
                totalBytes = totalBytes.Concat(realBuffer).ToArray();//C#������byte[]���Զ��ӳ�
                //�����ǰ������������ͷ�����ݵ�ʱ��
                if (totalBytes.Length <= (md5KeyLength + 4))
                {
                    //������һ��
                    return nowPackets;
                }
                //ѭ��ȥ��������ͷ
                while (totalBytes.Length > (md5KeyLength + 4))
                {
                    Packet nowPacket = new Packet();
                    byte[] md5Key = new byte[md5KeyLength];
                    //�����е�md5 copy����
                    Array.Copy(totalBytes, 0, md5Key, 0, md5KeyLength);
                    //int
                    String serverKey = Encoding.UTF8.GetString(md5Key);
                    nowPacket.Key = serverKey;
                    if (serverKey.ToString() == "cba6c83625b358bf562e624fc903c244")//���ҵİ�,�ҵ����룬����������ʼѭ����
                    {
                        //�������������ݰ��Ĵ�С
                        byte[] packSizeBuffer = new byte[4];//�ȶ��������Ĵ�С(������MD5)
                        //ȡ�����ݰ��еİ������ݳ��ȵ�,ƫ��32����
                        Array.Copy(totalBytes, md5KeyLength, packSizeBuffer, 0, 4);
                        //ȡ���������Ĵ�С
                        int packSize = BitConverter.ToInt32(packSizeBuffer, 0);
                        //��ǰ���ݻ����г���
                        int currentSize = totalBytes.Length - md5KeyLength - packSizeBuffer.Length;//����������ж��٣�Ĭ�ϼ�����ͷMd5+PackSize
                        //�ж�����Ƿ���һ�����������ݰ�
                        if (totalBytes.Length >= packSize)
                        {
                            //����ĳ���
                            int bodyShouldSize = packSize - md5KeyLength - 4;
                            byte[] nowPacketBodyBytes = new byte[bodyShouldSize];
                            //��������
                            Array.Copy(totalBytes, md5KeyLength + 4, nowPacketBodyBytes, 0, bodyShouldSize);
                            nowPacket.Body = Encoding.UTF8.GetString(nowPacketBodyBytes);
                            //ɾ�����Ѿ����˵�����
                            byte[] newTotalBytes = new byte[totalBytes.Length - packSize];
                            Array.Copy(totalBytes, packSize, newTotalBytes, 0, totalBytes.Length - packSize);
                            totalBytes = newTotalBytes;
                            this.nowPackets.Add(nowPacket);
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        //�Ͽ��ͻ��˵�����
                        byte[] newTotalBytes = new byte[0];
                        break;
                    }
                }

            }
            else
            {
                throw new Exception("connect read error");
            }
            return nowPackets;
        }
    }
}
