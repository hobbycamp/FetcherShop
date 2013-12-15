using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;

namespace FetcherShop.Logger
{
    public class LogThread
    {
        private BlockingCollection<LogItem> LogsQueue { get; set; }
        private string LogFileName { get; set; }
        private Dictionary<FileStream, Dictionary<int, long>> Seeks { get; set; }
        private Dictionary<LogLevel, FileStream> FileStreams = null;

        public LogThread(BlockingCollection<LogItem> logsQueue, string logFileName)
        {
            LogsQueue = logsQueue;
            LogFileName = logFileName;
            Seeks = new Dictionary<FileStream, Dictionary<int, long>>();
            FileStreams = new Dictionary<LogLevel, FileStream>();
        }

        public void MyThreadStart()
        {
            while (true)
            {
                try
                {
                    LogItem logItem = LogsQueue.Take();

                    IEnumerable<byte> bytes = Encoding.GetEncoding("GB2312").GetBytes(logItem.Content);
                    int length = Encoding.GetEncoding("GB2312").GetByteCount(logItem.Content);
                    FileStream stream = GetFileStream(logItem.LogLevel);
                    // First seek file pointer
                    long currentSeekPosition = 0;
                    if (!Seeks.ContainsKey(stream))
                    {
                        Seeks[stream] = new Dictionary<int, long>();
                    }

                    var seeks = Seeks[stream];

                    if (!seeks.TryGetValue(logItem.Id, out currentSeekPosition))
                    {
                        // logItem.id is the first time to be recorded
                        // so the seek position is the end of this stream
                        currentSeekPosition = stream.Length;
                    }
                    // seek file pointer to the correct position
                    stream.Seek(currentSeekPosition, SeekOrigin.Begin);
                    if (stream.Length > currentSeekPosition)
                    {
                        // Insert the current item in the middle of the file
                        byte[] remainBytes = new byte[stream.Length - currentSeekPosition];
                        stream.Read(remainBytes, 0, remainBytes.Length);
                        bytes = bytes.Concat(remainBytes);
                        // Because Read changes the file pointer, so seek it back
                        stream.Seek(currentSeekPosition, SeekOrigin.Begin);
                    }
                    stream.Write(bytes.ToArray(), 0, bytes.Count());
                    seeks[logItem.Id] = currentSeekPosition + length;

                    // Update others' seek
                    UpdateSeeks(seeks, currentSeekPosition, length, logItem.Id);

                    stream.Flush();
                }
                catch (InvalidOperationException)
                {
                    break;
                }
            } // end while

            // Close file streams
            Cleanup();
        }

        public void Cleanup()
        {
            foreach (var f in FileStreams)
            {
                f.Value.Flush();
                f.Value.Close();
            }
        }

        private FileStream GetFileStream(LogLevel logLevel)
        {
            if (!FileStreams.ContainsKey(logLevel))
            {
                string direct = Path.GetDirectoryName(LogFileName);
                string filenameWithoutExtension = Path.GetFileNameWithoutExtension(LogFileName);
                string extension = Path.GetExtension(LogFileName);
                FileStream stream = new FileStream(direct + "\\" + filenameWithoutExtension + " - " + logLevel.ToString() + extension,
                    FileMode.OpenOrCreate);
                FileStreams[logLevel] = stream;
            }

            return FileStreams[logLevel];
        }

        public void StartLog()
        {
            Thread myThread = new Thread(MyThreadStart);
            myThread.Start();
        }

        public void FinishLog()
        {
            LogsQueue.CompleteAdding();
        }

        private void UpdateSeeks(Dictionary<int, long> seeks, long currentSeekPosition, int length, int excludedId)
        {
            foreach (var key in seeks.Keys.ToList())
            {
                if (seeks[key] > currentSeekPosition && key != excludedId)
                {
                    seeks[key] += length;
                }
            }
        }
    }
}
