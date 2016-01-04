using System;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace MastersThesisCountDown.I2C
{
    public class RX8025NB : I2C
    {
        private enum RegisterAddress : byte
        {
            Seconds = 0x00,
            Minutes = 0x01,
            Hours = 0x02,
            Weekdays = 0x03,
            Days = 0x04,
            Months = 0x05,
            Years = 0x06,
            DigitalOffset = 0x07,
            AlarmDMinute = 0x08,
            AlarmDHour = 0x09,
            AlarmDWeekday = 0x0A,
            AlarmWMinute = 0x0B,
            AlarmWHour = 0x0C,
            Reserved = 0x0D,
            Control1 = 0x0E,
            Control2 = 0x0F
        }

        /// <summary>
        /// 割り込みモード
        /// </summary>
        public enum InterruptMode : byte
        {
            /// <summary>OFF (Hi-z)</summary>
            Off = 0x00,
            /// <summary>OFF (LOW固定)</summary>
            LowFixed = 0x01,
            /// <summary>パルスモード（2Hz）</summary>
            Pulse2Hz = 0x02,
            /// <summary>パルスモード（1Hz）</summary>
            Pulse1Hz = 0x03,
            /// <summary>レベルモード（1秒に1度）</summary>
            LevelSecond = 0x04,
            /// <summary>レベルモード（1分に1度）</summary>
            LevelMinute = 0x05,
            /// <summary>レベルモード（1時間に1度）</summary>
            LevelHour = 0x06,
            /// <summary>レベルモード（1月に1度）</summary>
            LevelMonth = 0x07
        }

        private const byte HourMode24 = 0x20;
        private const byte PowerOnReset = 0x00;

        private InterruptMode interruptMode = InterruptMode.Off;

        public RX8025NB() : base(0x32, 100, 500)
        {
            WriteToRegister(RegisterAddress.Control1, HourMode24);
            WriteToRegister(RegisterAddress.Control2, PowerOnReset);
            Thread.Sleep(1);
        }

        public DateTime CurrentTime
        {
            get
            {
                var second = BinaryCodeToInteger(ReadFromRegister(RegisterAddress.Seconds));
                var minute = BinaryCodeToInteger(ReadFromRegister(RegisterAddress.Minutes));
                var hour = BinaryCodeToInteger(ReadFromRegister(RegisterAddress.Hours));
                var day = BinaryCodeToInteger(ReadFromRegister(RegisterAddress.Days));
                var month = BinaryCodeToInteger(ReadFromRegister(RegisterAddress.Months));
                var year = 2000 + BinaryCodeToInteger(ReadFromRegister(RegisterAddress.Years));
                return new DateTime(year, month, day, hour, minute, second);
            }
            set
            {
                WriteToRegister(RegisterAddress.Seconds, IntegerToBinaryCode(value.Second));
                WriteToRegister(RegisterAddress.Minutes, IntegerToBinaryCode(value.Minute));
                WriteToRegister(RegisterAddress.Hours, IntegerToBinaryCode(value.Hour));
                WriteToRegister(RegisterAddress.Weekdays, DayOfWeekToBinaryCode(value.DayOfWeek));
                WriteToRegister(RegisterAddress.Days, IntegerToBinaryCode(value.Day));
                WriteToRegister(RegisterAddress.Months, IntegerToBinaryCode(value.Month));
                WriteToRegister(RegisterAddress.Years, IntegerToBinaryCode(value.Year % 100));
                Thread.Sleep(1);
            }
        }

        public void ClearInterruptFlag()
        {
            WriteToRegister(RegisterAddress.Control2, PowerOnReset);
        }

        public bool HasInitialized => ((ReadFromRegister(RegisterAddress.Control2) >> 4) & 0x01) == 0;

        public InterruptMode Interrupt
        {
            get
            {
                return interruptMode;
            }
            set
            {
                WriteToRegister(RegisterAddress.Control1, (byte)((byte)value | HourMode24));
            }
        }

        private int BinaryCodeToInteger(byte bcd) => ((bcd >> 4) & 0x0F) * 10 + (bcd & 0x0F);

        private byte IntegerToBinaryCode(int value)
        {
            if (value < 0 || value >= 100)
            {
                throw new ArgumentOutOfRangeException("BCD形式に変換する数値は0～99の範囲内になければなりません。");
            }

            var digit2 = value / 10;
            var digit1 = value % 10;

            return (byte)(digit2 * 16 + digit1);
        }

        private byte DayOfWeekToBinaryCode(DayOfWeek day)
        {
            switch (day)
            {
                case DayOfWeek.Sunday:
                    return 0;
                case DayOfWeek.Monday:
                    return 1;
                case DayOfWeek.Tuesday:
                    return 2;
                case DayOfWeek.Wednesday:
                    return 3;
                case DayOfWeek.Thursday:
                    return 4;
                case DayOfWeek.Friday:
                    return 5;
                case DayOfWeek.Saturday:
                    return 6;
                default:
                    throw new ArgumentException("曜日が不正です。");
            }
        }

        private void WriteToRegister(RegisterAddress address, byte value)
        {
            Write((byte)(((byte)address) << 4), value);
        }

        private byte ReadFromRegister(RegisterAddress address)
        {
            return ReadFromRegister((byte)(((byte)address) << 4), 1)[0];
        }
    }
}
