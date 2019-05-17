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
            GetData();
        }

        private async Task InitSpi()
        {
            try
            {
                var adcSettings = new SpiConnectionSettings(0)                         // Chip Select line 0
                {
                    ClockFrequency = 500 * 1000,                                    // Don't exceed 3.6 MHz
                    Mode = SpiMode.Mode3,
                };

                string spiAqs = SpiDevice.GetDeviceSelector("SPI0");                /* Find the selector string for the SPI bus controller          */
                var controller = await SpiController.GetDefaultAsync();    /* Find the SPI bus controller device with our selector string  */
                ADC = controller.GetDevice(adcSettings);     /* Create an SpiDevice with our bus controller and SPI settings */
                System.Diagnostics.Debug.WriteLine("Init ADC successful");

                var gyroSettings = new SpiConnectionSettings(1)                         // Chip Select line 0
                {
                    ClockFrequency = 500 * 1000,                                    // Don't exceed 3.6 MHz
                    Mode = SpiMode.Mode3,
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
            GryoRegWrite((byte)Adis16460.MSC_CTRL, 0xC1);  // Enable Data Ready, set polarity
           
            GryoRegWrite((byte)Adis16460.FLTR_CTRL, 0x500); // Set digital filter
        
            GryoRegWrite((byte)Adis16460.DEC_RATE, 0); // Disable decimation

            while (true)
            {
                double[] data = BurstRead();

                int channels = 8;

                double[] adcValues = new double[channels];
                for (int channel = 0; channel < channels; channel++)
                    adcValues[channel] = ADCRead(channel);

                System.Diagnostics.Debug.WriteLine("Gyro Values:[{0}]", string.Join(", ", data));
                System.Diagnostics.Debug.WriteLine("ADC Values[{0}]", string.Join(", ", adcValues));
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


        public void GryoRegWrite(byte regAddr, UInt16 regData)
        {

            UInt16 addr = (UInt16)(((regAddr & 0x7F) | 0x80) << 8);
            UInt16 lowWord = (UInt16)(addr | (regData & 0xFF));
            UInt16 highWord = (UInt16)((addr | 0x100) | ((regData >> 8) & 0xFF));

            //Split words into chars and place into char array
            byte[] writeBuffer = { (byte)(highWord >> 8), (byte)(highWord & 0xFF),
                (byte)(lowWord >> 8), (byte)(lowWord & 0xFF) };

            byte[] readBuffer = new byte[4];
            //Write to SPI bus
            Gyro.TransferFullDuplex(writeBuffer, readBuffer);
        }

        public byte[] GyroRegRead(SpiDevice gyro, byte regAddr)
        {
            // Write register address to be read
            byte[] writeBuffer = { regAddr, 0x00 };// Write 0x00 to the SPI bus fill the 16 bit transaction requirement
            byte[] readBuffer = new byte[2];


            gyro.TransferFullDuplex(writeBuffer, readBuffer);

            // Read data from requested register
            return readBuffer;
        }

        public double[] BurstRead()
        {
            byte[] burstdata = new byte[22]; //+2 bytes for the address selection
            double[] burstwords = new double[10];

            byte[] burstTrigger = { 0x3E, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            Gyro.TransferFullDuplex(burstTrigger, burstdata);

            var checksum = GetChecksum(burstdata);

            burstwords[0] = (UInt16)((burstdata[2] << 8) | (burstdata[3] & 0xFF)); //DIAG_STAT
            burstwords[1] = gyroScale((UInt16)((burstdata[4] << 8) | (burstdata[5] & 0xFF)));//XGYRO
            burstwords[2] = gyroScale((UInt16)((burstdata[6] << 8) | (burstdata[7] & 0xFF))); //YGYRO
            burstwords[3] = gyroScale((UInt16)((burstdata[8] << 8) | (burstdata[9] & 0xFF))); //ZGYRO
            burstwords[4] = accelScale((UInt16)((burstdata[10] << 8) | (burstdata[11] & 0xFF))); //XACCEL
            burstwords[5] = accelScale((UInt16)((burstdata[12] << 8) | (burstdata[13] & 0xFF))); //YACCEL
            burstwords[6] = accelScale((UInt16)((burstdata[14] << 8) | (burstdata[15] & 0xFF))); //ZACCEL
            burstwords[7] = tempScale((UInt16)((burstdata[16] << 8) | (burstdata[17] & 0xFF))); //TEMP_OUT
            burstwords[8] = (UInt16)((burstdata[18] << 8) | (burstdata[19] & 0xFF)); //SMPL_CNTR
            burstwords[9] = (UInt16)((burstdata[20] << 8) | (burstdata[21] & 0xFF)); //CHECKSUM

            return burstwords;
        }

        public UInt16 GetChecksum([ReadOnlyArray()] byte[] rdata)
        {
            byte[] data = (byte[])rdata.Clone();
            UInt16 sum = 0;
            foreach (byte b in data.Skip(2).Take(18)) //take 18 bytes, skipping the empty leading bytes and the trailing checksum bytes
            {
                sum += b;
            }

            return sum;
        }


        /////////////////////////////////////////////////////////////////////////////////////////
        // Converts accelerometer data output from the regRead() function and returns
        // acceleration in mg's
        /////////////////////////////////////////////////////////////////////////////////////////
        // sensorData - data output from regRead()
        // return - (double) signed/scaled accelerometer in g's
        /////////////////////////////////////////////////////////////////////////////////////////
        double accelScale(UInt16 sensorData)
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
        double gyroScale(UInt16 sensorData)
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
        double tempScale(UInt16 sensorData)
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
        double deltaAngleScale(UInt16 sensorData)
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
        double deltaVelocityScale(UInt16 sensorData)
        {
            double finalData = sensorData * 2.5; // Multiply by velocity scale (2.5 mm/sec/LSB)
            return finalData;
        }


        public double ADCRead(int channel)
        {

            byte[] readBuffer = new byte[3]; /* Buffer to hold read data*/
            byte[] writeBuffer = new byte[3] { MCP3208_CONFIG, (byte)(channel | 0x00), 0x00 };

            ADC.TransferFullDuplex(writeBuffer, readBuffer); /* Read data from the ADC                           */
            double adcValue = ConvertADCResult(readBuffer); /* Convert the returned bytes into an integer value */
            return adcValue;
        }

        public double ConvertADCResult([ReadOnlyArray()] byte[] rdata)
        {
            byte[] data = (byte[])rdata.Clone();
            double result = 0;
            //result = data[1] & 0x0F;
            //result <<= 8;
            //result += data[2];

            result = ((data[1] & 15) << 8) + data[2];

            result = result / (double)4095 * 3.3;

            return result;
        }
        private const byte MCP3208_CONFIG = 0x06; /* 00000110 single mode channel configuration data for the MCP3208 */

        internal enum Adis16460 : byte //Gyro
        {
            //// User Register Memory Map from Table 6
            FLASH_CNT = 0x00, //Flash memory write count
            DIAG_STAT = 0x02, //Diagnostic and operational status
            X_GYRO_LOW = 0x04, //X-axis gyroscope output, lower word
            X_GYRO_OUT = 0x06, //X-axis gyroscope output, upper word
            Y_GYRO_LOW = 0x08, //Y-axis gyroscope output, lower word
            Y_GYRO_OUT = 0x0A, //Y-axis gyroscope output, upper word
            Z_GYRO_LOW = 0x0C, //Z-axis gyroscope output, lower word
            Z_GYRO_OUT = 0x0E, //Z-axis gyroscope output, upper word
            X_ACCL_LOW = 0x10, //X-axis accelerometer output, lower word
            X_ACCL_OUT = 0x12, //X-axis accelerometer output, upper word
            Y_ACCL_LOW = 0x14, //Y-axis accelerometer output, lower word
            Y_ACCL_OUT = 0x16, //Y-axis accelerometer output, upper word
            Z_ACCL_LOW = 0x18, //Z-axis accelerometer output, lower word
            Z_ACCL_OUT = 0x1A, //Z-axis accelerometer output, upper word
            SMPL_CNTR = 0x1C, //Sample Counter, MSC_CTRL[3:2=11
            TEMP_OUT = 0x1E, //Temperature output (internal, not calibrated)
            X_DELT_ANG = 0x24, //X-axis delta angle output
            Y_DELT_ANG = 0x26, //Y-axis delta angle output
            Z_DELT_ANG = 0x28, //Z-axis delta angle output
            X_DELT_VEL = 0x2A, //X-axis delta velocity output
            Y_DELT_VEL = 0x2C, //Y-axis delta velocity output
            Z_DELT_VEL = 0x2E, //Z-axis delta velocity output
            MSC_CTRL = 0x32, //Miscellaneous control
            SYNC_SCAL = 0x34, //Sync input scale control
            DEC_RATE = 0x36, //Decimation rate control
            FLTR_CTRL = 0x38, //Filter control, auto-null record time
            GLOB_CMD = 0x3E, //Global commands
            XGYRO_OFF = 0x40, //X-axis gyroscope bias offset error
            YGYRO_OFF = 0x42, //Y-axis gyroscope bias offset error
            ZGYRO_OFF = 0x44, //Z-axis gyroscope bias offset factor
            XACCL_OFF = 0x46, //X-axis acceleration bias offset factor
            YACCL_OFF = 0x48, //Y-axis acceleration bias offset factor
            ZACCL_OFF = 0x4A, //Z-axis acceleration bias offset factor
            LOT_ID1 = 0x52, //Lot identification number
            LOT_ID2 = 0x54, //Lot identification number
            PROD_ID = 0x56, //Product identifier
            SERIAL_NUM = 0x58, //Lot-specific serial number
            CAL_SGNTR = 0x60, //Calibration memory signature value
            CAL_CRC = 0x62, //Calibration memory CRC values
            CODE_SGNTR = 0x64, //Code memory signature value
            CODE_CRC = 0x66 //Code memory CRC values}
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
