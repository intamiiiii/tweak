﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Drawing;
using System.Net.Sockets;

namespace Std.Network
{
    /// <summary>
    /// Http negotiation library v2.0
    /// </summary>
    public static class HttpWeb
    {
        static string userAgentString = "Std/HttpLib 2.0";

        /// <summary>
        /// User agent string
        /// </summary>
        public static string UserAgentString
        {
            get { return userAgentString; }
            set { userAgentString = value; }
        }

        static int timeoutInterval = 5000;

        /// <summary>
        /// Timeout interval
        /// </summary>
        public static int TimeoutInterval
        {
            get { return timeoutInterval; }
            set { timeoutInterval = value; }
        }

        static bool expect100continue = false;

        /// <summary>
        /// Use expect100continue
        /// </summary>j
        public static bool Expect100Continue
        {
            get { return expect100continue; }
            set { expect100continue = value; }
        }

        #region Converter delegate definitions and common converters

        /// <summary>
        /// Stream convert to preferred class
        /// </summary>
        public delegate T StreamConverter<out T>(Stream stream);

        /// <summary>
        /// Response convert to preferred class
        /// </summary>
        public delegate T ResponseConverter<out T>(HttpWebResponse response);

        /// <summary>
        /// Common stream converters
        /// </summary>
        public static class StreamConverters
        {
            /// <summary>
            /// Read string from stream
            /// </summary>
            public static string ReadString(Stream stream)
            {
                using (var sr = new StreamReader(stream))
                    return sr.ReadToEnd();
            }

            /// <summary>
            /// Read image data from stream
            /// </summary>
            public static Image ReadImage(Stream stream)
            {
                return Image.FromStream(stream);
            }
        }

        /// <summary>
        /// Common response converters
        /// </summary>
        public static class ResponseConverters
        {
            public static Stream GetStream(HttpWebResponse res)
            {
                return res.GetResponseStream();
            }
        }

        #endregion

        /// <summary>
        /// Create HTTP request
        /// </summary>
        /// <param name="uri">HTTP uri</param>
        /// <param name="method">using method</param>
        /// <param name="contentType">content type</param>
        /// <param name="credential">using credential</param>
        /// <returns>request</returns>
        public static HttpWebRequest CreateRequest(
            Uri uri, string method = "GET",
            string contentType = "application/x-www-form-urlencoded",
            ICredentials credential = null)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            HttpWebRequest req = null;
            // create request
            try
            {
                req = WebRequest.Create(uri) as HttpWebRequest;
                if (req == null)
                    throw new NotSupportedException("Std.HttpWeb supports only HTTP.");
            }
            catch (NotSupportedException)
            {
                throw;
            }

            // set parameters
            req.ServicePoint.Expect100Continue = expect100continue;
            req.UserAgent = userAgentString;
            req.Timeout = timeoutInterval;
            req.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

            // argument values
            req.ContentType = contentType;
            req.Method = method;
            req.Credentials = credential;
            return req;
        }


        /// <summary>
        /// Connect to web and download response
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <param name="req">requestor</param>
        /// <param name="streamconv">stream converter</param>
        /// <param name="responseconv">response converter</param>
        /// <param name="senddata">sending data info</param>
        /// <returns>converted data</returns>
        /// <exception cref="System.ArgumentException"></exception>
        public static OperationResult<T> WebConnect<T>(
            HttpWebRequest req,
            StreamConverter<T> streamconv = null,
            ResponseConverter<T> responseconv = null,
            byte[] senddata = null)
        {
            if(!(streamconv == null ^ responseconv == null))
                throw new ArgumentException("StreamConverter or ResponseConverter is must set.");

            try
            {
                // upload data
                if (senddata != null && senddata.Length > 0)
                {
                    // content length
                    req.ContentLength = senddata.Length;

                    // request streams
                    using (var s = req.GetRequestStream())
                    {
                        s.Write(senddata, 0, senddata.Length);
                    }
                }
                return TreatWebResponse((HttpWebResponse)req.GetResponse(), streamconv, responseconv);
            }
            catch (SocketException e)
            {
                return new OperationResult<T>(req.RequestUri, e);
            }
            catch (IOException e)
            {
                return new OperationResult<T>(req.RequestUri, e);
            }
            catch (WebException e)
            {
                return new OperationResult<T>(req.RequestUri, e);
            }
        }

        /// <summary>
        /// Request with arguments(web form style).<para />
        /// (use POST/x-www-form-urlencoded)
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <param name="req">request</param>
        /// <param name="dict">send arguments dictionary</param>
        /// <param name="encode">encoding</param>
        /// <param name="streamconv">stream converter</param>
        /// <param name="responseconv">response converter</param>
        /// <returns>OperationResult</returns>
        public static OperationResult<T> WebFormSendString<T>(
            HttpWebRequest req,
            IEnumerable<KeyValuePair<string, string>> dict,
            Encoding encode,
            StreamConverter<T> streamconv = null,
            ResponseConverter<T> responseconv = null)
        {
            var para = from k in dict
                       select k.Key + "=" + k.Value;

            var dat = encode.GetBytes(String.Join("&", para.ToArray()));
            req.Method = "POST"; // static
            req.ContentType = "application/x-www-form-urlencoded";
            return WebConnect(
                req,
                streamconv,
                responseconv,
                dat);
        }

        /// <summary>
        /// Request with .<para />
        /// (use POST/x-www-form-urlencoded)
        /// </summary>
        /// <typeparam name="T">return type</typeparam>
        /// <param name="req">request</param>
        /// <param name="sends">sending datas</param>
        /// <param name="encode">encoding</param>
        /// <param name="streamconv">stream converter</param>
        /// <param name="responseconv">response converter</param>
        /// <returns>OperationResult</returns>
        public static OperationResult<T> WebUpload<T>(
            HttpWebRequest req,
            IEnumerable<SendData> sends,
            Encoding encode,
            StreamConverter<T> streamconv = null,
            ResponseConverter<T> responseconv = null)
        {
            try
            {
                // boundary
                string boundary = Guid.NewGuid().ToString("N");
                string separator = "--" + boundary + "\r\n";

                req.Method = "POST";
                req.ContentType = "multipart/form-data; boundary=" + boundary;

                byte[] endsep = encode.GetBytes("\r\n--" + boundary + "--\r\n");
                long gross = 0;
                foreach (var s in sends)
                {
                    gross += s.GetDataLength(boundary, encode);
                }
                gross += endsep.Length;

                req.ContentLength = gross;
                using (var rs = req.GetRequestStream())
                {
                    foreach (var s in sends)
                    {
                        foreach (var i in s.EnumerateByte(boundary, encode))
                        {
                            rs.Write(i, 0, i.Length);
                        }
                    }
                }
                return TreatWebResponse((HttpWebResponse)req.GetResponse(), streamconv, responseconv);
            }
            catch (SocketException e)
            {
                return new OperationResult<T>(req.RequestUri, e);
            }
            catch (IOException e)
            {
                return new OperationResult<T>(req.RequestUri, e);
            }
            catch (WebException e)
            {
                return new OperationResult<T>(req.RequestUri, e);
            }
        }


        /// <summary>
        /// Parse web response
        /// </summary>
        private static OperationResult<T> TreatWebResponse<T>
            (HttpWebResponse res, StreamConverter<T> strconv, ResponseConverter<T> resconv)
        {
            if (!(strconv == null ^ resconv == null))
                throw new ArgumentException("StreamConverter or ResponseConverter is must set.");
            try
            {
                switch (res.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Accepted:
                    case HttpStatusCode.Created:
                    case HttpStatusCode.NoContent:
                    case HttpStatusCode.ResetContent:
                    case HttpStatusCode.PartialContent:
                        if (resconv != null)
                        {
                            return new OperationResult<T>(
                                res.ResponseUri,
                                res.StatusCode,
                                resconv(res));
                        }
                        else if (strconv != null)
                        {
                            using (var s = res.GetResponseStream())
                            {
                                return new OperationResult<T>(
                                    res.ResponseUri,
                                    res.StatusCode,
                                    strconv(s));
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException("StreamConverter or ResponseConverter is must set.");
                        }

                    default:
                        return new OperationResult<T>(
                            res.ResponseUri,
                            null,
                            res.StatusCode);
                }
            }
            catch (Exception e)
            {
                return new OperationResult<T>(
                    res.ResponseUri, e);
            }
        }

        /// <summary>
        /// Connect specified uri and download string
        /// </summary>
        /// <param name="req">request parameter</param>
        public static OperationResult<string> WebConnectDownloadString(
            HttpWebRequest req)
        {
            return WebConnect(
                req,
                new StreamConverter<string>(StreamConverters.ReadString));
        }

        /// <summary>
        /// Connect specified uri and download string
        /// </summary>
        public static OperationResult<string> WebConnectDownloadString(
            Uri uri, string method = "GET", ICredentials credential = null)
        {
            return WebConnectDownloadString(
                CreateRequest(uri, method, credential: credential));
        }

    }

    /// <summary>
    /// Sending data descriptor structure
    /// </summary>
    public struct SendData
    {
        private string name;

        /// <summary>
        /// Field name
        /// </summary>
        public string Name
        {
            get { return name; }
            set
            {
                if (value == name) return;
                name = value;
                cache = null;
            }
        }

        private string text;

        /// <summary>
        /// Argument text
        /// </summary>
        public string Text
        {
            get { return text; }
            set
            {
                if(value == null)
                    throw new ArgumentNullException();
                if (value == text) return;
                text = value;
                fpath = null;
                cache = null;
            }
        }

        private string fpath;

        /// <summary>
        /// Argument file's path
        /// </summary>
        public string FilePath
        {
            get { return fpath; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException();
                if (value == fpath) return;
                fpath = value;
                text = null;
                cache = null;
            }
        }

        /// <summary>
        /// Create text item
        /// </summary>
        /// <param name="name">field name</param>
        /// <param name="value">argument value</param>
        public SendData(string name, string value)
            : this()
        {
            this.Name = name;
            this.Text = value;
        }

        /// <summary>
        /// Create data item<para/>
        /// You must set text property or file property.
        /// </summary>
        /// <param name="name">field name</param>
        /// <param name="text">argument text</param>
        /// <param name="file">argument file</param>
        public SendData(string name, string text = null, string file = null)
            : this(name, text)
        {
            if (!(text == null ^ file == null))
                throw new ArgumentException("Specified value is not setted or setted excess.");

            this.FilePath = file;
        }

        private Encoding cacheenc;
        private string cacheboundary;
        private byte[] cache;
        /// <summary>
        /// Get byte data length
        /// </summary>
        public long GetDataLength(string boundary, Encoding encode)
        {
            UpdateCache(boundary, encode);
            if (text != null)
            {
                return cache.Length;
            }
            else if (fpath != null)
            {
                if (!File.Exists(fpath))
                    throw new FileNotFoundException("File not found.", fpath);
                using (var fs =
                    new FileStream(
                        fpath, FileMode.Open, FileAccess.Read))
                {
                    return fs.Length + cache.Length;
                }
            }
            else
            {
                throw new InvalidOperationException("Internal arguments are null reference.");
            }
        }

        /// <summary>
        /// Enumerate bytes array
        /// </summary>
        public IEnumerable<byte[]> EnumerateByte(string boundary, Encoding encode)
        {
            UpdateCache(boundary, encode);
            if (text != null)
            {
                yield return cache;
            }
            else if (fpath != null)
            {
                yield return cache;
                using (var fs = new FileStream(fpath, FileMode.Open, FileAccess.Read))
                {
                    byte[] rdata = new byte[0x1000];
                    int rsize = 0;
                    for (; ; )
                    {
                        rsize = fs.Read(rdata, 0, rsize);
                        if (rsize <= 0) break;
                        yield return rdata;
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Internal arguments are null reference.");
            }
        }

        /// <summary>
        /// Update internal cache
        /// </summary>
        private void UpdateCache(string boundary, Encoding encode)
        {
            if (cache == null || cacheenc != encode || cacheboundary != boundary)
            {
                var separator = "--" + boundary + "\r\n";
                StringBuilder sb = new StringBuilder();
                //generate byte cache
                if (text != null)
                {
                    sb.Append(separator +
                        "Content-Disposition: form-data; name=\"" +
                        this.name +
                        "\"\r\n\r\n");
                    sb.Append(this.text + "\r\n");
                }
                else if (fpath != null)
                {
                    sb.Append(separator +
                            "Content-Disposition: form-data; name=\"" +
                            this.name + 
                            "\"; filename=\"" +
                            Path.GetFileName(this.fpath) +
                            "\"\r\n");
                    sb.Append("Content-Type: application/octet-stream\r\n");
                    sb.Append("Content-Transfer-Encoding: binary\r\n\r\n");
                }
                else
                {
                    throw new InvalidOperationException("Internal arguments are null reference.");
                }
                cache = encode.GetBytes(sb.ToString());
                cacheboundary = boundary;
                cacheenc = encode;
            }
        }
    }

}