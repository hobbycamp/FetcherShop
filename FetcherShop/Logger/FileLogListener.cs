using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Concurrent;

namespace FetcherShop.Logger
{
    public class FileLogListener : LogListener
    {
        public string FileName { get; private set; }
        public FileLogListener(string fileName)
        {
            FileName = fileName;
        }

        public BlockingCollection<LogItem> LogQueue { get; private set; }

        public FileLogListener(BlockingCollection<LogItem> queue)
        {
            LogQueue = queue;
        }
        public override void Log(int id, string format, params object[] args)
        {
            string content = string.Format(format, args) + Environment.NewLine;
            content = string.Format("[{0}]: {1}", id, content);
            LogQueue.TryAdd(new LogItem() { 
                Id = id,
                Content = content
            });            
        }

        public override void Log(string format, params object[] args)
        {
            using (StreamWriter writer = File.AppendText(FileName))
            {
                writer.WriteLine(format, args);
            }
        }
    }
}
