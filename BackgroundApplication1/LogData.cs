using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices.WindowsRuntime;
using System.IO;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.Foundation;
using Windows.Security.Cryptography;

namespace BackgroundApplication1
{
    internal sealed class LogData
    {
        private StorageFile _gyroFile;
        private StorageFile _adcFile;
        private StorageFile _dataFile;
        private StorageFile _logFile;
        private int _gyroLineCount;
        private int _dataLineCount;
        internal DateTime _dateTime;
        private StorageFolder storageFolder;
        private int _adcLineCount;
        private const int LINE_LIMIT = 1000000;

        private List<double[]> _data = new List<double[]>();
        public double[][] DataBucket { get; set; }
        //{
        //    get { return new double[0]; } // do not use this
        //    set
        //    {
        //        _data.Add(value);
        //    }
        //}



        public LogData(object dateTime)
        {
            _dateTime = (DateTime)dateTime;
            _gyroLineCount = 0;
            _adcLineCount = 0;

            SetupLogger();
        }

        private void SetupLogger()
        {
            Task t = CreateDirectory();
            t.Wait();
            t = CreateDataFile();
            t.Wait();
            t = CreateGyroFile();
            t.Wait();
            t = CreateAdcFile();
            t.Wait();
            t = CreateLogFile();
            t.Wait();
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

        private async Task CreateDataFile()
        {
            _dataFile = await storageFolder.CreateFileAsync("Launch.txt", CreationCollisionOption.GenerateUniqueName);
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

        public async void StoreData()
        {
            var stream = await _dataFile.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowReadersAndWriters);

            using (var outputStream = stream.GetOutputStreamAt(0))
            {
                using (var dataWriter = new Windows.Storage.Streams.DataWriter(outputStream))
                {
                    const string delinieater = ";";
                    foreach (double[] data in _data)
                    {
                        foreach (double d in data)
                        {
                            dataWriter.WriteDouble(d);
                            dataWriter.WriteString(delinieater);
                        }
                    }
                    await dataWriter.FlushAsync();
                }
            }
            stream.Dispose();

            _dataLineCount += _data.Count();

            if (_dataLineCount >= LINE_LIMIT)
            {
                await CreateDataFile();
            }
            _data = new List<double[]>();
        }
    }
}
