using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FetcherShop.Logger
{
    public abstract class LogListener
    {
        // Id is the identifier of this log item
        public abstract void Log(LogLevel logLevel, int id, string format, params object[] args);

        public abstract void Log(string format, params object[] args);
    }
}
