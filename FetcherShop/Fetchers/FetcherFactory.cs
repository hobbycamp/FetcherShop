using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FetcherShop.Fetchers
{
    public class FetcherFactory
    {
        public static AbstractFetcher CreateFetcher(Zone zone)
        {
            if (string.Compare(zone.Name, "jiatingluanlun", true) == 0)
            {
                return new JTLLFetcher(zone);
            }
            else if (string.Compare(zone.Name, "btyazhouqu", true) == 0)
            {
                return new BTYZFetcher(zone);
            }
            else if (string.Compare(zone.Name, "btdongmanqu", true) == 0)
            {
                return new BTYZFetcher(zone);
            }
            else if (string.Compare(zone.Name, "btoumeiqu", true) == 0)
            {
                return new BTYZFetcher(zone);
            }

            return null;
        }
    }
}
