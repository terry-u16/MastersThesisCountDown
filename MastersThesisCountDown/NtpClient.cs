using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT;

namespace MastersThesisCountDown
{
    public class NtpClient
    {
        public int TimeZoneOffset { get; }

        public NtpClient(int timeZone)
        {
            TimeZoneOffset = timeZone;
        }

        public DateTime GetNtpTime(string address, int timeout = 5000)
        {
            return GetNtpTime(Dns.GetHostEntry(address).AddressList[0], timeout);
        }

        public DateTime GetNtpTime(IPAddress address, int timeout = 5000)
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

            const int offset = 40;
            ulong intPart = 0;
            ulong fractPart = 0;

            for (int i = 0; i <= 3; i++)
            {
                intPart = (intPart << 8) | ntpData[offset + i];
            }

            for (int i = 4; i <= 7; i++)
            {
                fractPart = (fractPart << 8) | ntpData[offset + i];
            }

            ulong milliseconds = intPart * 1000 + (fractPart * 1000) / 0x100000000L;
            var timeSpan = TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
            var universalDateTime = new DateTime(1900, 1, 1) + timeSpan;

            var offsetAmount = new TimeSpan(TimeZoneOffset, 0, 0);
            var networkDateTime = (universalDateTime + offsetAmount);

            return networkDateTime;
        }
    }
}
