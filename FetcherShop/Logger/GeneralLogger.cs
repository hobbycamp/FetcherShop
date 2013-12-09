using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FetcherShop.Logger
{
    public class GeneralLogger
    {
        private static GeneralLogger _instance = new GeneralLogger();

        private List<LogListener> _listeners = new List<LogListener>();

        private GeneralLogger() { }

        public static GeneralLogger Instance() {
            return _instance;
        }

        public void AddLogListner(LogListener logListener)
        {
            if (!_listeners.Contains(logListener))
                _listeners.Add(logListener);
        }

        public void Log(LogLevel logLevel, int id, string format, params object[] args)
        {
            foreach (LogListener listener in _listeners)
            {
                listener.Log(logLevel, id, format, args);
            }
        }

        public void Log(string format, params object[] args)
        {
            foreach (LogListener listener in _listeners)
            {
                listener.Log(format, args);
            }
        }
    }
}
