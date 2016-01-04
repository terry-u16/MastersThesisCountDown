using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace MastersThesisCountDown.I2C
{
    public abstract class I2C : IDisposable
    {
        protected I2CDevice Device { get; }
        protected int Timeout { get; }
        public ushort Address { get; }

        public I2C(ushort address, int clockRate, int timeout)
        {
            this.Address = address;
            this.Timeout = timeout;
            this.Device = new I2CDevice(new I2CDevice.Configuration(address, clockRate));
        }

        protected void Write(params byte[] buffer)
        {
            var transactions = new I2CDevice.I2CWriteTransaction[] { I2CDevice.CreateWriteTransaction(buffer) };
            var resultLength = Device.Execute(transactions, Timeout);

            while (resultLength < buffer.Length)
            {
                var extendedBuffer = new byte[buffer.Length - resultLength];
                Array.Copy(buffer, resultLength, extendedBuffer, 0, extendedBuffer.Length);

                transactions = new I2CDevice.I2CWriteTransaction[] { I2CDevice.CreateWriteTransaction(extendedBuffer) };
                resultLength += Device.Execute(transactions, Timeout);
            }

            if (resultLength != buffer.Length)
            {
                throw new Exception("Could not write to device.");
            }
        }

        protected byte[] Read(int length)
        {
            var buffer = new byte[length];
            var transactions = new I2CDevice.I2CReadTransaction[] { I2CDevice.CreateReadTransaction(buffer) };
            var resultLength = Device.Execute(transactions, Timeout);

            if (resultLength != length)
            {
                throw new Exception("Could not read from device.");
            }

            return buffer;
        }

        protected void WriteToRegister(byte register, byte value)
        {
            Write(register, value);
        }

        protected byte[] ReadFromRegister(byte register, int length)
        {
            var buffer = new byte[length];
            var transactions = new I2CDevice.I2CTransaction[]
            {
                I2CDevice.CreateWriteTransaction(new byte[] { register }),
                I2CDevice.CreateReadTransaction(buffer)
            };

            var resultLength = Device.Execute(transactions, Timeout);

            if (resultLength != (length + 1))
            {
                throw new Exception("Could not read from device.");
            }

            return buffer;
        }

        public void Dispose()
        {
            Device.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
