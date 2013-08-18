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
        static List<LogListener> listenerCollection = new List<LogListener>();
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("A configuration file is required");
                return;
            }
            WebSiteConfig siteConfig = ReadConfig(args[0]);

            //logFile = Path.Combine(siteConfig.LogDirectory, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt");
            listenerCollection.Add(new ConsoleLogListener());
            //listenerCollection.Add(new FileLogListener(logFile));
            Log("Parse the configuration of the web site");
            if (siteConfig == null)
            {
                Log("Site config is null because configuration file path doesn't exist or is invalid");
                return;
            }

            foreach (Category cat in siteConfig.Categories)
            {
                Log("Start to fetch category {0}", cat.Keyword);
                AbstractFetcher fetcher = FetcherFactory.CreateFetcher(cat);
                fetcher.Fetch(siteConfig);
            }

            UpdateConfig(args[0], siteConfig);            
            Log("Finish all categories");
        }

        static void Log(string format, params object[] args)
        {           
            foreach (LogListener listener in listenerCollection)
            {
                listener.Log(LogLevel.Information, 0, format, args);
            }
        }

        static WebSiteConfig ReadConfig(string configPath)
        {
            if (!Path.IsPathRooted(configPath))
            {
                // If given an absolute path, then combine it with the current directory
                configPath = Path.Combine(Environment.CurrentDirectory, configPath);
            }
            using (Stream stream = new FileStream(configPath, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(WebSiteConfig));
                return (WebSiteConfig)serializer.Deserialize(stream);
            }
        }

        static void UpdateConfig(string configPath, WebSiteConfig siteConfig)
        {
            if (!Path.IsPathRooted(configPath))
            {
                // If given an absolute path, then combine it with the current directory
                configPath = Path.Combine(Environment.CurrentDirectory, configPath);
            }
            using (FileStream newFileStream = new FileStream(configPath, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(WebSiteConfig));
                // Write back config file
                serializer.Serialize(newFileStream, siteConfig);
            }
        }
    }
}
