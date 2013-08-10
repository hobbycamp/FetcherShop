using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using System.Xml;
using System.Xml.Serialization;
using System.Globalization;
using FetcherShop.Logger;
using FetcherShop.Fetchers;

namespace FetcherShop
{
    class Program
    {
        //static string logFile = "";
        
        static List<LogListener> listenerCollection = new List<LogListener>();
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("A configuration file is required");
                return;
            }
            string configFilePath = args[0];
            if (!Path.IsPathRooted(args[0]))
            {
                // If given an absolute path, then combine it with the current directory
                configFilePath = Path.Combine(Environment.CurrentDirectory, configFilePath);
            }

            XmlSerializer serializer = new XmlSerializer(typeof(WebSiteConfig));
            Stream stream = new FileStream(configFilePath, FileMode.Open);
            WebSiteConfig siteConfig = (WebSiteConfig)serializer.Deserialize(stream);
            stream.Close();

            //logFile = Path.Combine(siteConfig.LogDirectory, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt");
            listenerCollection.Add(new ConsoleLogListener());
            //listenerCollection.Add(new FileLogListener(logFile));
            Log("Read the configuration of the web site");
            if (siteConfig == null)
            {
                Log("Site config null");
                return;
            }

            foreach (Category cat in siteConfig.Categories)
            {
                Log("Start to fetch category {0}", cat.Keyword);
                // Experiments
                //Anchor firstItem = GetFirstRecord(siteConfig.Url, cat);
                //cat.LastestRecordUrl = UrlCombine(siteConfig.Url, firstItem.Url);
                //Log("First item {0} with url {1}", firstItem.AnchorText, firstItem.Url);
                //StartFetch(firstItem, siteConfig.Url, siteConfig.OutputDirectory, cat);
                //JTLLFetcher fetcher = new JTLLFetcher(cat);
                //fetcher.Fetch(siteConfig);
                AbstractFetcher fetcher = FetcherFactory.CreateFetcher(cat);
                fetcher.Fetch(siteConfig);
            }
            //siteConfig.Categories[0].LastestRecordUrl = "http://780pp.com";

            //FileStream newFileStream = new FileStream(filePath, FileMode.Create);

            //// Write back config file
            //serializer.Serialize(newFileStream, siteConfig);
            //newFileStream.Close();
            Console.WriteLine("Finish all categories");
            //test();
        }

    //    static void test()
    //    {
    //        WebRequest req = WebRequest.Create("http://780pp.com/html/jiatingluanlun/11251534612011.html");

    //        WebResponse resp = req.GetResponse();
    //        HtmlDocument doc = new HtmlDocument();
    //        doc.Load(resp.GetResponseStream(), Encoding.GetEncoding("GB2312"));
    //        const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='content']";
    //        HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);

    //        foreach (HtmlNode n in node.ChildNodes)
    //        {
    //            Console.WriteLine(n.Name);
    //            Console.WriteLine(n.InnerText);
    //        }
    //    }

        static void Log(string format, params object[] args)
        {           
            foreach (LogListener listener in listenerCollection)
            {
                listener.Log(0, format, args);
            }
        }


    //    static string UrlCombine(string firstPart, string secondPart)
    //    {
    //        if (firstPart == null)
    //        {
    //            return secondPart;
    //        }

    //        if (secondPart == null)
    //        {
    //            return firstPart;
    //        }

    //        if (firstPart.EndsWith("/"))
    //        {
    //            if (secondPart.StartsWith("/"))
    //            {
    //                return firstPart + secondPart.Substring(1);

    //            }
    //            else
    //            {
    //                return firstPart + secondPart;
    //            }
    //        }
    //        else
    //        {
    //            if (!secondPart.StartsWith("/"))
    //            {
    //                return firstPart + "/" + secondPart;
    //            }
    //            else
    //            {
    //                return firstPart + secondPart;
    //            }
    //        }
    //    }

    //    static Anchor GetFirstRecord(string siteUrl, Category cat)
    //    {
    //        if (!siteUrl.EndsWith("/"))
    //        {
    //            siteUrl += "/"; 
    //        }

    //        siteUrl += ("html/" + cat.Keyword);
    //        if (!siteUrl.EndsWith("/"))
    //        {
    //            siteUrl += "/";
    //        }
    //        siteUrl += "index.html";
    //        WebRequest req = WebRequest.Create(siteUrl);
    //        WebResponse resp = req.GetResponse();

    //        Stream resStream = resp.GetResponseStream();
    //        HtmlDocument doc = new HtmlDocument();
    //        doc.Load(resStream, Encoding.GetEncoding("GB2312"));

    //        HtmlNode mainNode = doc.DocumentNode.SelectSingleNode("/html/body/div[contains(@class, 'main')]");
    //        if (mainNode == null)
    //        {
    //            Log("Main div element cound not be found");
    //            return null;
    //        }

    //        HtmlNode node = mainNode.SelectNodes(string.Format(".//a[contains(@href, '{0}')]", cat.Keyword)).FirstOrDefault();
    //        if (node == null)
    //            return null;
    //        resp.Close();
    //        return new Anchor()
    //        {
    //            AnchorText = node.InnerText,
    //            Url = node.GetAttributeValue("href", "")
    //        };
    //    }

    //    static Anchor GetPreviousItemAnchor(HtmlNode node)
    //    {
    //        const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='pagea']";
    //        HtmlNode pagea = node.SelectSingleNode(xpath);
    //        if (pagea == null)
    //        {
    //            Log("No pagea div found");
    //            return null;
    //        }

    //        string innerXML = pagea.InnerText;
    //        if (innerXML.Contains("上一篇：没有了"))
    //        {
    //            Log("Last item hitted");
    //            return null;
    //        }

    //        HtmlNode nd = pagea.SelectSingleNode("./a[1]");
    //        return new Anchor() {
    //            Url = nd.GetAttributeValue("href", null),
    //            AnchorText = nd.InnerText 
    //        };
    //    }

    //    static Anchor FetchCategory(Anchor anchor, string outputDirectory, Category cat)
    //    {
    //        WebRequest req = WebRequest.Create(anchor.Url);
    //        WebResponse resp = req.GetResponse();
    //        Stream stream = resp.GetResponseStream();

    //        HtmlDocument doc = new HtmlDocument();
    //        doc.Load(stream, Encoding.GetEncoding("GB2312"));
    //        foreach (Filter filter in cat.Filters)
    //        {
    //            // Assume output directory exists already
    //            string destiDir = Path.Combine(outputDirectory, filter.Accept);
    //            if (!Directory.Exists(destiDir))
    //            {
    //                Log("Create directory {0} for category {1} filter {2}", destiDir, cat.Keyword, filter.Accept);
    //                Directory.CreateDirectory(destiDir);
    //            }                
    //            string[] keywords = filter.Accept.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    //            bool flag = false;
    //            foreach (string keyword in keywords)
    //            {
    //                if (anchor.AnchorText.IndexOf(keyword) != -1)
    //                {
    //                    flag = true;
    //                    break;
    //                }                        
    //            }

    //            if (flag)
    //            {
    //                FetchArticleContent(anchor, doc, destiDir);
    //            }
    //        }
    //        return GetPreviousItemAnchor(doc.DocumentNode);
    //    }

    //    static void StartFetch(Anchor anchor, string webSiteUrl, string outputDirectory, Category cat)
    //    {
    //        while ((string.IsNullOrEmpty(cat.LastestRecordUrl)) || (anchor.Url != cat.LastestRecordUrl))
    //        {
    //            try {
    //                anchor.Url = UrlCombine(webSiteUrl, anchor.Url);
    //                anchor = FetchCategory(anchor, outputDirectory, cat);
    //                if (anchor == null)
    //                    break;
    //            } catch (Exception e)
    //            {
    //                Log(e.ToString());
    //                Log("Failed to fetch content for {0} with url {1}", anchor.AnchorText, anchor.Url);
    //            }
    //        }
            
    //        Log("Fetch category {0} completed", cat.Keyword);
    //    }

    //    static void FetchArticleContent(Anchor anchor, HtmlDocument doc, string destiDir)
    //    {
    //        const string xpath = "/html/body/div[contains(@class, 'main')]/div[@class='content']";
    //        HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);
    //        if (node == null)
    //            return;

    //        if(node.InnerText.Length == 0)
    //        {
    //            Log("Warning: Content empty, manually interferce needed");
    //            return;
    //        }
    //        string filePath = GetFilePath(destiDir, anchor.AnchorText);
    //        Log("Write file {0} for {1} with url {2}", filePath, anchor.AnchorText, anchor.Url);
    //        WriteAllText(filePath, node, anchor);
    //    }

    //    static void WriteAllText(string filePath, HtmlNode node, Anchor anchor)
    //    {
    //        using(StreamWriter writer = new StreamWriter(filePath))
    //        {
    //            foreach (HtmlNode child in node.ChildNodes)
    //            {
    //                if (child.NodeType == HtmlNodeType.Element && string.Compare("br", child.Name, true) == 0)
    //                {
    //                    writer.WriteLine();
    //                }
    //                else if (child.NodeType == HtmlNodeType.Text)
    //                {
    //                    writer.Write(child.InnerText.Trim(new char[] {' ', '\r', '\n'}));
    //                }
    //                else if (child.NodeType == HtmlNodeType.Element && string.Compare("p", child.Name, true) == 0)
    //                {
    //                    WriteAllTextFromParagraphElement(writer, child); 
    //                }
    //            }
    //        }

    //        FileInfo info = new FileInfo(filePath);
    //        if (info.Length == 0)
    //        {
    //            Log("Warning: {0} empty for {1}", anchor.AnchorText, anchor.Url);
    //        }                 
    //    }

    //    static void WriteAllTextFromParagraphElement(StreamWriter writer, HtmlNode node)
    //    {
    //        foreach (HtmlNode child in node.ChildNodes)
    //        {
    //            if (child.NodeType == HtmlNodeType.Element && string.Compare("br", child.Name, true) == 0)
    //            {
    //                writer.WriteLine();
    //            }
    //            else if (child.NodeType == HtmlNodeType.Text)
    //            {
    //                writer.Write(child.InnerText.Trim(new char[] { ' ', '\r', '\n' }));
    //            }                
    //        }
    //    }

    //    static string GetFilePath(string dir, string anchorText)
    //    {
    //        // Replace the invalid of file name chars with whitespace
    //        // Replace the invalid file name chars with whitespace
    //        StringBuilder sb = new StringBuilder(anchorText);
    //        int i = 0;
    //        for (i = 0; i < sb.Length; i++)
    //        {
    //            if (Array.IndexOf(INVALID_FILE_NAME_CHARS, sb[i]) != -1)
    //            {
    //                sb[i] = ' ';
    //            }
    //        }
    //        if (sb.ToString() != anchorText)
    //        {
    //            Log("File name change from {0} to {1}", anchorText, sb.ToString());
    //        }
    //        anchorText = sb.ToString();
    //        string filePath = Path.Combine(dir, anchorText + ".txt");
    //        if (!File.Exists(filePath))
    //            return filePath;
    //        string filePathFormat = Path.Combine(dir, anchorText + "{0}.txt");
    //        i = 0;
    //        string filePath1 = "";
    //        do
    //        {
    //            filePath1 = string.Format(filePathFormat, i++);

    //        } while (File.Exists(filePath1));
            
    //        return filePath1;
    //    }
    }
}
