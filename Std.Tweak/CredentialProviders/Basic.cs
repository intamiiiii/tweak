using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Std.Network;
using System.Net;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;

namespace Std.Tweak.CredentialProviders
{
    /// <summary>
    /// Basic credential
    /// </summary>
    /// <remarks>
    /// We STRONGLY recommended to use OAuth (or XAuth) alternate this.
    /// </remarks>
    [Obsolete("Basic authentication will unable by 2010/8/31.")]
    public class Basic : CredentialProvider
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public Basic() : this(null, null) { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="userName">username</param>
        /// <param name="password">password</param>
        public Basic(string userName, string password)
        {
            this.UserName = userName;
            this.Password = password;
        }

        /// <summary>
        /// User name
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// Pass word
        /// </summary>
        public string Password { get; set; }

        private enum DocumentTypes { Invalid, Xml, Json };

        /// <summary>
        /// Request API
        /// </summary>
        public sealed override System.Xml.Linq.XDocument RequestAPI(string uri, CredentialProvider.RequestMethod method, IEnumerable<KeyValuePair<string, string>> param)
        {
            if (String.IsNullOrEmpty(uri))
                throw new ArgumentNullException(uri);
            else if (uri.Length < 5)
                throw new ArgumentException("uri is too short.");

            DocumentTypes docType = DocumentTypes.Invalid;

            if (uri.EndsWith("xml"))
                docType = DocumentTypes.Xml;
            else if (uri.EndsWith("json"))
                docType = DocumentTypes.Json;
            else
                throw new ArgumentException("format can't identify. uriPartial is must ends with xml or json.");

            var target = CreateUri(uri, param);

            try
            {
                System.Diagnostics.Debug.WriteLine(target.ToString());
                var req = Http.CreateRequest(new Uri(target.ToString()), true);
                req.Credentials = new System.Net.NetworkCredential(UserName, Password);
                var ret = Http.WebConnect<XDocument>(
                    req,
                    method.ToString(), req.Credentials,
                    new Http.DStreamCallbackFull<XDocument>((res) =>
                    {
                        int rateLimit;
                        if (int.TryParse(res.Headers["X-RateLimit-Limit"], out rateLimit))
                        {
                            this.RateLimitMax = rateLimit;
                        }
                        int rateLimitRemaining;
                        if (int.TryParse(res.Headers["X-RateLimit-Remaining"], out rateLimitRemaining))
                        {
                            this.RateLimitRemaining = rateLimitRemaining;
                        }
                        long rateLimitReset;
                        if (long.TryParse(res.Headers["X-RateLimit-Reset"], out rateLimitReset))
                        {
                            this.RateLimitReset = UnixEpoch.GetDateTimeByUnixEpoch(rateLimitReset);
                        }

                        switch (docType)
                        {
                            case DocumentTypes.Xml:
                                return XDocumentGenerator(res);
                            case DocumentTypes.Json:
                                return XDocumentGenerator(res, (s) => JsonReaderWriterFactory.CreateJsonReader(s, XmlDictionaryReaderQuotas.Max));
                            default:
                                throw new NotSupportedException("Invalid format.");
                        }
                    }));
                if (ret.Succeeded && ret.Data != null)
                {
                    return ret.Data;
                }
                else
                {
                    if (ret.Exception != null)
                        throw ret.Exception;
                    else
                        throw new WebException(ret.Message);
                }
            }
            catch (WebException we)
            {
                System.Diagnostics.Debug.WriteLine(we.ToString());
            }
            catch (XmlException xe)
            {
                throw new Exceptions.TwitterXmlParseException(xe);
            }
            catch (IOException)
            {
                throw;
            }

            return null;
        }

        /// <summary>
        /// Request API
        /// </summary>
        public override Stream RequestStreamingAPI(string uri, CredentialProvider.RequestMethod method, IEnumerable<KeyValuePair<string, string>> param)
        {
            if (String.IsNullOrEmpty(uri))
                throw new ArgumentNullException(uri);
            else if (uri.Length < 5)
                throw new ArgumentException("uri is too short.");

            var target = CreateUri(uri, param);

            try
            {
                System.Diagnostics.Debug.WriteLine(target.ToString());
                var req = Http.CreateRequest(new Uri(target.ToString()), true);
                req.Credentials = new System.Net.NetworkCredential(UserName, Password);
                req.ReadWriteTimeout = System.Threading.Timeout.Infinite;
                req.Timeout = System.Threading.Timeout.Infinite;
                var ret = Http.WebConnectStreaming(
                    req,
                    method.ToString());
                System.Diagnostics.Debug.WriteLine("Connected");
               if (ret.Succeeded && ret.Data != null)
                {
                    return ret.Data;
                }
                else
                {
                    if (ret.Data != null)
                        ret.Data.Close();
                    if (ret.Exception != null)
                        throw ret.Exception;
                    else
                        throw new WebException(ret.Message);
                }
            }
            catch (WebException we)
            {
                System.Diagnostics.Debug.WriteLine(we.ToString());
            }
            catch (XmlException xe)
            {
                throw new Exceptions.TwitterXmlParseException(xe);
            }
            catch (IOException)
            {
                throw;
            }

            return null;
        }

        private string CreateUri(string uri, IEnumerable<KeyValuePair<string, string>> param)
        {
            if (param == null)
                return uri;
            var pstr =  String.Join("&", from p in param select p.Key + "=" + p.Value);
            if (String.IsNullOrEmpty(pstr))
                return uri;
            else
                return uri + "?" + pstr;
        }
    }
}