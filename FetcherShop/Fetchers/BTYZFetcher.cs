using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using HtmlAgilityPack;
using FetcherShop.Helpers;

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
        //private static string sResponseEncoding = "gb2312";

        public BTYZFetcher(Category category)
            : base(category)
        { }

        protected override void FetchPageContent(Anchor anchor, HtmlNode node, string destiDir)
        {
            destiDir = Path.Combine(destiDir, Util.FilterEntryName(anchor.AnchorText));
            if (!Directory.Exists(destiDir))
            {
                Log(anchor.Id, "Create directory {0} for {1}", destiDir, Category.Keyword);
                Directory.CreateDirectory(destiDir);
            }

            base.FetchPageContent(anchor, node, destiDir);

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
                        DownloadRemoteImageFileWithRetry(anchor, imageSrcUrl, GetFilePath1(destiDir, imageFileName));
                    }
                }
            }   
        }

        private void DownloadRemoteImageFileWithRetry(Anchor anchor, string uri, string fileName)
        {
            try {
                Log(anchor.Id, "Begin downloading remote image file {0}", uri);
                Util.DoWithRetry("DownloadRemoteImageFile",
                    () => { DownloadRemoteImageFile(anchor, uri, fileName); },
                    anchor.Id,
                    LogListeners,
                    3,
                    5000
                    );
            } catch (Exception e)
            {
                Log(anchor.Id, "Erroring: Downloading image file of {0} failed with exception {1}", anchor.Url, e.ToString());
            }
        }

        private void DownloadRemoteImageFile(Anchor anchor, string uri, string fileName)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                using (var response = (HttpWebResponse)request.GetResponse())
                {

                    // Check that the remote file was found. The ContentType
                    // check is performed since a request for a non-existent
                    // image file might be redirected to a 404-page, which would
                    // yield the StatusCode "OK", even though the image was not
                    // found.
                    if ((response.StatusCode == HttpStatusCode.OK ||
                        response.StatusCode == HttpStatusCode.Moved ||
                        response.StatusCode == HttpStatusCode.Redirect)
                        //&& response.ContentType.StartsWith("image", StringComparison.OrdinalIgnoreCase)
                        )
                    {
                        // if the remote file was found, download oit
                        using (Stream inputStream = response.GetResponseStream())
                        {
                            using (Stream outputStream = File.OpenWrite(fileName))
                            {
                                byte[] buffer = new byte[8192];
                                int bytesRead;
                                do
                                {
                                    bytesRead = inputStream.Read(buffer, 0, buffer.Length);
                                    outputStream.Write(buffer, 0, bytesRead);
                                } while (bytesRead != 0);
                            }
                        }
                        Log(anchor.Id, "Finish writing image file {0}", fileName);
                    }
                }
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    if (((HttpWebResponse)e.Response).StatusCode != HttpStatusCode.NotFound)
                    {
                        throw;
                    }
                }
            }
        }

        protected string GetFilePath1(string dir, string imageFileName)
        {
            imageFileName = Util.FilterEntryName(imageFileName);
            string filePath = Path.Combine(dir, imageFileName);
            if (!File.Exists(filePath))
                return filePath;

            string fileNameFormat = GetFileNameFormat(imageFileName);
            if (fileNameFormat == null)
            {
                //Log("Warning: {0} is an invalid image file name", imageFileName);
                return null; ;
            }
            string filePathFormat = Path.Combine(dir, fileNameFormat);
            int i = 0;
            string filePath1 = "";
            do
            {
                filePath1 = string.Format(filePathFormat, i++);

            } while (File.Exists(filePath1));

            return filePath1;
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
            Log(anchor.Id, "Begin fetching torrent file");
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
                        DownloadTorrentWithRetry(anchor, hrefUrl, destiDir);
                    }
                }
            }
            else
            {
                Log(anchor.Id, "Manually downloading torrent needed for {0}", anchor.Url);
            }
        }

        private void DownloadTorrentWithRetry(Anchor anchor, string hrefUrl, string destiDir)
        {
            try
            {
                Util.DoWithRetry("DownloadTorrent",
                    () => { DownloadTorrent(anchor, Util.GetDownloadUrl(hrefUrl), hrefUrl, destiDir); },
                    anchor.Id,
                    LogListeners,
                    3,
                    2000
                    );
            }
            catch (Exception e)
            {
                Log(anchor.Id, "Warning: Downloading torrent file of {0} failed with exception {1}", anchor.Url, e.ToString());
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

        private void DownloadTorrent(Anchor anchor, string downloadUrl, string hrefUrl, string destiDir)
        {
            if (string.IsNullOrEmpty(downloadUrl))
            {
                Log(anchor.Id, "Error: download url is null or empty for {0} {1}", anchor.Url, anchor.AnchorText);
                return;
            }
            Encoding encoding = Encoding.GetEncoding(sRequestEncoding);
            const string mark = "ref=";
            int index = hrefUrl.IndexOf(mark);
            string code = hrefUrl.Substring(index + mark.Length);
            byte[] bytesToPost = encoding.GetBytes(BodyString(code));
            PostDataToUrl(bytesToPost, downloadUrl, hrefUrl, anchor, destiDir);
        }

        private void PostDataToUrl(byte[] data, string url, string refererUrl, Anchor anchor, string destiDir)
        {
            WebRequest webRequest = WebRequest.Create(url);
            HttpWebRequest httpRequest = webRequest as HttpWebRequest;
            if (httpRequest == null)
            {
                throw new Exception(
                    string.Format("Invalid url string: {0}", url)
                );
            }
 
            httpRequest.UserAgent = sUserAgent;
            httpRequest.ContentType = sContentType;            
            httpRequest.Method = "POST";
            httpRequest.Referer = refererUrl;
 
            // Fill the content of post data
            httpRequest.ContentLength = data.Length;
            Stream requestStream = httpRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
 
            // Get the response
            Stream responseStream = null;
            try
            {
                string fileEntry = Util.FilterEntryName(anchor.AnchorText);
                string fileName = Util.GetNewFilePath(destiDir, fileEntry, ".torrent");

                responseStream = httpRequest.GetResponse().GetResponseStream();
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

                Log(anchor.Id, "Finish writing torrent file {0}", fileName);
            }
            catch (Exception e)
            {
                // log error
                Log(anchor.Id, "POST operation occurs exceptions：{0}", e.Message);
                throw e;
            }
            finally
            {
                if (responseStream != null)
                    responseStream.Close();
            }
        }
    }
}
