using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using FetcherShop.Helpers;
using FetcherShop.Logger;

namespace FetcherShop
{
    public partial class Zone
    {
        public string SiteUrl { get; set; }
        public int TotalPageNumber { get; set; }
        public string PageUrlPrefix { get; set; }

        private string GeneratePageMainUrl()
        {
            string pageMainUrl = SiteUrl;
            if (!pageMainUrl.EndsWith("/"))
            {
                pageMainUrl += "/";
            }

            pageMainUrl += ("html/" + Name);
            if (!pageMainUrl.EndsWith("/"))
            {
                pageMainUrl += "/";
            }
            pageMainUrl += "index.html";

            return pageMainUrl; 
        }

        /// <summary>
        /// Initialize the total number of pages and the url prefix of pages
        /// </summary>
        /// <returns></returns>
        public bool PreFetch()
        {
            string pageMainUrl = GeneratePageMainUrl();
            const string mainDivXPath = "/html/body/div[contains(@class, 'main')]";
            const string endPageXPath = ".//a[contains(text(), '末页')]";
            try
            {
                HtmlDocument doc = Util.GetHtmlDocument(pageMainUrl);
                HtmlNode mainNode = doc.DocumentNode.SelectSingleNode(mainDivXPath);
                if (mainNode == null)
                {
                    throw new InvalidOperationException("Main div element cound not be found");
                }

                HtmlNode node = mainNode.SelectNodes(endPageXPath).FirstOrDefault();
                if (node == null)
                {
                    throw new InvalidOperationException("Can't find the end page number");
                }
                string url = node.GetAttributeValue("href", null);
                if (url == null)
                {
                    throw new InvalidOperationException("The url of end page is null");
                }

                int number;
                int lastDotIndex = url.LastIndexOf('.');
                int lastDashIndex = url.LastIndexOf('_');
                if (lastDashIndex >= (lastDotIndex - 1) ||
                   !Int32.TryParse(url.Substring(lastDashIndex + 1, lastDotIndex - lastDashIndex - 1), out number))
                {
                    throw new InvalidOperationException(url + " is an invalid page url suffix");
                }

                TotalPageNumber = number;
                PageUrlPrefix = url.Substring(0, lastDashIndex + 1);
                GeneralLogger.Instance().Log(LogLevel.Information, 0, "Zone rough information: {0}, {1}", TotalPageNumber, PageUrlPrefix);
                return true;
            }
            catch (Exception e)
            {
                GeneralLogger.Instance().Log(LogLevel.Error, 0, "Exception happened when fetching zone rough information {0}: {1}", Name, e);
            }
            return false;
        }

        /// <summary>
        /// Get the url of one page which contains multiple items
        /// </summary>
        /// <param name="i"></param>
        /// <returns></returns>
        public string GetPageUrl(int i)
        {
            string pageUrl = SiteUrl;
            if (!pageUrl.EndsWith("/"))
            {
                pageUrl += "/";
            }

            pageUrl += ("html/" + Name);
            if (!pageUrl.EndsWith("/"))
            {
                pageUrl += "/";
            }
            return (pageUrl + PageUrlPrefix + i + ".html");
        }


        /// <summary>
        /// Get the all the urls of items in one page
        /// </summary>
        /// <param name="pageUrl"></param>
        /// <returns></returns>
        public IEnumerable<Anchor> GetItems(string pageUrl)
        {
            try
            {
                const string listXPath = "/html/body/div[contains(@class, 'main')]/div[@class='list']";
                GeneralLogger.Instance().Log(LogLevel.Information, 0, "Begin getting all the urls of the page {0}", pageUrl);
                HtmlDocument doc = Util.GetHtmlDocument(pageUrl);
                HtmlNode mainNode = doc.DocumentNode.SelectSingleNode(listXPath);
                if (mainNode != null)
                {
                    HtmlNodeCollection node = mainNode.SelectNodes(string.Format(".//a[contains(@href, '{0}')]", Name));
                    return node.Select(n =>
                        new Anchor()
                        {
                            AnchorText = n.InnerText,
                            AbsoluteUrl = Util.UrlCombine(SiteUrl, n.GetAttributeValue("href", null))
                        }).ToArray();
                }
            }
            catch (Exception e)
            {
                GeneralLogger.Instance().Log(LogLevel.Error, 0, "Exception occurred when getting items from the page {0}: {1}", pageUrl, e);
            }
            return null;
        }
    }
}
