using System;
using System.Collections.Generic;
using System.Net;
using System.Xml;
using System.Xml.Linq;
using Std.Network;
using System.IO;

namespace Std.Tweak
{
    /// <summary>
    /// Provides credential information for accessing Twitter.
    /// </summary>
    public abstract class CredentialProvider
    {
        /// <summary>
        /// API rate limit descriptor
        /// </summary>
        public int RateLimitMax { get; protected set; }

        /// <summary>
        /// API rate limit remaining
        /// </summary>
        public int RateLimitRemaining { get; protected set; }

        /// <summary>
        /// API rate limit reset date-time
        /// </summary>
        public DateTime RateLimitReset { get; protected set; }

        /// <summary>
        /// Request methods
        /// </summary>
        public enum RequestMethod
        {
            /// <summary>
            /// GET request
            /// </summary>
            GET,

            /// <summary>
            /// POST request
            /// </summary>
            POST
        };

        /// <summary>
        /// Request API
        /// </summary>
        /// <param name="uri">target uri</param>
        /// <param name="method">target method</param>
        /// <param name="param">parameters</param>
        /// <returns>XML document</returns>
        public abstract XDocument RequestAPI(string uri, RequestMethod method, IEnumerable<KeyValuePair<string,string>> param);

        /// <summary>
        /// Request API, returns stream
        /// </summary>
        /// <param name="uri">target uri(full)</param>
        /// <param name="method">requesting method</param>
        /// <param name="param">parameters</param>
        /// <returns>Connection stream</returns>
        public abstract Stream RequestStreamingAPI(string uri, RequestMethod method, IEnumerable<KeyValuePair<string, string>> param);

        /// <summary>
        /// XDocument generator
        /// </summary>
        /// <param name="res">WebResponse</param>
        /// <returns>XML Document</returns>
        protected XDocument XDocumentGenerator(WebResponse res)
        {
            return XDocumentGenerator(res, XmlReader.Create);
        }

        /// <summary>
        /// XDocument generator
        /// </summary>
        /// <param name="res">WebResponse</param>
        /// <param name="converter">Input converter</param>
        /// <returns>XML Document</returns>
        protected XDocument XDocumentGenerator(WebResponse res, Func<Stream, XmlReader> converter)
        {
            //read api rate
            int rateLimit;
            if (int.TryParse(res.Headers["X-RateLimit-Limit"], out rateLimit))
                RateLimitMax = rateLimit;

            int rateLimitRemaining;
            if (int.TryParse(res.Headers["X-RateLimit-Remaining"], out rateLimitRemaining))
                RateLimitRemaining = rateLimitRemaining;

            long rateLimitReset;
            if (long.TryParse(res.Headers["X-RateLimit-Reset"], out rateLimitReset))
                RateLimitReset = UnixEpoch.GetDateTimeByUnixEpoch(rateLimitReset);

            //Create xml document
            XDocument xDoc = new XDocument();
            try
            {
                using (var s = res.GetResponseStream())
                {
                    xDoc = XDocument.Load(converter(s));
                }
            }
            catch (XmlException xe)
            {
                throw new Exceptions.TwitterXmlParseException(xe);
            }
            catch (IOException ioe)
            {
                throw new Exceptions.TwitterException("Document read failed.", ioe);
            }
            catch (Exception)
            {
                throw;
            }
            return xDoc;
        }
    }
}
