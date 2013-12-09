using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace FetcherShop
{
    [Serializable]
    public class FetcherConfig
    {
        public string RootUrl { get; set; }
        public string LogDirectory { get; set; }
        [XmlArray("Zones")]
        public Zone[] Zones { get; set;}        
    }
    [Serializable]
    public partial class Zone
    {
        [XmlAttribute]
        public string Name { get; set; }
        [XmlAttribute]
        public string RelativeUrl { get; set; }

        public RunConfiguration RunConfiguration { get; set; }
    }

    [Serializable]
    public class RunConfiguration
    {
        public string LatestAnchorUrl { get; set; }
        public string OutputDirectory { get; set; }
        public string LogDirectory { get; set; }
    }
}
