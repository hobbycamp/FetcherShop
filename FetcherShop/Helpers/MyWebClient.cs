using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace FetcherShop.Helpers
{
    public class MyWebClient : WebClient
    {
        public int Timeout { get; set; }
        // Default timeout is 180(s)
        public MyWebClient() : this(180000) { }

        public MyWebClient(int timeout)
        {
            this.Timeout = timeout;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request != null)
            {
                request.Timeout = this.Timeout;
            }
            return request;
        }
    }
}
