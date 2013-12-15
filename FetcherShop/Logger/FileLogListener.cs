using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FetcherShop.Logger
{
    public class FileLogListener : LogListener
    {
        public string FileName { get; private set; }
        public FileLogListener(string fileName)
        {
            FileName = fileName;
        }

        public override void Log(string format, params object[] args)
        {
            using (StreamWriter writer = File.AppendText(FileName))
            {
                writer.WriteLine(format, args);
            }
        }

        public override void Log(LogLevel logLevel, int id, string format, params object[] args)
        {
            // Omit this kind of logs
        }
    }
}
