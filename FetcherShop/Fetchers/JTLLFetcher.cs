using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using HtmlAgilityPack;
using System.Net;
using FetcherShop.Logger;
using FetcherShop.Helpers;

namespace FetcherShop.Fetchers
{
    public class JTLLFetcher : AbstractFetcher
    {
        public JTLLFetcher(Category category) : base(category)
        {
        }
    }
}
