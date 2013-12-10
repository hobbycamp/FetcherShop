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
        public Zone Zone { get; set; }
        private LogThread logThread = null;

        public AbstractFetcher(Zone zone)
        {
            Zone = zone;
        }

        protected void InitializeAnchorFileLogListener()
        {
            if (!Directory.Exists(Zone.RunConfiguration.LogDirectory))
            {
                Directory.CreateDirectory(Zone.RunConfiguration.LogDirectory);
            }
            string logFile = Path.Combine(Zone.RunConfiguration.LogDirectory, 
                Zone.Name + DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt");

            // Initialize a central log items queue
            BlockingCollection<LogItem> queue = new BlockingCollection<LogItem>();
            GeneralLogger.Instance().AddLogListner(new AnchorFileLogListener(queue));

            logThread = new LogThread(queue, logFile);
            logThread.StartLog();
        }
        
        public void Fetch()
        {
            InitializeAnchorFileLogListener();
            if (!Zone.PreFetch())
            {
                GeneralLogger.Instance().Log(LogLevel.Error, 0, "[Crash] Failed to get the outline information of category {0}", Zone.Name);
                return;
            }

            int startAnchorId = 1;
            //bool hitLastRecord = false;
            for (int i = 1; i <= Zone.TotalPageNumber; i++)
            {
                string url = Zone.GetPageUrl(i);
                // Fetch all the items in this page.
                IEnumerable<Anchor> allItems = Zone.GetItems(url);
                if (allItems == null)
                    continue;
                //hitLastRecord = IsHitLastRecord(allItems, Category);
                GeneralLogger.Instance().Log(LogLevel.Information, 0, "Number of items is {0} for {1}", allItems.Count(), url);
                Interlocked.Add(ref numberOfItems, allItems.Count());
                foreach (Anchor anchor in allItems)
                {
                    if (anchor.AbsoluteUrl != null)
                    {
                        //if (startAnchorId == 1)
                        //{
                        //    Category.LastestRecordUrl = anchor.Url;
                        //}
                        anchor.Id = startAnchorId++;
                            
                        ThreadPool.QueueUserWorkItem(new WaitCallback(MyWaitCallback), new ObjectState()
                        {
                            Anchor = anchor,
                            Zone = Zone
                        });                            
                    }
                }
            }
            finished.WaitOne();
            GeneralLogger.Instance().Log(LogLevel.Information, 0, "Finish fetching zone {0}", Zone);

            logThread.FinishLog();
        }

        public void MyWaitCallback(object state)
        {
            ObjectState os = (ObjectState)state;
            try
            {
                FetchAnchor(os.Anchor, os.Zone);
            }
            finally 
            {
                if (Interlocked.Decrement(ref numberOfItems) == 0)
                {
                    GeneralLogger.Instance().Log(LogLevel.Information, 0, "Finished all queue items for {0}", os.Zone.Name);
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
        //bool IsHitLastRecord(IEnumerable<Anchor> anchor, Category cat)
        //{
        //    if (anchor == null)
        //        return false;
        //    foreach (Anchor a in anchor)
        //    {
        //        if (a.Url == cat.LastestRecordUrl)
        //            return true;
        //    }
        //    return false;
        //}
        
        

        
        
        ///// <summary>
        ///// The template method of fetching the content of one item
        ///// </summary>
        ///// <param name="anchor"></param>
        ///// <param name="doc"></param>
        ///// <param name="destiDir"></param>
        //protected void FetchAnchorContentTemplate(Anchor anchor, HtmlDocument doc, string destiDir)
        //{
        //    const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='content']";
        //    HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);
        //    if (node == null)
        //    {
        //        Log(LogLevel.Warning, anchor.Id, "Warning: content node can't be found");
        //        return;
        //    }
        //    if (node.InnerText.Length == 0)
        //    {
        //        Log(LogLevel.Warning, anchor.Id, "Warning: Content empty, manually interferce maybe needed");
        //        return;
        //    }
        //    FetchAnchorContent(anchor, node, destiDir);
        //}

        protected virtual void FetchAnchorContent(Anchor anchor, HtmlNode node, string destiDir)
        {
            try
            {
                bool isExisted;
                string filePath = GetFilePath(destiDir, anchor.AnchorText, ".txt", out isExisted);
                //if (Category.Overrite || !isExisted)
                //{
                GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Write text file {0} with url {1}", filePath, anchor.AbsoluteUrl);
                FetchText(filePath, node, anchor);
                //}
            }
            catch (Exception e)
            {
                GeneralLogger.Instance().Log(LogLevel.Error, anchor.Id, "Exception: Fetch text file with url {0}: {1}", anchor.AbsoluteUrl, e); 
            }
        }

        /// <summary>
        /// Start to fetch an anchor
        /// </summary>
        /// <param name="anchor"></param>
        protected void FetchAnchor(Anchor anchor, Zone zone)
        {
            try
            {
                HtmlDocument doc = Util.GetHtmlDocument(anchor);
                //bool filterMatch = false;
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
                //if (!filterMatch)
                //{
                string dir = zone.RunConfiguration.OutputDirectory;
                if (!Directory.Exists(dir))
                {
                    GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Create directory {0} for zone {1}", dir, zone.Name);
                    Directory.CreateDirectory(dir);
                }
                const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='content']";
                HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);
                if (node == null)
                {
                    GeneralLogger.Instance().Log(LogLevel.Warning, anchor.Id, "Warning: content node can't be found");
                    return;
                }
                if (node.InnerText.Length == 0)
                {
                    GeneralLogger.Instance().Log(LogLevel.Warning, anchor.Id, "Warning: Content empty, manually interferce maybe needed");
                    return;
                }
                FetchAnchorContent(anchor, node, dir);
                //}
            }
            catch (Exception e)
            {
                GeneralLogger.Instance().Log(LogLevel.Error, anchor.Id, "[Error] Exception occurred when fetching {0}: {1}", anchor.AbsoluteUrl, e);
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

                writer.WriteLine(anchor.AbsoluteUrl);
            }

            FileInfo info = new FileInfo(filePath);
            if (info.Length == 0)
            {
                GeneralLogger.Instance().Log(LogLevel.Warning, anchor.Id, "Literature Warning: {0} empty for {1}", anchor.AnchorText, anchor.AbsoluteUrl);
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

        protected string GetFilePath(string dir, string anchorText, string suffix, out bool isExisted)
        {
            // Replace the invalid file name chars with whitespace
            anchorText = Util.FilterEntryName(anchorText);
            return Util.GetNewFilePath(dir, anchorText, suffix, out isExisted);
        }
    }
}
