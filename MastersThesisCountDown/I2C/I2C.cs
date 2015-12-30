using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace MastersThesisCountDown.I2C
{
    public abstract class I2C : IDisposable
    {
        private I2CDevice device;
        private int timeout;
        public ushort Address { get; }

        public I2C(ushort address, int clockRate, int timeout)
        {
            this.Address = address;
            this.timeout = timeout;
            this.device = new I2CDevice(new I2CDevice.Configuration(address, clockRate));
        }

        protected void Write(params byte[] buffer)
        {
            var transactions = new I2CDevice.I2CWriteTransaction[] { I2CDevice.CreateWriteTransaction(buffer) };
            var resultLength = device.Execute(transactions, timeout);

            while (resultLength < buffer.Length)
            {
                var extendedBuffer = new byte[buffer.Length - resultLength];
                Array.Copy(buffer, resultLength, extendedBuffer, 0, extendedBuffer.Length);

                transactions = new I2CDevice.I2CWriteTransaction[] { I2CDevice.CreateWriteTransaction(extendedBuffer) };
                resultLength += device.Execute(transactions, timeout);
            }

            if (resultLength != buffer.Length)
            {
                throw new Exception("Could not write to device.");
            }
        }

        protected void Read(params byte[] buffer)
        {
            var transactions = new I2CDevice.I2CReadTransaction[] { I2CDevice.CreateReadTransaction(buffer) };
            var resultLength = device.Execute(transactions, timeout);

            if (resultLength != buffer.Length)
            {
                throw new Exception("Could not read from device.");
            }
        }

        protected void WriteToRegister(byte register, byte value)
        {
            Write(register, value);
        }

        protected byte[] ReadFromRegister(byte register, int length)
        {
            var buffer = new byte[length];
            Write(register);
            Read(buffer);
            return buffer;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
