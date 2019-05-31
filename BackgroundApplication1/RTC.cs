using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;

//Adapted from:
//https://github.com/jeremylindsayni/Magellanic.Sensors.DS1307/blob/master/DS1307.cs


namespace BackgroundApplication1
{
    internal sealed class RTC
    {
        private byte I2C_ADDRESS = 0x68;
        private I2cDevice rtc;

        public RTC()
        {
            Task t = initI2c();
            t.Wait();
        }

        private async Task initI2c()
        {
            I2cConnectionSettings settings = new I2cConnectionSettings(I2C_ADDRESS)
            {
                BusSpeed = I2cBusSpeed.FastMode
            };

            var controller = await I2cController.GetDefaultAsync();
            rtc = controller.GetDevice(settings);
        }

        /// <summary>
        /// Windows Runtime cannot use DateTime as parameters or return values. Convert to DateTime.
        /// </summary>
        /// <returns></returns>
        public object GetCurrentTime()
        {
            byte[] readBuffer = new byte[7];

            rtc.WriteRead(new byte[] { 0x00 }, readBuffer);

            return ConvertByteBufferToDateTime(readBuffer);
        }

        public string GetCurrentTimeString()
        {
            DateTime dateTime = (DateTime)GetCurrentTime();
            return $"[{dateTime.ToLongTimeString()}]\t";
        }

        /// <summary>
        /// Send a DateTime Value.
        /// </summary>
        /// <param name="dateTime"></param>
        public void SetDateTime(object dateTime)
        {
            rtc.Write(ConvertTimeToByteArray(dateTime));
        }

        private int BinaryCodedDecimalToInteger(int value)
        {
            var lowerNibble = value & 0x0F;
            var upperNibble = value >> 4;

            var multipleOfOne = lowerNibble;
            var multipleOfTen = upperNibble * 10;

            return multipleOfOne + multipleOfTen;
        }

        private object ConvertByteBufferToDateTime(byte[] dateTimeBuffer)
        {
            var second = BinaryCodedDecimalToInteger(dateTimeBuffer[0]);
            var minute = BinaryCodedDecimalToInteger(dateTimeBuffer[1]);
            var hour = BinaryCodedDecimalToInteger(dateTimeBuffer[2]);
            var dayofWeek = BinaryCodedDecimalToInteger(dateTimeBuffer[3]);
            var day = BinaryCodedDecimalToInteger(dateTimeBuffer[4]);
            var month = BinaryCodedDecimalToInteger(dateTimeBuffer[5]);
            var year = 2000 + BinaryCodedDecimalToInteger(dateTimeBuffer[6]);

            return new DateTime(year, month, day, hour, minute, second);
        }

        private byte[] ConvertTimeToByteArray(object dateTime)
        {
            DateTime _dateTime = (DateTime)dateTime;
            var dateTimeByteArray = new byte[8];

            dateTimeByteArray[0] = 0;
            dateTimeByteArray[1] = IntegerToBinaryCodedDecimal(_dateTime.Second);
            dateTimeByteArray[2] = IntegerToBinaryCodedDecimal(_dateTime.Minute);
            dateTimeByteArray[3] = IntegerToBinaryCodedDecimal(_dateTime.Hour);
            dateTimeByteArray[4] = IntegerToBinaryCodedDecimal((byte)_dateTime.DayOfWeek);
            dateTimeByteArray[5] = IntegerToBinaryCodedDecimal(_dateTime.Day);
            dateTimeByteArray[6] = IntegerToBinaryCodedDecimal(_dateTime.Month);
            dateTimeByteArray[7] = IntegerToBinaryCodedDecimal(_dateTime.Year - 2000);

            return dateTimeByteArray;
        }

        private byte IntegerToBinaryCodedDecimal(int value)
        {
            var multipleOfOne = value % 10;
            var multipleOfTen = value / 10;

            // convert to nibbles
            var lowerNibble = multipleOfOne;
            var upperNibble = multipleOfTen << 4;

            return (byte)(lowerNibble + upperNibble);
        }
    }
}