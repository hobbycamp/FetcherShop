﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using FetcherShop.Helpers;
using FetcherShop.Logger;

namespace FetcherShop.Fetchers
{
    public class BTYZFetcher : AbstractFetcher
    {
        private static string sUserAgent =
                       "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 5.2; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        private static string sBoundary = "---------------------------7dd731a807a4";
        private static string sContentType =
                "multipart/form-data; boundary=---------------------------7dd731a807a4";
        private static string sRequestEncoding = "ascii";

        public BTYZFetcher(Zone zone)
            : base(zone)
        { }

        protected override void ExtractAnchorContent(Anchor anchor, HtmlNode node, string destiDir)
        {
            destiDir = Path.Combine(destiDir, Util.FilterEntryName(anchor.AnchorText));
            if (!Directory.Exists(destiDir))
            {
                GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Create directory {0} for {1}", destiDir, Zone.Name);
                Directory.CreateDirectory(destiDir);
            }

            base.ExtractAnchorContent(anchor, node, destiDir);

            FetchImageContent(anchor, node, destiDir);

            FetchTorrentContent(anchor, node, destiDir);
        }

        protected void FetchImageContent(Anchor anchor, HtmlNode node, string destiDir)
        {
            foreach (HtmlNode child in node.ChildNodes)
            {
                if (child.NodeType == HtmlNodeType.Element && string.Compare(child.Name, "img", true) == 0)
                {
                    string imageSrcUrl = child.GetAttributeValue("src", null);
                    if (imageSrcUrl != null)
                    { 
                        string imageFileName = Util.GetRemoteFileName(imageSrcUrl);
                        if (imageFileName != null)
                        {
                            bool isExisted = IsImageFileExist(destiDir, imageFileName);
                            //if (!isExisted || Category.Overrite)
                            //{
                            string imageFilePath = GetImageFilePath(destiDir, imageFileName);
                            if (imageFilePath == null)
                            {
                                GeneralLogger.Instance().Log(LogLevel.Warning, anchor.Id, "Failed to combine a valid image file path with {0}",
                                    imageFileName);
                            }
                            else
                            {
                                DownloadRemoteImageFileWithRetry(anchor, imageSrcUrl, imageFilePath);
                            }
                            //}
                        }
                    }
                }
            }   
        }

        private void CheckImageFile(string imageFilePath, out bool isExisted)
        {
            isExisted = File.Exists(ChangeExtension(imageFilePath));
        }

        private bool IsImageFileExist(string dir, string imageFileName)
        {
            imageFileName = Util.FilterEntryName(imageFileName);
            string imageFilePath = Path.Combine(dir, imageFileName);
            return File.Exists(imageFilePath) ||
                File.Exists(ChangeExtension(imageFilePath));
        }

        private void DownloadRemoteImageFileWithRetry(Anchor anchor, string uri, string fileName)
        {
            try {
                GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Begin downloading remote image file {0}", uri);
                Util.DoWithRetry("DownloadRemoteImageFile",
                    () => { DownloadRemoteImageFile(anchor, uri, fileName); },
                    anchor.Id,
                    3,
                    10000
                );
               
            } catch (Exception e)
            {
                LogLevel l = LogLevel.ImageError;
                if (uri.Contains("www.shotpix.com/images") || uri.Contains("www.axiazai.com"))
                {
                    l = LogLevel.ShotpixOrAXiaZaiImageError;
                }
                GeneralLogger.Instance().Log(l, anchor.Id, "Error: Downloading image file {0} of {1} failed with exception {2}", uri, anchor.AbsoluteUrl, e.ToString());
            }
        }

        private void DownloadRemoteImageFile(Anchor anchor, string uri, string fileName)
        {
            Exception exp = null;
            bool isUseProxy = false;
            try
            {
                MyWebClient w = new MyWebClient();
                w.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                w.DownloadFile(uri, fileName);
                GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Finish writing image file {0}", fileName);
            }
            catch (WebException webExp)
            {
                exp = webExp;
                if (webExp.Status == WebExceptionStatus.ConnectFailure ||
                    webExp.Status == WebExceptionStatus.UnknownError ||
                    webExp.Status == WebExceptionStatus.ProtocolError ||
                    webExp.Status == WebExceptionStatus.NameResolutionFailure ||
                    webExp.Status == WebExceptionStatus.Timeout ||
                    webExp.Status == WebExceptionStatus.ReceiveFailure ||
                    webExp.Status == WebExceptionStatus.ConnectionClosed)
                {
                    isUseProxy = true;
                }
                else
                {
                    GeneralLogger.Instance().Log(LogLevel.Error, 0, "Web exception status {0}", webExp.Status.ToString());
                }

                if (webExp.Response != null)
                {
                    if (((HttpWebResponse)webExp.Response).StatusCode == HttpStatusCode.Forbidden ||
                        ((HttpWebResponse)webExp.Response).StatusCode == HttpStatusCode.InternalServerError)
                    {
                        isUseProxy = true;
                    }
                    else if (((HttpWebResponse)webExp.Response).StatusCode == HttpStatusCode.NotFound)
                    {
                        using (File.Create(ChangeExtension(fileName))) { };
                    }
                }                
            }
            // There is an exception occurred when trying to download the image for the first time
            // Would try to download it again with proxy if necessary
            if (exp != null)
            {
                MyWebClient w = new MyWebClient();
                if (isUseProxy)
                {
                    w.Proxy = new WebProxy("127.0.0.1", 8087);
                }
                w.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)");
                w.DownloadFile(uri, fileName);
                GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Finish writing image file {0}", fileName);
            }
        }

        private string ChangeExtension(string filePath)
        {
            int lastDot = filePath.LastIndexOf('.');
            return filePath.Substring(0, lastDot) + "NOT FOUND" + ".txt";
        }


        protected string GetImageFilePath(string dir, string imageFileName)
        {
            imageFileName = Util.FilterEntryName(imageFileName);
            return Path.Combine(dir, imageFileName);
            //if (!File.Exists(filePath))
            //{
            //    return filePath;
            //}

            //string fileNameFormat = GetFileNameFormat(imageFileName);
            //if (fileNameFormat == null)
            //{
            //    return null;
            //}
            //string filePathFormat = Path.Combine(dir, fileNameFormat);
            //int i = 1;
            //string filePath1 = "";
            //do
            //{
            //    filePath1 = string.Format(filePathFormat, i++);

            //} while (File.Exists(filePath1));
            //return filePath1;
        }

        private string GetFileNameFormat(string fileName)
        {
            int lastIndex = fileName.LastIndexOf('.');
            if (lastIndex == -1)
                return null;
            return fileName.Substring(0, lastIndex) + "{0}." + fileName.Substring(lastIndex + 1);
        }

        private void FetchTorrentContent(Anchor anchor, HtmlNode node, string destiDir)
        {
            string xpath = "/html/body//a[contains(text(), 'jandown.com')]";
            HtmlNodeCollection nodes = node.SelectNodes(xpath);
            if (nodes == null || nodes.Count == 0) {
                xpath = "/html/body//a[contains(text(), 'mimima.com')]";
                nodes = node.SelectNodes(xpath);
            }
            if (nodes != null)
            {
                foreach (HtmlNode n in nodes)
                {
                    string hrefUrl = n.GetAttributeValue("href", null);
                    if (hrefUrl != null)
                    {
                        bool isExisted;
                        string fileEntry = Util.FilterEntryName(anchor.AnchorText);
                        string fileName = Util.GetNewFilePath(destiDir, fileEntry, ".torrent", out isExisted);
                        //if (Category.Overrite || !isExisted)
                        //{
                        DownloadTorrentWithRetry(anchor, hrefUrl, destiDir, fileName);
                        //}
                    }
                }
            }
            else
            {
                GeneralLogger.Instance().Log(LogLevel.Warning, anchor.Id, "Manually downloading torrent needed for {0}", anchor.AbsoluteUrl);
            }
        }

        private void DownloadTorrentWithRetry(Anchor anchor, string hrefUrl, string destiDir, string fileName)
        {
            GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Begin fetching torrent file");
            try
            {
                Util.DoWithRetry("DownloadTorrent",
                    () => { DownloadTorrent(anchor, Util.GetDownloadUrl(hrefUrl), hrefUrl, destiDir, fileName); },
                    anchor.Id,
                    3,
                    2000
                    );
            }
            catch (Exception e)
            {
                GeneralLogger.Instance().Log(LogLevel.TorrentError, anchor.Id, "Error: Downloading torrent file of {0} failed with exception {1}", anchor.AbsoluteUrl, e.ToString());
            }
        }

        private string BodyString(string postData)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("--");
            sb.Append(sBoundary);
            sb.Append("\r\n");
            sb.Append("Content-Disposition: form-data; name=\"code\"\r\n\r\n");
            sb.Append(postData);
            sb.Append("\r\n--");
            sb.Append(sBoundary);
            sb.Append("--\r\n");
            return sb.ToString();
        }

        private void DownloadTorrent(Anchor anchor, string downloadUrl, string hrefUrl, string destiDir, string fileName)
        {
            if (string.IsNullOrEmpty(downloadUrl))
            {
                GeneralLogger.Instance().Log(LogLevel.Warning, anchor.Id, "Warning: downloading url is null or empty for {0} {1}", anchor.AbsoluteUrl, anchor.AnchorText);
                return;
            }
            Encoding encoding = Encoding.GetEncoding(sRequestEncoding);
            const string mark = "ref=";
            int index = hrefUrl.IndexOf(mark);
            string code = hrefUrl.Substring(index + mark.Length);
            byte[] bytesToPost = encoding.GetBytes(BodyString(code));
            PostDataToUrl(bytesToPost, downloadUrl, hrefUrl, anchor, destiDir, fileName);
        }

        private void PostDataToUrl(byte[] data, string url, string refererUrl, Anchor anchor, string destiDir, string fileName)
        {
            // Gets or sets the time-out value in milliseconds for the System.Net.HttpWebRequest.GetResponse()
            // and System.Net.HttpWebRequest.GetRequestStream() methods.
            const int TIME_OUT = 300000;
            WebRequest webRequest = WebRequest.Create(url);
            HttpWebRequest httpRequest = webRequest as HttpWebRequest;
            if (httpRequest == null)
            {
                throw new InvalidOperationException(
                    string.Format("Invalid url string: {0}", url)
                );
            }

            httpRequest.UserAgent = sUserAgent;
            httpRequest.ContentType = sContentType;
            httpRequest.Method = "POST";
            httpRequest.Referer = refererUrl;
            httpRequest.Timeout = TIME_OUT;

            // Fill the content of post data
            httpRequest.ContentLength = data.Length;
            Stream requestStream = httpRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();

            // Get the response
            using (var responseStream = httpRequest.GetResponse().GetResponseStream())
            {
                using (Stream outputStream = File.OpenWrite(fileName))
                {
                    byte[] buffer = new byte[8192];
                    int bytesRead;
                    do
                    {
                        bytesRead = responseStream.Read(buffer, 0, buffer.Length);
                        outputStream.Write(buffer, 0, bytesRead);
                    } while (bytesRead != 0);
                }
            }

            GeneralLogger.Instance().Log(LogLevel.Information, anchor.Id, "Finish writing torrent file {0}", fileName);
        }
    }
}
