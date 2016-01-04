using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using MastersThesisCountDown.I2C;

namespace MastersThesisCountDown
{
    public class Program
    {
        public static void Main()
        {
            var clock = new RX8025NB();
            var lcd = new KanaLCD(0x38, 2, 40);


            clock.Interrupt = RX8025NB.InterruptMode.Pulse1Hz;

            while (true)
            {
                var duration = new TimeSpan(24, 17, 42, 18);
                lcd.SetCursor(0, 0);
                lcd.Write($"シュウロン テイシュツ マデ");
                lcd.SetCursor(1, 0);
                lcd.Write($"アト {duration.Days,2:#0}d {duration.Hours,2:#0}:{duration.Minutes,2:#0}:{duration.Seconds,2:#0}");
            }
        }

        private DateTime GetTimeViaHttp(TimeSpan timeout)
        {
            // Wifi接続待ち
            while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any) ;

            var client = HttpWebRequest.Create()
        }
    }
}
