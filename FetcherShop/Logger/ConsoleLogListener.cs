using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FetcherShop.Logger
{
    public class ConsoleLogListener : LogListener
    {
        private static ConsoleLogListener _consoleLogListener = new ConsoleLogListener();
        private ConsoleLogListener() { }
        public static ConsoleLogListener Instance() { return _consoleLogListener; }
        public override void Log(LogLevel logLevel, int id, string format, params object[] args)
        {
            string logItem = string.Format(format, args);
            Console.WriteLine("[{0}] {1}", id, logItem);
        }

        public override void Log(string format, params object[] args)
        {
            Console.WriteLine(format, args);
        }
    }
}
