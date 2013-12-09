using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using FetcherShop.Logger;
using HtmlAgilityPack;
using System.Net;

namespace FetcherShop.Helpers
{
    public static class Util
    {
        public static char[] INVALID_FILE_NAME_CHARS = new char[] { '/', '\\', '?', ':', '*', '<', '>', '|', '\"' };
        public static string UrlCombine(string firstPart, string secondPart)
        {
            if (firstPart == null || secondPart == null)
            {
                return null;
            }

            if (firstPart.EndsWith("/"))
            {
                if (secondPart.StartsWith("/"))
                {
                    return firstPart + secondPart.Substring(1);

                }
                else
                {
                    return firstPart + secondPart;
                }
            }
            else
            {
                if (!secondPart.StartsWith("/"))
                {
                    return firstPart + "/" + secondPart;
                }
                else
                {
                    return firstPart + secondPart;
                }
            }
        }

        public static string GetRemoteFileName(string uri)
        {
            int lastIndex = uri.LastIndexOf('/');
            if (lastIndex == -1)
                return null;
            // Get the part of the file name
            string fileName = uri.Substring(lastIndex + 1);
            if (string.IsNullOrEmpty(fileName))
            {
                fileName = Guid.NewGuid().ToString() + ".png";
            } 
            else if (!fileName.Contains('.'))
            {
                fileName += ".png";
            }
            else if (fileName[fileName.Length - 1] == '.')
            {
                fileName += "png";
            }

            return fileName;
        }

        public static string FilterEntryName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name;

            // Replace the invalid file name chars with whitespace
            StringBuilder sb = new StringBuilder(name);
            for (int i = 0; i < sb.Length; i++)
            {
                if (Array.IndexOf(Util.INVALID_FILE_NAME_CHARS, sb[i]) != -1)
                {
                    sb[i] = ' ';
                }
            }
            return sb.ToString();
        }

        public static void DoWithRetry(string actionName, Action func, int id, int retryCount = 5, int retryInterval = 120000)
        {
            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    func();
                    return;
                }
                catch (Exception)
                {
                    //Log(listeners, id, "Warning: {0}th {1} failed with exception {2}", i, actionName,  e);
                    if (i == (retryCount - 1))
                    {
                        throw;
                    }
                    Thread.Sleep(retryInterval);
                }
            }
        }

        //public static void Log(List<LogListener> LogListeners, int id, string format, params object[] args)
        //{
        //    if (LogListeners == null)
        //        return;
        //    foreach (var listener in LogListeners)
        //    {
        //        listener.Log(id, format, args); 
        //    } 
        //}

        public static string GetNewFilePath(string dir, string fileName, string suffix, out bool isExisted)
        {
            string filePath = Path.Combine(dir, fileName + suffix);
            if (!File.Exists(filePath))
            {
                isExisted = false;
                return filePath;
            }
            string filePathFormat = Path.Combine(dir, fileName + "{0}" + suffix);
            int i = 1;
            string filePath1 = "";
            do
            {
                filePath1 = string.Format(filePathFormat, i++);

            } while (File.Exists(filePath1));
            isExisted = true;
            return filePath1;
        }

        public static string GetDownloadUrl(string refererUrl)
        {
            if (refererUrl.IndexOf("jandown.com") != -1)
            {
                return "http://www.jandown.com/fetch.php";
            }
            else if (refererUrl.IndexOf("mimima.com") != -1)
            {
                return "http://www6.mimima.com/fetch.php";
            }

            return null; 
        }

        public static HtmlDocument GetHtmlDocument(Anchor url)
        {
            HtmlDocument doc = null;
            DoWithRetry("GetHtmlDocument", () =>
            {
                WebRequest req = WebRequest.Create(url.AbsoluteUrl);
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();

                doc = new HtmlDocument();
                doc.Load(stream, Encoding.GetEncoding("GB2312"));
                resp.Close();
            }, url.Id, 1, 20000);

            return doc;
        }

        public static HtmlDocument GetHtmlDocument(string url)
        {
            HtmlDocument doc = null;
            DoWithRetry("GetHtmlDocument", () =>
            {
                WebRequest req = WebRequest.Create(url);
                WebResponse resp = req.GetResponse();
                Stream stream = resp.GetResponseStream();

                doc = new HtmlDocument();
                doc.Load(stream, Encoding.GetEncoding("GB2312"));
                resp.Close();
            }, 0, 2, 20000);

            return doc;
        }

    }
}
