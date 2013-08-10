using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FetcherShop
{
    public class ObjectState
    {
        public Anchor anchor { get; set; }
        public string outputDirectory { get; set; }
        public Category cat { get; set; }
        public ManualResetEvent manualResetEvent{ get; set; }
    }
}
