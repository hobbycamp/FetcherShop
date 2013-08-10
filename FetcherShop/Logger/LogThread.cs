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
        private Dictionary<int, long> Seeks { get; set; }

        public LogThread(BlockingCollection<LogItem> logsQueue, string logFileName)
        {
            LogsQueue = logsQueue;
            LogFileName = logFileName;
            Seeks = new Dictionary<int, long>();
        }
        
        public void StartLog()
        {
            Thread myThread = new Thread(() => {
                using (FileStream stream = new FileStream(LogFileName, FileMode.OpenOrCreate))
                {
                    while (true)
                    {
                        try
                        {
                            LogItem logItem = LogsQueue.Take();

                            IEnumerable<byte> bytes = Encoding.GetEncoding("GB2312").GetBytes(logItem.Content);
                            int length = Encoding.GetEncoding("GB2312").GetByteCount(logItem.Content);

                            // First seek file pointer
                            long currentSeekPosition = 0;
                            if (!Seeks.TryGetValue(logItem.Id, out currentSeekPosition))
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
                            Seeks[logItem.Id] = currentSeekPosition + length;

                            // Update others' seek
                            UpdateSeeks(currentSeekPosition, length, logItem.Id);

                            stream.Flush();
                        }
                        catch (InvalidOperationException)
                        {
                            break;
                        }
                    }
                }
            });
            myThread.Start();
        }

        public void FinishLog()
        {
            LogsQueue.CompleteAdding();
        }

        private void UpdateSeeks(long currentSeekPosition, int length, int excludedId)
        {
            foreach (var key in Seeks.Keys.ToList())
            {
                if (Seeks[key] > currentSeekPosition && key != excludedId)
                {
                    Seeks[key] += length;
                }
            }
        }
    }
}
