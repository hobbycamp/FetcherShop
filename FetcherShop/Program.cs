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
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("A configuration file is required");
                return;
            }
            FetcherConfig siteConfig = ReadConfig(args[0]);
            if (siteConfig == null)
            {
                Console.WriteLine("Fetcher configuration is null");
                return;
            }
            InitializeLogger(siteConfig.LogDirectory);
            foreach (Zone zone in siteConfig.Zones)
            {
                zone.SiteUrl = siteConfig.RootUrl;

                GeneralLogger.Instance().Log("Start to fetch zone {0}", zone.Name);

                AbstractFetcher fetcher = FetcherFactory.CreateFetcher(zone);
                if (fetcher == null)
                {
                    GeneralLogger.Instance().Log("Unknown or unsupported zone {0}", zone.Name);
                    continue;
                }
                fetcher.Fetch();
            }
            
            GeneralLogger.Instance().Log("Finish all categories");
        }

        static FetcherConfig ReadConfig(string configPath)
        {
            if (!Path.IsPathRooted(configPath))
            {
                // If given a relative path, then combine it with the current directory
                configPath = Path.Combine(Environment.CurrentDirectory, configPath);
            }
            using (Stream stream = new FileStream(configPath, FileMode.Open))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(FetcherConfig));
                return (FetcherConfig)serializer.Deserialize(stream);
            }
        }

        static void UpdateConfig(string configPath, FetcherConfig siteConfig)
        {
            if (!Path.IsPathRooted(configPath))
            {
                // If given an absolute path, then combine it with the current directory
                configPath = Path.Combine(Environment.CurrentDirectory, configPath);
            }
            using (FileStream newFileStream = new FileStream(configPath, FileMode.Create))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(FetcherConfig));
                // Write back config file
                serializer.Serialize(newFileStream, siteConfig);
            }
        }

        static void InitializeLogger(string logDirectory)
        {
            GeneralLogger.Instance().AddLogListner(ConsoleLogListener.Instance());
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            string logFile = Path.Combine(logDirectory, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss") + ".txt");
            GeneralLogger.Instance().AddLogListner(new FileLogListener(logFile));
        }
    }
}
