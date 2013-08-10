using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FetcherShop
{
    [Serializable]
    public class WebSiteConfig
    {
        public string Url { get; set; }
        [XmlArray("Categories")]
        public Category[] Categories { get; set;}
        
        public string OutputDirectory { get; set; }
        public string LogDirectory { get; set; }
    }
    [Serializable]
    public class Category
    {
        public string Keyword { get; set; }
        public string LastestRecordUrl { get; set; }
        [XmlArray("Filters")]
        public Filter[] Filters { get; set; }
    }

    [Serializable]
    public class Filter
    {
        public string Accept { get; set; }
        public string Deny { get; set; }
    }
}
