using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.Netduino;
using MastersThesisCountDown.I2C;
using MastersThesisCountDown.Extensions;

namespace MastersThesisCountDown
{
    public class Program
    {
        public static void Main()
        {
            using (var lcd = new KanaLCD(0x38, 2, 40))
            {
                lcd.Initialize();
                lcd.BackLight = true;
                lcd.Write("Initializing...");
            }

            using (var clock = new RX8025NB())
            {
                clock.Initialize();
                clock.CurrentTime = GetTimeViaHttp(9);
                clock.Interrupt = RX8025NB.InterruptMode.Pulse1Hz;
            }
            var interruptPort = new InterruptPort(Pins.GPIO_PIN_D12, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);

            interruptPort.OnInterrupt += (_, __, ___) => {
                DateTime currentTime;
                DateTime dueTime = new DateTime(2016, 2, 15, 16, 0, 0);
                using (var clock = new RX8025NB())
                {
                    currentTime = clock.CurrentTime;
                }
                var leftTime = dueTime - currentTime;
                using (var lcd = new KanaLCD(0x38, 2, 40))
                {
                    lcd.ClearScreen();
                    lcd.SetCursor(0, 0);
                    lcd.Write("シュウロン テイシュツ マデ");
                    lcd.SetCursor(1, 0);
                    lcd.Write("アト " + leftTime.Days.ToString() + "d " + leftTime.Hours.To2DigitString() + ":"
                        + leftTime.Minutes.To2DigitString() + ":" + leftTime.Seconds.To2DigitString());
                }
            };

            while (true)
            {
                Thread.Sleep(1000);
            }
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