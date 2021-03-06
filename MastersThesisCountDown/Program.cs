﻿using System;
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
        static int anotherDisplayCount = 0;
        static int backLightCount = 0;

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

            using (var thermometer = new ADT7410(ADT7410.MeasurementMode.OneSamplePerSecond))
            {
                thermometer.Initialize();
            }

            var interruptPort = new InterruptPort(Pins.GPIO_PIN_D12, true, Port.ResistorMode.Disabled, Port.InterruptMode.InterruptEdgeLow);
            var lightPort = new SecretLabs.NETMF.Hardware.AnalogInput(Pins.GPIO_PIN_A0);

            // RTCモジュールによる割り込み処理
            interruptPort.OnInterrupt += (_, __, ___) =>
            {
                // 割り込みから秒カウンタの書き換えまで92μsかかる
                Thread.Sleep(1);

                DateTime currentTime;
                using (var clock = new RX8025NB())
                {
                    currentTime = clock.CurrentTime;
                }

                StartCounters();

                if (anotherDisplayCount <= 0)
                {
                    ShowMainDisplay(currentTime);
                }
                else
                {
                    ShowAnotherDisplay(currentTime);
                }

                ManageBackLight(lightPort);
            };

            while (true)
            {
                // メインスレッドは特になにもしない
                Thread.Sleep(1000);
            }
        }

        private static void ManageBackLight(SecretLabs.NETMF.Hardware.AnalogInput lightPort)
        {
            using (var lcd = new KanaLCD(0x38, 2, 40))
            {
                // 暗かったらバックライトを自動で消す
                lcd.BackLight = lightPort.Read() < 900 || backLightCount-- > 0;
            }
        }

        private static void StartCounters()
        {
            using (var lcd = new KanaLCD(0x38, 2, 40))
            {
                // 各カウンタの始動
                if (lcd.LeftSwitchIsPressed)
                {
                    anotherDisplayCount = 5;
                }

                if (lcd.RightSwitchIsPressed)
                {
                    backLightCount = 5;
                }
                if (backLightCount < 0)
                {
                    backLightCount = 0;
                }
            }
        }

        private static void ShowMainDisplay(DateTime currentTime)
        {
            var thesisDueTime = new DateTime(2016, 2, 15, 16, 0, 0);
            var examStartTime = new DateTime(2016, 2, 22, 8, 30, 0);
            var examEndTime = new DateTime(2016, 2, 22, 18, 30, 0);

            using (var lcd = new KanaLCD(0x38, 2, 40))
            {
                if (currentTime.Ticks < thesisDueTime.Ticks)
                {
                    var leftTime = thesisDueTime - currentTime;
                    lcd.ClearScreen();
                    lcd.SetCursor(0, 0);
                    lcd.Write("シュウロン テイシュツ マデ");
                    lcd.SetCursor(1, 0);
                    lcd.Write("アト " + leftTime.Days.ToString() + "d " + leftTime.Hours.To2DigitString() + ":"
                        + leftTime.Minutes.To2DigitString() + ":" + leftTime.Seconds.To2DigitString());
                }
                else if (currentTime.Ticks < examStartTime.Ticks)
                {
                    var leftTime = examStartTime - currentTime;
                    lcd.ClearScreen();
                    lcd.SetCursor(0, 0);
                    lcd.Write("シモン カイシ マデ");
                    lcd.SetCursor(1, 0);
                    lcd.Write("アト " + leftTime.Days.ToString() + "d " + leftTime.Hours.To2DigitString() + ":"
                        + leftTime.Minutes.To2DigitString() + ":" + leftTime.Seconds.To2DigitString());
                }
                else if (currentTime.Ticks < examEndTime.Ticks)
                {
                    var elapsedTime = currentTime - examStartTime;
                    lcd.ClearScreen();
                    lcd.SetCursor(0, 0);
                    lcd.Write("シモン カイシ カラ");
                    lcd.SetCursor(1, 0);
                    lcd.Write(elapsedTime.Hours.To2DigitString() + ":" + elapsedTime.Minutes.To2DigitString() + ":"
                        + elapsedTime.Seconds.To2DigitString() + " ケイカ");
                }
                else
                {
                    lcd.ClearScreen();
                    lcd.SetCursor(0, 0);
                    lcd.Write("シモン オツカレサマデシタ!");
                    lcd.SetCursor(1, 0);
                    lcd.Write(currentTime.Month.To2DigitString() + "/" + currentTime.Day.To2DigitString() + " "
                        + currentTime.Hour.To2DigitString() + ":" + currentTime.Minute.To2DigitString() + ":"
                        + currentTime.Second.To2DigitString());
                }
            }
        }

        private static void ShowAnotherDisplay(DateTime currentTime)
        {
            double temperature;
            using (var thermometer = new ADT7410(ADT7410.MeasurementMode.OneSamplePerSecond))
            {
                temperature = thermometer.ReadTemperature();
            }

            using (var lcd = new KanaLCD(0x38, 2, 40))
            {
                lcd.ClearScreen();
                lcd.SetCursor(0, 0);
                lcd.Write(currentTime.Year.To4DigitString() + "/"
                    + currentTime.Month.To2DigitString() + "/" + currentTime.Day.To2DigitString());
                lcd.SetCursor(1, 0);
                lcd.Write(currentTime.Hour.To2DigitString() + ":" + currentTime.Minute.To2DigitString() + ":"
                    + currentTime.Second.To2DigitString() + " "
                    + ((int)temperature).To2DigitString() + "." + (int)(temperature * 10) % 10 + "°C");
            }

            anotherDisplayCount--;
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