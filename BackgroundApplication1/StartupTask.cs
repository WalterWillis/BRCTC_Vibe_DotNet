using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace BackgroundApplication1
{
    public sealed class StartupTask : IBackgroundTask
    {
        private SpiDevice ADC;
        private SpiDevice Gyro;
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Task t = InitSpi();
            t.Wait(); // wait until init is complete
            TestGyro();
            TestRegRead();
            GetData();
        }

        public bool TestGyro()
        {
            for (int i = 0; i < 50; i++)
            {
                short testbytes = (short)Adis16460.PROD_ID; // Product ID Register
                byte[] readbuffer = new byte[2];
                byte[] writeBuffer = BitConverter.GetBytes(testbytes);

                Gyro.TransferFullDuplex(writeBuffer, readbuffer);

                readbuffer = readbuffer.Reverse().ToArray();

                short result = BitConverter.ToInt16(readbuffer, 0);

                System.Diagnostics.Debug.WriteLine("Device ID: " + result);

                if (result == 16460)
                    return true;
            }

            return false;
        }

        public bool TestRegRead()
        {
            for (int i = 0; i < 50; i++)
            {
                short testbytes = (short)Adis16460.PROD_ID; // Product ID Register

                short result = GyroRegRead(testbytes);

                System.Diagnostics.Debug.WriteLine("Device ID: " + result);

                if (result == 16460)
                    return true;
            }

            return false;
        }

        private async Task InitSpi()
        {
            try
            {
                var adcSettings = new SpiConnectionSettings(0)                         // Chip Select line 0
                {
                    ClockFrequency = 500 * 100,                                    // Don't exceed 3.6 MHz
                    Mode = SpiMode.Mode0,
                    SharingMode = SpiSharingMode.Shared
                };

                string spiAqs = SpiDevice.GetDeviceSelector("SPI0");                /* Find the selector string for the SPI bus controller          */
                var controller = await SpiController.GetDefaultAsync();    /* Find the SPI bus controller device with our selector string  */
                ADC = controller.GetDevice(adcSettings);     /* Create an SpiDevice with our bus controller and SPI settings */
                System.Diagnostics.Debug.WriteLine("Init ADC successful");

                var gyroSettings = new SpiConnectionSettings(1)                         // Chip Select line 0
                {
                    ClockFrequency = 500 * 100,                                    // Don't exceed 3.6 MHz
                    Mode = SpiMode.Mode3,
                    SharingMode = SpiSharingMode.Shared
                };

                Gyro = controller.GetDevice(gyroSettings);   /* Create an SpiDevice with our bus controller and SPI settings */
                System.Diagnostics.Debug.WriteLine("Init Gyro successful");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("InitSpi threw " + ex);
            }
        }

        public void GetData()
        {
            //GryoRegWrite((short)Adis16460.MSC_CTRL, 0x00C1);  // Enable Data Ready, set polarity
           
            GryoRegWrite((short)Adis16460.FLTR_CTRL, 0x500); // Set digital filter
        
            GryoRegWrite((short)Adis16460.DEC_RATE, 0); // Disable decimation

            while (true)
            {
                double[] data = BurstRead();

                int channels = 8;

                double[] adcValues = new double[channels];
                for (int channel = 0; channel < channels; channel++)
                    adcValues[channel] = ADCRead(channel);

                System.Diagnostics.Debug.WriteLine("Gyro Values: " + string.Join(", ", data));
                System.Diagnostics.Debug.WriteLine("ADC Values:" + string.Join(", ", adcValues));
            }
        }

        /// <summary>
        /// Converts the array of 3 bytes into an integer.
        /// Uses the 10 least significant bits, and discards the rest
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static int ConvertToInt([ReadOnlyArray()] byte[] rdata)
        {
            byte[] data = (byte[])rdata.Clone();
            int result = 0;
            result = data[1] & 0x03;
            result <<= 8;
            result += data[2];
            return result;
        }


        public void GryoRegWrite(short regAddr, short regData)
        {

            byte[] regAddrBytes = BitConverter.GetBytes(regAddr);
            byte[] dataBytes = BitConverter.GetBytes(regData);

            //Split words into chars and place into char array
            byte[] writeBuffer = {regAddrBytes[0], regAddrBytes[1], dataBytes[0], dataBytes[1]};

            byte[] readBuffer = new byte[4];
            //Write to SPI bus
            Gyro.TransferFullDuplex(writeBuffer, readBuffer);
        }

        public short GyroRegRead(short regAddr)
        {
            // Write register address to be read
            byte[] writeBuffer = BitConverter.GetBytes(regAddr);
            byte[] readBuffer = new byte[2];


            Gyro.TransferFullDuplex(writeBuffer, readBuffer);

            short result = BitConverter.ToInt16(readBuffer.Reverse().ToArray(),0);

            // Read data from requested register
            return result;
        }

        public double[] BurstRead()
        {
            byte[] burstdata = new byte[22]; //+2 bytes for the address selection
            short[] burstwords = new short[10];

            byte[] burstTrigger = { 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            Gyro.TransferFullDuplex(burstTrigger, burstdata);

            byte[] data = (byte[])burstdata.Skip(2).ToArray();
            int counter = 0;

            for (int i = 0; i < data.Length; i += 2)
            {
                byte[] bytes = data.Skip(i).Take(2).Reverse().ToArray();
                burstwords[counter++] = BitConverter.ToInt16(bytes, 0);
            }

            var checksum = GetChecksum(burstwords);

            double[] burstResults = new double[10];
            burstResults[0] = burstwords[0]; //DIAG_STAT
            burstResults[1] = gyroScale(burstwords[1]);//XGYRO
            burstResults[2] = gyroScale(burstwords[2]); //YGYRO
            burstResults[3] = gyroScale(burstwords[3]); //ZGYRO
            burstResults[4] = accelScale(burstwords[4]); //XACCEL
            burstResults[5] = accelScale(burstwords[5]); //YACCEL
            burstResults[6] = accelScale(burstwords[6]); //ZACCEL
            burstResults[7] = tempScale(burstwords[7]); //TEMP_OUT
            burstResults[8] = burstwords[8]; //SMPL_CNTR
            burstResults[9] = burstwords[9]; //CHECKSUM

            return burstResults;
        }

        public short GetChecksum([ReadOnlyArray()] short[] rdata)
        {
            short[] data = (short[])rdata;

            short s = 0;
            for (int i = 0; i < 9; i++) // Checksum value is not part of the sum!!
            {
                s += (short)(data[i] & 0xFF); // Count lower byte
                s += (short)((data[i] >> 8) & 0xFF); // Count upper byte
            }

            return s;
        }


        /////////////////////////////////////////////////////////////////////////////////////////
        // Converts accelerometer data output from the regRead() function and returns
        // acceleration in mg's
        /////////////////////////////////////////////////////////////////////////////////////////
        // sensorData - data output from regRead()
        // return - (double) signed/scaled accelerometer in g's
        /////////////////////////////////////////////////////////////////////////////////////////
        double accelScale(short sensorData)
        {
            double finalData = sensorData * 0.00025; // Multiply by accel sensitivity (25 mg/LSB)
            return finalData;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Converts gyro data output from the regRead() function and returns gyro rate in deg/sec
        /////////////////////////////////////////////////////////////////////////////////////////////
        // sensorData - data output from regRead()
        // return - (double) signed/scaled gyro in degrees/sec
        /////////////////////////////////////////////////////////////////////////////////////////
        double gyroScale(short sensorData)
        {
            double finalData = sensorData * 0.005;
            return finalData;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Converts temperature data output from the regRead() function and returns temperature 
        // in degrees Celcius
        /////////////////////////////////////////////////////////////////////////////////////////////
        // sensorData - data output from regRead()
        // return - (double) signed/scaled temperature in degrees Celcius
        /////////////////////////////////////////////////////////////////////////////////////////
        double tempScale(short sensorData)
        {
            int signedData = 0;
            int isNeg = sensorData & 0x8000;
            if (isNeg == 0x8000) // If the number is negative, scale and sign the output
                signedData = sensorData - 0xFFFF;
            else
                signedData = sensorData;
            double finalData = (signedData * 0.05) + 25; // Multiply by temperature scale and add 25 to equal 0x0000
            return finalData;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Converts integrated angle data output from the regRead() function and returns delta angle in degrees
        /////////////////////////////////////////////////////////////////////////////////////////////
        // sensorData - data output from regRead()
        // return - (double) signed/scaled delta angle in degrees
        /////////////////////////////////////////////////////////////////////////////////////////
        double deltaAngleScale(short sensorData)
        {
            double finalData = sensorData * 0.005; // Multiply by delta angle scale (0.005 degrees/LSB)
            return finalData;
        }

        /////////////////////////////////////////////////////////////////////////////////////////////
        // Converts integrated velocity data output from the regRead() function and returns delta velocity in mm/sec
        /////////////////////////////////////////////////////////////////////////////////////////////
        // sensorData - data output from regRead()
        // return - (double) signed/scaled delta velocity in mm/sec
        /////////////////////////////////////////////////////////////////////////////////////////
        double deltaVelocityScale(short sensorData)
        {
            double finalData = sensorData * 2.5; // Multiply by velocity scale (2.5 mm/sec/LSB)
            return finalData;
        }


        public double ADCRead(int channel)
        {

            byte[] readBuffer = new byte[3]; /* Buffer to hold read data*/
            //byte[] writeBuffer = new byte[3] { MCP3208_CONFIG, (byte)(channel | 0x00), 0x00 };

            short _fullByte = (short)(0x0400 | (channel << 6));

            byte[] writeBuffer = { (byte)((_fullByte >> 8) & 0xFF), (byte)(_fullByte & 0xFF), 0x00};

            ADC.TransferFullDuplex(writeBuffer, readBuffer); /* Read data from the ADC                           */
            double adcValue = ConvertADCResult(readBuffer); /* Convert the returned bytes into an integer value */
            return adcValue;
        }

        public double ConvertADCResult([ReadOnlyArray()] byte[] rdata)
        {
            byte[] data = (byte[])rdata.Clone();
            double result = 0;

            int med = 0;
            med = data[1] & 0x0F;
            med <<= 8;
            med += data[2];

            //result = ((data[1] & 15) << 8) + data[2];
            //result = (data[1] & 0xFF) << 8 | (data[2] & 0xFF);

            result = med / (double)4095 * 3.3;

            return result;
        }
        private const short MCP3208_CONFIG = 0x06; /* 00000110 single mode channel configuration data for the MCP3208 */

        /// <summary>
        /// Gyro is MSB first. These bytes are reversed for SPI.
        /// </summary>
        internal enum Adis16460 : short //Gyro 
        {
            //// User Register Memory Map from Table 6
            FLASH_CNT = 0x0000, //Flash memory write count
            DIAG_STAT = 0x0200, //Diagnostic and operational status
            X_GYRO_LOW = 0x0400, //X-axis gyroscope output00, lower word
            X_GYRO_OUT = 0x0600, //X-axis gyroscope output00, upper word
            Y_GYRO_LOW = 0x0800, //Y-axis gyroscope output00, lower word
            Y_GYRO_OUT = 0x0A00, //Y-axis gyroscope output00, upper word
            Z_GYRO_LOW = 0x0C00, //Z-axis gyroscope output00, lower word
            Z_GYRO_OUT = 0x0E00, //Z-axis gyroscope output00, upper word
            X_ACCL_LOW = 0x1000, //X-axis accelerometer output00, lower word
            X_ACCL_OUT = 0x1200, //X-axis accelerometer output00, upper word
            Y_ACCL_LOW = 0x1400, //Y-axis accelerometer output00, lower word
            Y_ACCL_OUT = 0x1600, //Y-axis accelerometer output00, upper word
            Z_ACCL_LOW = 0x1800, //Z-axis accelerometer output00, lower word
            Z_ACCL_OUT = 0x1A00, //Z-axis accelerometer output00, upper word
            SMPL_CNTR = 0x1C00, //Sample Counter00, MSC_CTRL[3:2=11
            TEMP_OUT = 0x1E00, //Temperature output (internal00, not calibrated)
            X_DELT_ANG = 0x2400, //X-axis delta angle output
            Y_DELT_ANG = 0x2600, //Y-axis delta angle output
            Z_DELT_ANG = 0x2800, //Z-axis delta angle output
            X_DELT_VEL = 0x2A00, //X-axis delta velocity output
            Y_DELT_VEL = 0x2C00, //Y-axis delta velocity output
            Z_DELT_VEL = 0x2E00, //Z-axis delta velocity output
            MSC_CTRL = 0x3200, //Miscellaneous control
            SYNC_SCAL = 0x3400, //Sync input scale control
            DEC_RATE = 0x3600, //Decimation rate control
            FLTR_CTRL = 0x3800, //Filter control00, auto-null record time
            GLOB_CMD = 0x3E00, //Global commands
            XGYRO_OFF = 0x4000, //X-axis gyroscope bias offset error
            YGYRO_OFF = 0x4200, //Y-axis gyroscope bias offset error
            ZGYRO_OFF = 0x4400, //Z-axis gyroscope bias offset factor
            XACCL_OFF = 0x4600, //X-axis acceleration bias offset factor
            YACCL_OFF = 0x4800, //Y-axis acceleration bias offset factor
            ZACCL_OFF = 0x4A00, //Z-axis acceleration bias offset factor
            LOT_ID1 = 0x5200, //Lot identification number
            LOT_ID2 = 0x5400, //Lot identification number
            PROD_ID = 0x5600, //Product identifier
            SERIAL_NUM = 0x5800, //Lot-specific serial number
            CAL_SGNTR = 0x6000, //Calibration memory signature value
            CAL_CRC = 0x6200, //Calibration memory CRC values
            CODE_SGNTR = 0x6400, //Code memory signature value
            CODE_CRC = 0x6600 //Code memory CRC values}
        }

        internal enum MCP3208 : byte //ADC
        {
            SINGLE_0 = 0x08, //0b1000, /* single channel 0 */
            SINGLE_1 = 0x09, //0b1001, /* single channel 1 */
            SINGLE_2 = 0x0A, //0b1010, /* single channel 2 */
            SINGLE_3 = 0x0B, //0b1011, /* single channel 3 */
            SINGLE_4 = 0x0C, //0b1100, /* single channel 4 */
            SINGLE_5 = 0x0D, //0b1101, /* single channel 5 */
            SINGLE_6 = 0x0E, //0b1110, /* single channel 6 */
            SINGLE_7 = 0x0F, //0b1111, /* single channel 7 */
            DIFF_0PN = 0x00, //0b0000, /* differential channel 0 (input 0+,1-) */
            DIFF_0NP = 0x01, //0b0001, /* differential channel 0 (input 0-,1+) */
            DIFF_1PN = 0x02, //0b0010, /* differential channel 1 (input 2+,3-) */
            DIFF_1NP = 0x03, //0b0011, /* differential channel 1 (input 2-,3+) */
            DIFF_2PN = 0x04, //0b0100, /* differential channel 2 (input 4+,5-) */
            DIFF_2NP = 0x05, //0b0101, /* differential channel 2 (input 5-,5+) */
            DIFF_3PN = 0x06,//0b0110, /* differential channel 3 (input 6+,7-) */
            DIFF_3NP = 0x07 //0b0111  /* differential channel 3 (input 6-,7+) */
        }
    }
}
