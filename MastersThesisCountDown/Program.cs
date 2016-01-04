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
            //var lcd = new KanaLCD(0x38, 2, 40);
            var interruptPort = new InterruptPort(Pins.GPIO_PIN_D13, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);

            clock.CurrentTime = GetTimeViaHttp(9);
            clock.Interrupt = RX8025NB.InterruptMode.Pulse1Hz;
            interruptPort.OnInterrupt += (_, __, ___) => {
                Thread.Sleep(1);
                Debug.Print(clock.CurrentTime.ToString());
            };

            while (true)
            {
                Thread.Sleep(1000);
                //var duration = new TimeSpan(24, 17, 42, 18);
                //lcd.SetCursor(0, 0);
                //lcd.Write($"シュウロン テイシュツ マデ");
                //lcd.SetCursor(1, 0);
                //lcd.Write($"アト {duration.Days,2:#0}d {duration.Hours,2:#0}:{duration.Minutes,2:#0}:{duration.Seconds,2:#0}");
            }
        }

        private static void InterruptPort_OnInterrupt(uint data1, uint data2, DateTime time)
        {
            throw new NotImplementedException();
        }

        private static DateTime GetTimeViaHttp(int timeZoneOffset)
        {
            // Wifi接続待ち
            while (IPAddress.GetDefaultLocalAddress() == IPAddress.Any) ;

            var client = new NtpClient(timeZoneOffset);
            return client.GetNtpTime("ntp.nict.jp");
        }
    }


}
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 
// Copyright (c) Microsoft Corporation.  All rights reserved. 
//////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////// 

namespace System.Diagnostics
{
    //  DebuggerBrowsableState states are defined as follows:
    //      Never       never show this element
    //      Expanded    expansion of the class is done, so that all visible internal members are shown
    //      Collapsed   expansion of the class is not performed. Internal visible members are hidden
    //      RootHidden  The target element itself should not be shown, but should instead be
    //                  automatically expanded to have its members displayed.
    //  Default value is collapsed

    //  Please also change the code which validates DebuggerBrowsableState variable (in this file)
    //  if you change this enum.
    public enum DebuggerBrowsableState
    {
        Never = 0,
        Collapsed = 2,
        RootHidden = 3
    }
}