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
            byte[] buffer = new byte[512];
            //key ��֤�ͻ������� 4λ
            //header length  4λ
            int recSize = this.stream.Read(buffer, 0, buffer.Length);
            if (recSize != 0)
            {
                //�ŵ���������
                byte[] realBuffer = new byte[recSize];
                //��ȥû�õ�һ��
                Array.Copy(buffer, 0, realBuffer, 0, recSize);
                totalBytes = totalBytes.Concat(realBuffer).ToArray();//C#������byte[]���Զ��ӳ�
                //�����ǰ�����а�ͷ�����ݵ�ʱ��
                if (totalBytes.Length <= 8)
                {
                    //������һ��
                    return nowPackets;
                }

                while (totalBytes.Length > 8)
                {
                    Packet nowPacket = new Packet();
                    //TODO �� int�͵�key����MD5
                    int keyLength = 4;
                    byte[] md5Key = new byte[4];
                    //�����е�4λint copy����
                    Array.Copy(totalBytes, 0, md5Key, 0, 4);
                    //int
                    int serverKey = BitConverter.ToInt32(md5Key, 0);
                    nowPacket.Key = serverKey;
                    if (serverKey.ToString() == "19960615")//���ҵİ�,�ҵ����룬����������ʼѭ����
                    {
                        //�������������ݰ��Ĵ�С
                        byte[] packSizeBuffer = new byte[4];//�ȶ��������Ĵ�С(������MD5)
                        //ȡ�����ݰ��еİ������ݳ��ȵ�
                        Array.Copy(totalBytes, 4, packSizeBuffer, 0, 4);
                        //ȡ���������Ĵ�С
                        int packSize = BitConverter.ToInt32(packSizeBuffer, 0);
                        //��ǰ���ݻ����г���
                        int currentSize = totalBytes.Length - keyLength - packSizeBuffer.Length;//����������ж��٣�Ĭ�ϼ�����ͷMd5+PackSize
                        //�ж�����Ƿ���һ�����������ݰ�
                        if (totalBytes.Length >= packSize)
                        {
                            //����ĳ���
                            int bodyShouldSize = packSize - 8;
                            //Console.Write(String.Format("TotalBytes:{0},��ǰ���ݰ���{1} \n", totalBytes.Length, packSize));
                            byte[] nowPacketBodyBytes = new byte[bodyShouldSize];
                            //��������
                            Array.Copy(totalBytes, 8, nowPacketBodyBytes, 0, bodyShouldSize);
                            nowPacket.Body = Encoding.UTF8.GetString(nowPacketBodyBytes);
                            //ɾ�����Ѿ����˵�����
                            byte[] newTotalBytes = new byte[totalBytes.Length - packSize];
                            Array.Copy(totalBytes, packSize, newTotalBytes, 0, totalBytes.Length - packSize);
                            //Array.Clear(totalBytes, 0, packSize); //�˷����޷�ɾ��bytes�����е�����
                            totalBytes = newTotalBytes;
                            this.nowPackets.Add(nowPacket);
                            //Console.WriteLine(String.Format("�Ƴ����ݰ���{0},Packts����:{1}", totalBytes.Length, nowPackets.Count));
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

        private void ParsePacket()
        {
            Packet nowPacket = new Packet();
            //TODO �� int�͵�key����MD5
            int keyLength = 4;
            byte[] md5Key = new byte[4];
            //�����е�4λint copy����
            Array.Copy(totalBytes, 0, md5Key, 0, 4);
            //int
            int serverKey = BitConverter.ToInt32(md5Key, 0);
            nowPacket.Key = serverKey;
            if (serverKey.ToString() == "19960615")//���ҵİ�,�ҵ����룬����������ʼѭ����
            {
                //�������������ݰ��Ĵ�С
                byte[] packSizeBuffer = new byte[4];//�ȶ��������Ĵ�С(������MD5)
                //ȡ�����ݰ��еİ������ݳ��ȵ�
                Array.Copy(totalBytes, 4, packSizeBuffer, 0, 4);
                //ȡ���������Ĵ�С
                int packSize = BitConverter.ToInt32(packSizeBuffer, 0);
                //��ǰ���ݻ����г���
                int currentSize = totalBytes.Length - keyLength - packSizeBuffer.Length;//����������ж��٣�Ĭ�ϼ�����ͷMd5+PackSize
                //�ж�����Ƿ���һ�����������ݰ�
                if (totalBytes.Length >= packSize)
                {
                    //����ĳ���
                    int bodyShouldSize = packSize - 8;
                    Console.Write(String.Format("TotalBytes:{0},��ǰ���ݰ���{1} \n", totalBytes.Length, packSize));
                    byte[] nowPacketBodyBytes = new byte[bodyShouldSize];
                    //��������
                    Array.Copy(totalBytes, 8, nowPacketBodyBytes, 0, bodyShouldSize);
                    nowPacket.Body = Encoding.UTF8.GetString(nowPacketBodyBytes);
                    //ɾ�����Ѿ����˵�����
                    Array.Clear(totalBytes, 0, packSize);
                    this.nowPackets.Add(nowPacket);
                }
                else
                {
                    return;
                }
            }
        }

    }
}
