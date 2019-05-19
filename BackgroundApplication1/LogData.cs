using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using Windows.Storage;
using Windows.Security.Cryptography;
using Windows.Foundation;

namespace BackgroundApplication1
{
    public sealed class LogData
    {
        private StorageFile _gyroFile;
        private StorageFile _adcFile;
        private StorageFile _logFile;
        private int _gyroLineCount;
        internal DateTime _dateTime;
        private StorageFolder storageFolder;
        private int _adcLineCount;
        private const int LINE_LIMIT = 1000000;

        public LogData(object dateTime)
        {
            _dateTime = (DateTime)dateTime;
            _gyroLineCount = 0;
            _adcLineCount = 0;

            SetupLogger();
        }

        private void SetupLogger()
        {
            Task dir = CreateDirectory();
            dir.Wait();
            Task Gyro = CreateGyroFile();
            Gyro.Wait();
            Task Adc = CreateAdcFile();
            Adc.Wait();
            Task Logs = CreateLogFile();
            Logs.Wait();
        }

        private async Task CreateDirectory()
        {
            var dir = ApplicationData.Current.LocalFolder;

            storageFolder = await dir.CreateFolderAsync("Data",
                CreationCollisionOption.OpenIfExists);

            storageFolder = await storageFolder.CreateFolderAsync($@"{_dateTime.ToString("yyyy-MM-dd-HH-mm-ss")}");
        }

        /// <summary>
        /// Create a file that increments automatically each time this function is called.
        /// </summary>
        /// <returns></returns>
        private async Task CreateGyroFile()
        {
            _gyroFile = await storageFolder.CreateFileAsync("Gyro.txt", CreationCollisionOption.GenerateUniqueName);
        }

        private async Task CreateAdcFile()
        {
            _adcFile = await storageFolder.CreateFileAsync("ADC.txt", CreationCollisionOption.GenerateUniqueName);
        }

        private async Task CreateLogFile()
        {
            _logFile = await storageFolder.CreateFileAsync("Log.txt", CreationCollisionOption.ReplaceExisting);
        }

        public IAsyncAction WriteGyroData(IEnumerable<string> data)
        {
            return WriteGyroDataHelper(data).AsAsyncAction();
        }

        private async Task WriteGyroDataHelper(IEnumerable<string> data)
        {
            await FileIO.AppendLinesAsync(_gyroFile, data);

            _gyroLineCount += data.Count();

            if (_gyroLineCount >= LINE_LIMIT)
            {
                await CreateGyroFile();
            }
        }


        public IAsyncAction WriteAdcData(IEnumerable<string> data)
        {
            return WriteAdcDataHelper(data).AsAsyncAction();
        }

        private async Task WriteAdcDataHelper(IEnumerable<string> data)
        {
            await FileIO.AppendLinesAsync(_adcFile, data);

            _adcLineCount += data.Count();

            if (_adcLineCount >= LINE_LIMIT)
            {
                await CreateAdcFile();
            }
        }


        public IAsyncAction Log(string message)
        {
            return LogHelper(message).AsAsyncAction();
        }

        private async Task LogHelper(string message)
        {
            await FileIO.AppendTextAsync(_logFile, message + Environment.NewLine);
        }


        /// <summary>
        /// For Testing API
        /// </summary>
        /// <returns></returns>
        public IAsyncAction ReadLog()
        {
            return ReadLogHelper().AsAsyncAction();
        }

        private async Task ReadLogHelper()
        {
            string text = await FileIO.ReadTextAsync(_logFile);

            System.Diagnostics.Debug.WriteLine(text);
        }
    }
}
