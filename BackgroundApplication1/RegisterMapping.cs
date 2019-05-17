//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace BackgroundApplication1
//{
//    public static class RegisterMapping
//    {
//        public enum Adis16460
//        {
//            //// User Register Memory Map from Table 6
//            FLASH_CNT = 0x00, //Flash memory write count
//            DIAG_STAT = 0x02, //Diagnostic and operational status
//            X_GYRO_LOW = 0x04, //X-axis gyroscope output, lower word
//            X_GYRO_OUT = 0x06, //X-axis gyroscope output, upper word
//            Y_GYRO_LOW = 0x08, //Y-axis gyroscope output, lower word
//            Y_GYRO_OUT = 0x0A, //Y-axis gyroscope output, upper word
//            Z_GYRO_LOW = 0x0C, //Z-axis gyroscope output, lower word
//            Z_GYRO_OUT = 0x0E, //Z-axis gyroscope output, upper word
//            X_ACCL_LOW = 0x10, //X-axis accelerometer output, lower word
//            X_ACCL_OUT = 0x12, //X-axis accelerometer output, upper word
//            Y_ACCL_LOW = 0x14, //Y-axis accelerometer output, lower word
//            Y_ACCL_OUT = 0x16, //Y-axis accelerometer output, upper word
//            Z_ACCL_LOW = 0x18, //Z-axis accelerometer output, lower word
//            Z_ACCL_OUT = 0x1A, //Z-axis accelerometer output, upper word
//            SMPL_CNTR = 0x1C, //Sample Counter, MSC_CTRL[3:2=11
//            TEMP_OUT = 0x1E, //Temperature output (internal, not calibrated)
//            X_DELT_ANG = 0x24, //X-axis delta angle output
//            Y_DELT_ANG = 0x26, //Y-axis delta angle output
//            Z_DELT_ANG = 0x28, //Z-axis delta angle output
//            X_DELT_VEL = 0x2A, //X-axis delta velocity output
//            Y_DELT_VEL = 0x2C, //Y-axis delta velocity output
//            Z_DELT_VEL = 0x2E, //Z-axis delta velocity output
//            MSC_CTRL = 0x32, //Miscellaneous control
//            SYNC_SCAL = 0x34, //Sync input scale control
//            DEC_RATE = 0x36, //Decimation rate control
//            FLTR_CTRL = 0x38, //Filter control, auto-null record time
//            GLOB_CMD = 0x3E, //Global commands
//            XGYRO_OFF = 0x40, //X-axis gyroscope bias offset error
//            YGYRO_OFF = 0x42, //Y-axis gyroscope bias offset error
//            ZGYRO_OFF = 0x44, //Z-axis gyroscope bias offset factor
//            XACCL_OFF = 0x46, //X-axis acceleration bias offset factor
//            YACCL_OFF = 0x48, //Y-axis acceleration bias offset factor
//            ZACCL_OFF = 0x4A, //Z-axis acceleration bias offset factor
//            LOT_ID1 = 0x52, //Lot identification number
//            LOT_ID2 = 0x54, //Lot identification number
//            PROD_ID = 0x56, //Product identifier
//            SERIAL_NUM = 0x58, //Lot-specific serial number
//            CAL_SGNTR = 0x60, //Calibration memory signature value
//            CAL_CRC = 0x62, //Calibration memory CRC values
//            CODE_SGNTR = 0x64, //Code memory signature value
//            CODE_CRC = 0x66 //Code memory CRC values}
//        }

//        public enum ADC
//        {
//            SINGLE_0 = 0x08, //0b1000, /* single channel 0 */
//            SINGLE_1 = 0x09, //0b1001, /* single channel 1 */
//            SINGLE_2 = 0x0A, //0b1010, /* single channel 2 */
//            SINGLE_3 = 0x0B, //0b1011, /* single channel 3 */
//            SINGLE_4 = 0x0C, //0b1100, /* single channel 4 */
//            SINGLE_5 = 0x0D, //0b1101, /* single channel 5 */
//            SINGLE_6 = 0x0E, //0b1110, /* single channel 6 */
//            SINGLE_7 = 0x0F, //0b1111, /* single channel 7 */
//            DIFF_0PN = 0x00, //0b0000, /* differential channel 0 (input 0+,1-) */
//            DIFF_0NP = 0x01, //0b0001, /* differential channel 0 (input 0-,1+) */
//            DIFF_1PN = 0x02, //0b0010, /* differential channel 1 (input 2+,3-) */
//            DIFF_1NP = 0x03, //0b0011, /* differential channel 1 (input 2-,3+) */
//            DIFF_2PN = 0x04, //0b0100, /* differential channel 2 (input 4+,5-) */
//            DIFF_2NP = 0x05, //0b0101, /* differential channel 2 (input 5-,5+) */
//            DIFF_3PN = 0x06,//0b0110, /* differential channel 3 (input 6+,7-) */
//            DIFF_3NP = 0x07 //0b0111  /* differential channel 3 (input 6-,7+) */
//        }
//    }
//}
