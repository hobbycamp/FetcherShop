using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FetcherShop.Logger;
using System.Net;
using HtmlAgilityPack;
using System.IO;
using FetcherShop.Helpers;
using System.Threading;
using System.Collections.Concurrent;

namespace FetcherShop.Fetchers
{
    public abstract class AbstractFetcher
    {
        private int numberOfItems = 0;
        private ManualResetEvent finished = new ManualResetEvent(false);

        public void Fetch(WebSiteConfig siteConfig)
        {
            string logFile = Path.Combine(siteConfig.LogDirectory, Category.Keyword + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt");
            BlockingCollection<LogItem> queue = new BlockingCollection<LogItem>();
            LogListeners.Add(new FileLogListener(queue));

            LogThread logThread = new LogThread(queue, logFile);
            logThread.StartLog();

            //Anchor firstItem = GetFirstRecord(siteConfig.Url, Category);
            //Category.LastestRecordUrl = Util.UrlCombine(siteConfig.Url, firstItem.Url);
            //Log("First item {0} with url {1} for category {2}", firstItem.AnchorText, firstItem.Url, Category.Keyword);
            //StartFetch(firstItem, siteConfig.Url, siteConfig.OutputDirectory, Category);
            CategoryOutline outline = GetCategoryOutline(siteConfig.Url, Category);
            if (outline == null)
            {
                Log(0, "[Crash] Failed to get the outline information of category {0}", Category.Keyword);
                return;
            }

            int startAnchorId = 1;
            bool hitLastRecord = false;
            for (int i = 1; i <= outline.TotalPageNumber && !hitLastRecord; i++)
            {
                string url = GetPageUrl(siteConfig.Url, outline, Category, i);
                // Fetch all the items in this page.
                IEnumerable<Anchor> allItems = GetItems(url, siteConfig, Category);
                if (allItems != null)
                {
                    hitLastRecord = IsHitLastRecord(allItems, Category);
                    Log(0, "Number of items is {0} for {1}", allItems.Count(), url);
                    Interlocked.Add(ref numberOfItems, allItems.Count());
                    foreach (Anchor anchor in allItems)
                    {
                        if (anchor.Url != null)
                        {
                            anchor.Id = startAnchorId++;
                            ThreadPool.QueueUserWorkItem(new WaitCallback(MyWaitCallback), new ObjectState()
                            {
                                anchor = anchor,
                                outputDirectory = siteConfig.OutputDirectory,
                                cat = Category,
                            });                            
                            //FetchCategory1(anchor, siteConfig.OutputDirectory, Category);
                        }
                    }
                }
            }

            finished.WaitOne();
            Log(0, "Finished category {0}", Category.Keyword);
            queue.CompleteAdding();            
        }

        public void MyWaitCallback(object state)
        {
            ObjectState os = (ObjectState)state;
            try
            {
                FetchAnchor(os.anchor, os.outputDirectory, os.cat);
            }
            finally 
            {
                if (Interlocked.Decrement(ref numberOfItems) == 0)
                {
                    Log(os.anchor.Id, "Finished all tasks");
                    finished.Set();
                }
            }
        }

        /// <summary>
        /// Return true if anchor contains the lastest record url
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="cat"></param>
        /// <returns></returns>
        bool IsHitLastRecord(IEnumerable<Anchor> anchor, Category cat)
        {
            if (anchor == null)
                return false;
            foreach (Anchor a in anchor)
            {
                if (a.Url == cat.LastestRecordUrl)
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// Get the all the urls of items in one page
        /// </summary>
        /// <param name="pageUrl"></param>
        /// <param name="config"></param>
        /// <param name="cat"></param>
        /// <returns></returns>
        protected IEnumerable<Anchor> GetItems(string pageUrl, WebSiteConfig config, Category cat)
        {
            try
            {
                Log(0, "Begin getting all the urls of the page {0}", pageUrl);
                HtmlDocument doc = null;

                doc = Util.GetHtmlDocument(pageUrl, LogListeners);
                if (doc == null)
                {
                    return null;
                }

                HtmlNode mainNode = doc.DocumentNode.SelectSingleNode("/html/body/div[contains(@class, 'main')]/div[@class='list']");
                if (mainNode != null)
                {
                    HtmlNodeCollection node = mainNode.SelectNodes(string.Format(".//a[contains(@href, '{0}')]", cat.Keyword));
                    var result = node.Select(n => 
                        new Anchor()
                        {
                            AnchorText = n.InnerText,
                            Url = Util.UrlCombine(config.Url, n.GetAttributeValue("href", null))
                        }
                         );
                    return result;
                }
                return null;
            }
            catch (Exception e)
            {
                Log(0, "Exception occurred when getting items from the page {0}: {1}", pageUrl, e);
                return null;
            } 
        }

        /// <summary>
        /// Get the url of one page which contains multiple items
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <param name="outline"></param>
        /// <param name="cat"></param>
        /// <param name="i"></param>
        /// <returns></returns>
        protected string GetPageUrl(string siteUrl, CategoryOutline outline, Category cat, int i)
        {
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }

            siteUrl += ("html/" + cat.Keyword);
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }
            return (siteUrl + outline.ListUrlPrefix + i + ".html");
        }

        protected virtual CategoryOutline GetCategoryOutline(string siteUrl, Category cat)
        {
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }

            siteUrl += ("html/" + cat.Keyword);
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }
            siteUrl += "index.html";
            WebRequest req = WebRequest.Create(siteUrl);
            WebResponse resp = req.GetResponse();

            Stream resStream = resp.GetResponseStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(resStream, Encoding.GetEncoding("GB2312"));
            resp.Close();
            HtmlNode mainNode = doc.DocumentNode.SelectSingleNode("/html/body/div[contains(@class, 'main')]");
            if (mainNode == null)
            {
                Log(0, "Main div element cound not be found");
                return null;
            }

            CategoryOutline outline = new CategoryOutline();
            HtmlNode node = mainNode.SelectNodes(string.Format(".//a[contains(text(), '末页')]", cat.Keyword)).FirstOrDefault();
            if (node == null)
                return null;
            string url = node.GetAttributeValue("href", null);
            if (url == null)
                return null;

            int lastDotIndex = url.LastIndexOf('.');
            int lastDashIndex = url.LastIndexOf('_');
            if (lastDashIndex >= (lastDotIndex - 1))
                return null;
            int number;
            if(Int32.TryParse(url.Substring(lastDashIndex + 1, lastDotIndex - lastDashIndex - 1), out number))
            {
                outline.TotalPageNumber = number;
                outline.ListUrlPrefix = url.Substring(0, lastDashIndex + 1);
                return outline;
            }
            return null;
        }

        protected Anchor GetPreviousItemAnchor(HtmlNode node)
        {
            const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='pagea']";
            HtmlNode pagea = node.SelectSingleNode(xpath);
            if (pagea == null)
            {
                Log(0, "No pagea div found");
                return null;
            }

            string innerXML = pagea.InnerText;
            if (innerXML.Contains("上一篇：没有了"))
            {
                Log(0, "Last item hitted");
                return null;
            }

            HtmlNode nd = pagea.SelectSingleNode("./a[1]");
            return new Anchor()
            {
                Url = nd.GetAttributeValue("href", null),
                AnchorText = nd.InnerText
            };
        }

        //protected void StartFetch(Anchor anchor, string webSiteUrl, string outputDirectory, Category cat)
        //{
        //    while ((string.IsNullOrEmpty(cat.LastestRecordUrl)) || (anchor.Url != cat.LastestRecordUrl))
        //    {
        //        try
        //        {
        //            string latterUrl = anchor.Url;
        //            Util.DoWithRetry("FetchCategory",
        //                () =>
        //                {
        //                    anchor.Url = Util.UrlCombine(webSiteUrl, latterUrl);
        //                    anchor = FetchCategory(anchor, outputDirectory, cat);
        //                }, LogListeners);
        //            if (anchor == null)
        //                break;
        //        }
        //        catch (Exception e)
        //        {
        //            Log(0, "Failed to fetch content for {0} with url {1}. Exception: {2}", anchor.AnchorText, anchor.Url, e);
        //            break;
        //        }                
        //    }

        //    Log(0, "Fetch category {0} completed", cat.Keyword);
        //}
        
        public List<LogListener> LogListeners { get; set; }
        public Category Category { get; set; }

        public AbstractFetcher(Category  category) {
            LogListeners = new List<LogListener>();
            LogListeners.Add(new ConsoleLogListener());

            Category = category;
        }

        public void Log(int id, string format, params object[] args)
        {
            foreach (LogListener listener in LogListeners)
            {
                listener.Log(id, format, args);
            }
        }

        Anchor GetFirstRecord(string siteUrl, Category cat)
        {
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }

            siteUrl += ("html/" + cat.Keyword);
            if (!siteUrl.EndsWith("/"))
            {
                siteUrl += "/";
            }
            siteUrl += "index.html";
            WebRequest req = WebRequest.Create(siteUrl);
            WebResponse resp = req.GetResponse();

            Stream resStream = resp.GetResponseStream();
            HtmlDocument doc = new HtmlDocument();
            doc.Load(resStream, Encoding.GetEncoding("GB2312"));

            HtmlNode mainNode = doc.DocumentNode.SelectSingleNode("/html/body/div[contains(@class, 'main')]");
            if (mainNode == null)
            {
                Log(0, "Main div element cound not be found");
                return null;
            }

            HtmlNode node = mainNode.SelectNodes(string.Format(".//a[contains(@href, '{0}')]", cat.Keyword)).FirstOrDefault();
            if (node == null)
                return null;
            resp.Close();
            return new Anchor()
            {
                AnchorText = node.InnerText,
                Url = node.GetAttributeValue("href", "")
            };
        }
        
        /// <summary>
        /// The template method of fetching the content of one page
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="doc"></param>
        /// <param name="destiDir"></param>
        protected void FetchPageContentTemplate(Anchor anchor, HtmlDocument doc, string destiDir)
        {
            const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='content']";
            HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);
            if (node == null)
                return;

            if (node.InnerText.Length == 0)
            {
                Log(anchor.Id, "Warning: Content empty, manually interferce needed");
                return;
            }
            FetchPageContent(anchor, node, destiDir);
        }

        protected virtual void FetchPageContent(Anchor anchor, HtmlNode node, string destiDir)
        {
            string filePath = GetFilePath(destiDir, anchor.AnchorText);
            Log(anchor.Id, "Write text file {0} with url {1}", filePath, anchor.Url);
            FetchText(filePath, node, anchor);
        }

        protected Anchor FetchCategory(Anchor anchor, string outputDirectory, Category cat)
        {
            WebRequest req = WebRequest.Create(anchor.Url);
            WebResponse resp = req.GetResponse();
            Stream stream = resp.GetResponseStream();

            HtmlDocument doc = new HtmlDocument();
            doc.Load(stream, Encoding.GetEncoding("GB2312"));
            resp.Close();
            bool filterMatch = false;
            foreach (Filter filter in cat.Filters)
            {
                // Assume output directory exists already
                string destiDir = Path.Combine(outputDirectory, filter.Accept);
                if (!Directory.Exists(destiDir))
                {
                    Log(anchor.Id, "Create directory {0} for category {1} filter {2}", destiDir, cat.Keyword, filter.Accept);
                    Directory.CreateDirectory(destiDir);
                }
                string[] keywords = filter.Accept.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                bool flag = false;
                foreach (string keyword in keywords)
                {
                    if (anchor.AnchorText.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) != -1)
                    {
                        flag = true;
                        filterMatch = true;
                        break;
                    }
                }

                if (flag)
                {
                    FetchPageContentTemplate(anchor, doc, destiDir); 
                }
            }
            if (!filterMatch)
            {
                string destiDir = Path.Combine(outputDirectory, "others");
                if (!Directory.Exists(destiDir))
                {
                    Log(anchor.Id, "Create directory {0} for category {1} filter {2}", destiDir, cat.Keyword, "others");
                    Directory.CreateDirectory(destiDir);
                }
                FetchPageContentTemplate(anchor, doc, destiDir);
            }
            return GetPreviousItemAnchor(doc.DocumentNode);
        }

        /// <summary>
        /// Start to fetch an anchor
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="outputDirectory"></param>
        /// <param name="cat"></param>
        protected void FetchAnchor(Anchor anchor, string outputDirectory, Category cat)
        {
            try
            {
                HtmlDocument doc = Util.GetHtmlDocument(anchor, LogListeners);
                bool filterMatch = false;
                //foreach (Filter filter in cat.Filters)
                //{
                //    // Assume output directory exists already
                //    string destiDir = Path.Combine(outputDirectory, filter.Accept);
                //    if (!Directory.Exists(destiDir))
                //    {
                //        Log("Create directory {0} for category {1} filter {2}", destiDir, cat.Keyword, filter.Accept);
                //        Directory.CreateDirectory(destiDir);
                //    }
                //    string[] keywords = filter.Accept.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                //    bool flag = false;
                //    foreach (string keyword in keywords)
                //    {
                //        if (anchor.AnchorText.IndexOf(keyword, StringComparison.CurrentCultureIgnoreCase) != -1)
                //        {
                //            flag = true;
                //            filterMatch = true;
                //            break;
                //        }
                //    }

                //    if (flag)
                //    {
                //        FetchPageContentTemplate(anchor, doc, destiDir);
                //    }
                //}
                if (!filterMatch)
                {
                    string destiDir = Path.Combine(outputDirectory, "others");
                    if (!Directory.Exists(destiDir))
                    {
                        Log(anchor.Id, "Create directory {0} for category {1} filter {2}", destiDir, cat.Keyword, "others");
                        Directory.CreateDirectory(destiDir);
                    }
                    FetchPageContentTemplate(anchor, doc, destiDir);
                }
            }
            catch (Exception e)
            {
                Log(anchor.Id, "Exception occurred when fetching {0}: {1}", anchor.Url, e);
            }
        }

        void FetchText(string filePath, HtmlNode node, Anchor anchor)
        {
            using (StreamWriter writer = new StreamWriter(filePath))
            {
                foreach (HtmlNode child in node.ChildNodes)
                {
                    if (child.NodeType == HtmlNodeType.Element && string.Compare("br", child.Name, true) == 0)
                    {
                        writer.WriteLine();
                    }
                    else if (child.NodeType == HtmlNodeType.Text)
                    {
                        writer.Write(child.InnerText.Trim(new char[] { ' ', '\r', '\n' }));
                    }
                    else if (child.NodeType == HtmlNodeType.Element && string.Compare("p", child.Name, true) == 0)
                    {
                        FetchTextFromParagraphElement(writer, child);
                    }
                }

                writer.WriteLine(anchor.Url);
            }

            FileInfo info = new FileInfo(filePath);
            if (info.Length == 0)
            {
                Log(anchor.Id, "Warning: {0} empty for {1}", anchor.AnchorText, anchor.Url);
            }
        }

        void FetchTextFromParagraphElement(StreamWriter writer, HtmlNode node)
        {
            foreach (HtmlNode child in node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Element && string.Compare("br", child.Name, true) == 0)
                {
                    writer.WriteLine();
                }
                else if (child.NodeType == HtmlNodeType.Text)
                {
                    writer.Write(child.InnerText.Trim(new char[] { ' ', '\r', '\n' }));
                }
            }
        }

        protected virtual string GetFilePath(string dir, string anchorText)
        {
            // Replace the invalid file name chars with whitespace
            anchorText = Util.FilterEntryName(anchorText);
            return Util.GetNewFilePath(dir, anchorText, ".txt");
        }
    }

}
