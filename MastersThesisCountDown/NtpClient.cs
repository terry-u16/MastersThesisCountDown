using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT;

namespace MastersThesisCountDown
{
    public class NtpClient
    {
        public DateTime GetNtpTime(string address, int timeout = 10000)
        {
            return GetNtpTime(Dns.GetHostEntry(address).AddressList[0], timeout);
        }

        public DateTime GetNtpTime(IPAddress address, int timeout = 10000)
        {
            var endPoint = new IPEndPoint(address, 123);

            var ntpData = new byte[48];

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.SendTimeout = timeout;
                socket.ReceiveTimeout = timeout;
                socket.Connect(endPoint);

                // プロトコルバージョンを設定
                ntpData[0] = 0x1B;

                socket.Send(ntpData);
                socket.Receive(ntpData);
                socket.Close();
            }

        }
    }
}
