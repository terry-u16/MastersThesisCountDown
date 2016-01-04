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
            /// ���x�l�i��ʁj
            /// </summary>
            Temperature_MSB = 0x00,
            /// <summary>
            /// ���x�l�i���ʁj
            /// </summary>
            Temperature_LSB = 0x01,
            /// <summary>
            /// �ݒ�
            /// </summary>
            Configuration = 0x03
        }

        private enum DataFormat : byte
        {
            /// <summary>
            /// 13bit���[�h�œ��삵�܂��B
            /// </summary>
            ThirteenBit = 0x00,
            /// <summary>
            /// 16bit���[�h�œ��삵�܂��B
            /// </summary>
            SixteenBit = 0x80
        }

        public enum MeasurementMode : byte
        {
            /// <summary>
            /// �A�����ĉ��x���v�����܂��B
            /// </summary>
            Normal = 0x00,
            /// <summary>
            /// ���莞�̂ݒʓd���܂��B
            /// </summary>
            OneShot = 0x20,
            /// <summary>
            /// 1�b�Ԃ�1��̂݃T���v�����O���s���܂��B
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
