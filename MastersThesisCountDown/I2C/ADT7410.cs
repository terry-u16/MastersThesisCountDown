using System;
using Microsoft.SPOT;
using System.Threading;

namespace MastersThesisCountDown.I2C
{
    public class ADT7410 : I2C
    {
        private enum Registers : byte
        {
            /// <summary>
            /// 温度値（上位）
            /// </summary>
            Temperature_MSB = 0x00,
            /// <summary>
            /// 温度値（下位）
            /// </summary>
            Temperature_LSB = 0x01,
            /// <summary>
            /// 設定
            /// </summary>
            Configuration = 0x03
        }

        private enum DataFormat : byte
        {
            /// <summary>
            /// 13bitモードで動作します。
            /// </summary>
            ThirteenBit = 0x00,
            /// <summary>
            /// 16bitモードで動作します。
            /// </summary>
            SixteenBit = 0x80
        }

        public enum MeasurementMode : byte
        {
            /// <summary>
            /// 連続して温度を計測します。
            /// </summary>
            Normal = 0x00,
            /// <summary>
            /// 測定時のみ通電します。
            /// </summary>
            OneShot = 0x20,
            /// <summary>
            /// 1秒間に1回のみサンプリングを行います。
            /// </summary>
            OneSamplePerSecond = 0x40,
            Shutdown = 0x60
        }

        DataFormat dataFormat = DataFormat.SixteenBit;
        MeasurementMode mode;

        byte Configuration => (byte)((byte)this.dataFormat | (byte)this.mode);

        public ADT7410()
            : this(MeasurementMode.Normal)
        {
        }

        public ADT7410(MeasurementMode mode)
            : base(0x48, 100, 500)
        {
            this.mode = mode;
        }

        public void Initialize()
        {
            this.WriteToRegister((byte)Registers.Configuration, this.Configuration);
        }

        public double ReadTemperature()
        {
            if (this.mode == MeasurementMode.OneShot)
            {
                this.WriteToRegister((byte)Registers.Configuration, this.Configuration);
                Thread.Sleep(250);
            }

            var data = this.ReadFromRegister((byte)Registers.Temperature_MSB, 2);
            long value = (long)data[0] * 256 + (long)data[1];

            if (value >= 32768)
            {
                value = value - 65536;
            }

            return value / 128.0;
        }
    }
}
