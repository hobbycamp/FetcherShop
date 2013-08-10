using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FetcherShop.Fetchers
{
    public class FetcherFactory
    {
        public static AbstractFetcher CreateFetcher(Category category)
        {
            if (string.Compare(category.Keyword, "jiatingluanlun", true) == 0)
            {
                return new JTLLFetcher(category);
            }
            else if (string.Compare(category.Keyword, "btyazhouqu", true) == 0)
            {
                return new BTYZFetcher(category);
            }
            else if (string.Compare(category.Keyword, "btdongmanqu", true) == 0)
            {
                return new BTYZFetcher(category);
            }
            else if (string.Compare(category.Keyword, "btoumeiqu", true) == 0)
            {
                return new BTYZFetcher(category);
            }

            return null;
        }
    }
}
