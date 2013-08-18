using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FetcherShop.Logger
{
    public class LogItem
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public LogLevel LogLevel { get; set; }
    }
}
