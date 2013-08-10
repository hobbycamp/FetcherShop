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
        public List<LogListener> LogListeners { get; set; }
        public Category Category { get; set; }
        private LogThread logThread = null;

        public AbstractFetcher(Category  category)
        {
            LogListeners = new List<LogListener>();
            LogListeners.Add(new ConsoleLogListener());

            Category = category;
        }

        protected void InitializeFileLogListener(WebSiteConfig siteConfig)
        {
            string logFile = Path.Combine(siteConfig.LogDirectory, Category.Keyword + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt");
            // Initialize a central log items queue
            BlockingCollection<LogItem> queue = new BlockingCollection<LogItem>();
            LogListeners.Add(new FileLogListener(queue));

            logThread = new LogThread(queue, logFile);
            logThread.StartLog();
        }
        
        public void Fetch(WebSiteConfig siteConfig)
        {
            InitializeFileLogListener(siteConfig);
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
                            if (startAnchorId == 1)
                            {
                                Category.LastestRecordUrl = anchor.Url;
                            }
                            anchor.Id = startAnchorId++;
                            
                            ThreadPool.QueueUserWorkItem(new WaitCallback(MyWaitCallback), new ObjectState()
                            {
                                anchor = anchor,
                                outputDirectory = siteConfig.OutputDirectory,
                                cat = Category,
                            });                            
                        }
                    }
                }
            }
            finished.WaitOne();
            Log(0, "Finished category {0}", Category.Keyword);

            logThread.FinishLog();
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
                    Log(0, "Finished all tasks for {0}", os.cat.Keyword);
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
                const string listXPath = "/html/body/div[contains(@class, 'main')]/div[@class='list']";
                Log(0, "Begin getting all the urls of the page {0}", pageUrl);
                HtmlDocument doc = Util.GetHtmlDocument(pageUrl, LogListeners);
                HtmlNode mainNode = doc.DocumentNode.SelectSingleNode(listXPath);
                if (mainNode != null)
                {
                    HtmlNodeCollection node = mainNode.SelectNodes(string.Format(".//a[contains(@href, '{0}')]", cat.Keyword));
                    var result = node.Select(n => 
                        new Anchor()
                        {
                            AnchorText = n.InnerText,
                            Url = Util.UrlCombine(config.Url, n.GetAttributeValue("href", null))
                        });
                    return result;
                }
            }
            catch (Exception e)
            {
                Log(0, "Exception occurred when getting items from the page {0}: {1}", pageUrl, e);
            }
            return null;
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

        /// <summary>
        /// Get the max number of pages and the list url of this category
        /// </summary>
        /// <param name="siteUrl"></param>
        /// <param name="cat"></param>
        /// <returns></returns>
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
            const string mainDivXPath = "/html/body/div[contains(@class, 'main')]";
            const string endPageXPath = ".//a[contains(text(), '末页')]";
            try
            {
                HtmlDocument doc = Util.GetHtmlDocument(siteUrl, LogListeners);
                HtmlNode mainNode = doc.DocumentNode.SelectSingleNode(mainDivXPath);
                if (mainNode == null)
                {
                    throw new InvalidOperationException("Main div element cound not be found");
                }

                CategoryOutline outline = new CategoryOutline();
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
                   !Int32.TryParse(url.Substring(lastDashIndex + 1, lastDotIndex - lastDashIndex - 1), out number) )
                {
                    throw new InvalidOperationException(url + " is an invalid page url suffix");
                }
                
                outline.TotalPageNumber = number;
                outline.ListUrlPrefix = url.Substring(0, lastDashIndex + 1);
                Log(0, "Category out line: {0}, {1}", outline.TotalPageNumber, outline.ListUrlPrefix);
                return outline;
            }
            catch (Exception e) 
            {
                Log(0, "Exception happened when fetching category outline {0}: {1}", cat, e);
            }
            return null;
        }

        public void Log(int id, string format, params object[] args)
        {
            foreach (LogListener listener in LogListeners)
            {
                listener.Log(id, format, args);
            }
        }
        
        /// <summary>
        /// The template method of fetching the content of one page
        /// </summary>
        /// <param name="anchor"></param>
        /// <param name="doc"></param>
        /// <param name="destiDir"></param>
        protected void FetchAnchorContentTemplate(Anchor anchor, HtmlDocument doc, string destiDir)
        {
            const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='content']";
            HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);
            if (node == null)
            {
                Log(anchor.Id, "Warning: content node can't be found");
                return;
            }
            if (node.InnerText.Length == 0)
            {
                Log(anchor.Id, "Warning: Content empty, manually interferce maybe needed");
                return;
            }
            FetchAnchorContent(anchor, node, destiDir);
        }

        protected virtual void FetchAnchorContent(Anchor anchor, HtmlNode node, string destiDir)
        {
            try
            {
                string filePath = GetFilePath(destiDir, anchor.AnchorText);
                Log(anchor.Id, "Write text file {0} with url {1}", filePath, anchor.Url);
                FetchText(filePath, node, anchor);
            }
            catch (Exception e)
            {
                Log(anchor.Id, "Fetch text file with url {0}: {1}", anchor.Url, e); 
            }
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
                    FetchAnchorContentTemplate(anchor, doc, destiDir);
                }
            }
            catch (Exception e)
            {
                Log(anchor.Id, "[Error] Exception occurred when fetching {0}: {1}", anchor.Url, e);
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
                Log(anchor.Id, "Literature Warning: {0} empty for {1}", anchor.AnchorText, anchor.Url);
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
