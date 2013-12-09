using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace FetcherShop
{
    public class ObjectState
    {
        public Anchor Anchor { get; set; }
        public Zone Zone { get; set; }
        public ManualResetEvent ManualResetEvent{ get; set; }
    }
}
