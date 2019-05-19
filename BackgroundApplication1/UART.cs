using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.SerialCommunication;
using Windows.Storage.Streams;

//adapted from : https://raspberrypi.stackexchange.com/questions/91854/uart-raspberry-pi-iot-windows-iot-core
namespace BackgroundApplication1
{
    public sealed partial class UART
    {
        private SerialDevice SerialPort;
        private DataWriter dataWriter;

        public UART()
        {
            InitSerial();
        }

        private async void InitSerial()
        {
            string aqs = SerialDevice.GetDeviceSelector("UART0");
            var dis = await DeviceInformation.FindAllAsync(aqs);
            SerialPort = await SerialDevice.FromIdAsync(dis[0].Id);
            SerialPort.WriteTimeout = TimeSpan.FromMilliseconds(1000);
            SerialPort.ReadTimeout = TimeSpan.FromMilliseconds(1000);
            SerialPort.BaudRate = 57600;
            SerialPort.Parity = SerialParity.None;
            SerialPort.StopBits = SerialStopBitCount.One;
            SerialPort.DataBits = 8;

            dataWriter = new DataWriter();
        }

        public async void SerialSend(string txBuffer2)
        {
            /* Write a string out over serial */
            string txBuffer = txBuffer2;
            dataWriter.WriteString(txBuffer);
            uint bytesWritten = await SerialPort.OutputStream.WriteAsync(dataWriter.DetachBuffer());
        }
    }
}