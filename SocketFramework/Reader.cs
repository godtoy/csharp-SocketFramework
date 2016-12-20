using System;
using System.Collections;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;

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


        //ͬ��������ȡһ�����������ݰ�
        public Packet ReadPackSync()
        {
            Packet packet = new Packet();

            //��ʼѭ��ȥ��ȡ���ݵ�������
            byte[] buffer = new byte[512];
            byte[] totalBytes = new byte[0];
            //key ��֤�ͻ������� 4λ
            //header length  4λ
            int recSize = this.stream.Read(buffer, 0, buffer.Length);
            if (recSize != 0)
            {
                //�ŵ���������
                totalBytes = totalBytes.Concat(buffer).ToArray();//C#������byte[]���Զ��ӳ�
                //TODO �� int�͵�key����MD5
                int keyLength = 4;
                byte[] md5Key = new byte[4];
                //�Ѱ�ͷ��4λint copy����
                Array.Copy(totalBytes, 0, md5Key, 0, 4);
                //int
                int serverKey = BitConverter.ToInt32(md5Key, 0);
                Console.WriteLine("MD5��ͷKey:" + serverKey);
                if (serverKey.ToString() == "19960615")//���ҵİ�,�ҵ����룬����������ʼѭ����
                {
                    //�������������ݰ��Ĵ�С
                    byte[] packSizeBuffer = new byte[4];//�ȶ��������Ĵ�С(������MD5)
                    //ȡ�����ݰ��еİ������ݳ��ȵ�
                    Array.Copy(totalBytes, 4, packSizeBuffer, 0, 4);
                    //ȡ���������Ĵ�С
                    int packSize = BitConverter.ToInt32(packSizeBuffer, 0);

                    //��ǰ���峤��
                    int currentSize = recSize - keyLength - packSizeBuffer.Length;//����������ж��٣�Ĭ�ϼ�����ͷMd5+PackSize
                    int bodyShouldSize = packSize - 8;
                    Console.Write(String.Format("���ܳ��ȣ�{0},Body��ǰ��{1},Bodyʣ��:{2} \n", packSize, currentSize, bodyShouldSize - currentSize));
                    //�����ǰ�ĳ��ȼ�����ȡ����,��Ҫ���������ݰ�����
                    while (this.stream.DataAvailable && currentSize < bodyShouldSize)
                    {
                        recSize = this.stream.Read(buffer, 0, 512);
                        if (recSize > 0)
                        {
                            currentSize += recSize;//��ȡ�ɹ������е���
                            totalBytes = totalBytes.Concat(buffer).ToArray();//����ȡ��������ӵ���������
                            Console.WriteLine("current size:" + currentSize + "\n");
                        }
                        else
                        {
                            throw new Exception("connect read error");
                        }
                    }
                    //��ȡ���ˣ�ƴװ�������ݰ�
                    byte[] data = new byte[packSize - 8];
                    //����
                    Array.Copy(totalBytes, 8, data, 0, packSize - 8);//��ȡ����
                    //��������֪ͨ�¼�
                    Console.WriteLine("���ݣ�" + Encoding.UTF8.GetString(data));
                    //��Ҫ������⿪��
                    //��������
                    String dataStr = Encoding.UTF8.GetString(data);
                    packet.Key = serverKey;
                    packet.Body = dataStr;
                }
            }
            else
            {
                throw new Exception("connect read error");
            }
            return packet;
        }
    }
}
