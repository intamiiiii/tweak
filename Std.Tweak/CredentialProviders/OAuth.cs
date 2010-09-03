using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Std.Network;
using System.Web;
using System.Runtime.Serialization.Json;

namespace Std.Tweak.CredentialProviders
{
    static class OAuthSigTypeResolver
    {
        public static string GetString(this OAuth.OAuthSigType sig)
        {
            switch (sig)
            {
                case OAuth.OAuthSigType.Hmac_Sha1:
                    return HMACSHA1;
                case OAuth.OAuthSigType.PlainText:
                    return PlainText;
                case OAuth.OAuthSigType.Rsa_Sha1:
                    return RSASHA1;
                default:
                    return null;
            }
        }

        const string HMACSHA1 = "HMAC-SHA1";
        const string PlainText = "PLAINTEXT";
        const string RSASHA1 = "RSA-SHA1";

    }

    /// <summary>
    /// OAuth abstracted class
    /// </summary>
    /// <remarks>
    /// You MUST get api token and secret for your application.
    /// </remarks>
    public abstract class OAuth : CredentialProvider
    {
        /// <summary>
        /// OAuth credential provider
        /// </summary>
        public OAuth() : this(null, null) { }

        /// <summary>
        /// OAuth credential provider
        /// </summary>
        /// <param name="token">user token</param>
        /// <param name="secret">user secret</param>
        public OAuth(string token, string secret)
        {
            this.Token = token;
            this.Secret = secret;
        }

        private enum DocumentTypes { Invalid, Xml, Json };

        /// <summary>
        /// RequestAPI implementation
        /// </summary>
        /// <param name="uri">full uri</param>
        /// <param name="method">using method for HTTP negotiation</param>
        /// <param name="param">additional parameters</param>
        /// <returns>XML documents</returns>
        public sealed override XDocument RequestAPI(string uri, CredentialProvider.RequestMethod method, IEnumerable<KeyValuePair<string,string>> param)
        {
            // validate arguments
            if (String.IsNullOrEmpty(uri))
                throw new ArgumentNullException(uri);
            else if (uri.Length < 5)
                throw new ArgumentException("uri is too short.");
            string target = uri;

            // detection return type of api
            DocumentTypes docType = DocumentTypes.Invalid;

            if (target.EndsWith("xml"))
                docType = DocumentTypes.Xml;
            else if (target.EndsWith("json"))
                docType = DocumentTypes.Json;
            else
                throw new ArgumentException("format can't identify. uriPartial is must ends with .xml or .json.");

            // pre-validation authentication
            if(String.IsNullOrEmpty(Token) || String.IsNullOrEmpty(Secret))
            {
                throw new Exceptions.TwitterOAuthRequestException("OAuth is not validated.");
            }

            // generate OAuth uri
            var authuri = CreateUrl(target, method, param);
            try
            {
                var ret = HttpWeb.WebConnect<XDocument>(
                    HttpWeb.CreateRequest(new Uri(authuri), method.ToString()),
                    responseconv: new HttpWeb.ResponseConverter<XDocument>((res) =>
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
                throw new Exceptions.TwitterRequestException(we);
            }
            catch (XmlException xe)
            {
                throw new Exceptions.TwitterXmlParseException(xe);
            }
            catch (IOException)
            {
                throw;
            }
        }

        /// <summary>
        /// RequestStreamAPI implementation
        /// </summary>
        /// <param name="uri">full uri</param>
        /// <param name="method">using method for HTTP negotiation</param>
        /// <param name="param">parameters</param>
        /// <returns>Callback stream</returns>
        public sealed override Stream RequestStreamingAPI(string uri, CredentialProvider.RequestMethod method, IEnumerable<KeyValuePair<string, string>> param)
        {
            if (String.IsNullOrEmpty(uri))
                throw new ArgumentNullException(uri);
            else if (uri.Length < 5)
                throw new ArgumentException("uri is too short.");
            string target = uri;

            if (String.IsNullOrEmpty(Token) || String.IsNullOrEmpty(Secret))
            {
                throw new Exceptions.TwitterOAuthRequestException("OAuth is not validated.");
            }

            var authuri = CreateUrl(target, method, param);
            try
            {
                var req = HttpWeb.CreateRequest(new Uri(authuri), method.ToString());
                // timeout is 5 seconds

                req.Timeout = 5000;
                var ret = HttpWeb.WebConnect<Stream>(req: req, responseconv: HttpWeb.ResponseConverters.GetStream);
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
                throw new Exceptions.TwitterRequestException(we);
            }
            catch (IOException)
            {
                throw;
            }
        }

        /// <summary>
        /// Send query with OAuth credentials<para />
        /// (Use for OAuth Echo)
        /// </summary>
        public void MakeOAuthEchoRequest(ref HttpWebRequest request, IEnumerable<KeyValuePair<string, string>> param = null, string providerUri = ProviderEchoAuthorizeUrl)
        {
            // pre-validation authentication
            if (String.IsNullOrEmpty(Token) || String.IsNullOrEmpty(Secret))
            {
                throw new Exceptions.TwitterOAuthRequestException("OAuth is not validated.");
            }

            request.Headers["X-Auth-Service-Provider"] = providerUri;
            var header = GetAuthorizationHeaderText(providerUri, RequestMethod.GET, param);
            request.Headers["X-Verify-Credentials-Authorization"] = "OAuth realm=\"http://api.twitter.com/\"," + header;
        }

        #region OAuth property

        /// <summary>
        /// Consumer key
        /// </summary>
        protected abstract string ConsumerKey { get; }

        /// <summary>
        /// Consumer secret
        /// </summary>
        protected abstract string ConsumerSecret { get; }

        const string ProviderRequestTokenUrl = "http://twitter.com/oauth/request_token";
        const string ProviderAccessTokenUrl = "http://twitter.com/oauth/access_token";
        const string ProviderAuthorizeUrl = "http://twitter.com/oauth/authorize";
        const string ProviderEchoAuthorizeUrl = "https://api.twitter.com/1/account/verify_credentials.json";

        const OAuthSigType SignatureType = OAuthSigType.Hmac_Sha1;

        #endregion

        #region OAuth reserved values
        /// <summary>
        /// OAuth signature type
        /// </summary>
        public enum OAuthSigType
        {
            /// <summary>
            /// HMAC SHA1
            /// </summary>
            Hmac_Sha1,
            /// <summary>
            /// Plain text
            /// </summary>
            PlainText,
            /// <summary>
            /// RSA SHA1
            /// </summary>
            Rsa_Sha1
        }


        const string Version = "1.0";
        const string ParamPrefix = "oauth_";

        #region Key values
        const string ConsumerKeyKey = "oauth_consumer_key";
        const string CallbackKey = "oauth_callback";
        const string VersionKey = "oauth_version";
        const string SignatureMethodKey = "oauth_signature_method";
        const string SignatureKey = "oauth_signature";
        const string TimestampKey = "oauth_timestamp";
        const string NonceKey = "oauth_nonce";
        const string TokenKey = "oauth_token";
        const string TokenSecretKey = "oauth_token_secret";
        const string VerifierKey = "oauth_verifier";
        #endregion

        const string AllowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";
        #endregion

        #region OAuth system

        #region Property

        /// <summary>
        /// OAuth token
        /// </summary>
        public string Token { get; set; }

        /// <summary>
        /// OAuth secret
        /// </summary>
        public string Secret { get; set; }

        #endregion

        private string GetRequestToken()
        {
            try
            {
                var target = CreateUrl(ProviderRequestTokenUrl, RequestMethod.POST, null);
                var ret = HttpWeb.WebConnectDownloadString(new Uri(target), "POST", null);
                if (ret.Exception != null)
                    throw ret.Exception;
                if (!ret.Succeeded)
                    throw new Exception(ret.Message);
                var query = SplitParam(ret.Data);
                foreach (var q in query)
                {
                    if (q.Key == "oauth_token")
                    {
                        return q.Value;
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
            return null;
        }

        private string GetProviderAuthUrl(string token)
        {
            return ProviderAuthorizeUrl + "?oauth_token=" + token;
        }

        /// <summary>
        /// Get provider's authorization url
        /// </summary>
        /// <param name="reqToken">request token string</param>
        /// <returns>access uri</returns>
        public Uri GetProviderAuthUrl(out string reqToken)
        {
            reqToken = GetRequestToken();
            return new Uri(GetProviderAuthUrl(reqToken));
        }

        /// <summary>
        /// Get access token from request token
        /// </summary>
        /// <param name="token">request token</param>
        /// <param name="pin">personal identify code</param>
        /// <param name="userId">user id</param>
        /// <returns>succeed authorization</returns>
        public bool GetAccessToken(string token, string pin, out string userId)
        {
            
            //Generate param
            string paramName = TokenKey + "=";
            int idx = token.IndexOf(paramName);

            if (idx > 0)
                Token = token.Substring(idx + paramName.Length);
            else
                Token = token;

            var target = CreateUrl(ProviderAccessTokenUrl, RequestMethod.GET, null, pin);
            try
            {
                var ret = HttpWeb.WebConnectDownloadString(new Uri(target), "GET", null);
                if (ret.Exception != null)
                    throw ret.Exception;
                if (!ret.Succeeded)
                {
                    userId = null;
                    return false;
                }
                var rd = SplitParamDict(ret.Data);
                if (rd.ContainsKey("oauth_token") && rd.ContainsKey("oauth_token_secret"))
                {
                    Token = rd["oauth_token"];
                    Secret = rd["oauth_token_secret"];
                    userId = rd["screen_name"];
                    return true;
                }
                else
                {
                    userId = null;
                    return false;
                }
            }
            catch (WebException)
            {
                throw;
            }
        }

        #region Negotiation method

        /// <summary>
        /// Create request uri
        /// </summary>
        protected string CreateUrl(string uri, RequestMethod method, IEnumerable<KeyValuePair<string, string>> param, string pin = null)
        {
            param = AddOAuthParams(
                param,
                ConsumerKey, Token,
                GetTimestamp(), GetNonce(),
                SignatureType, pin);
            string strp = JoinParam(param);
            string sig = GetSignature(
                new Uri(uri),
                ConsumerSecret, Secret,
                strp, SignatureType, method.ToString());

            // Re-generate parameters with signature
            List<KeyValuePair<string, string>> np = new List<KeyValuePair<string, string>>();
            if (param != null)
                np.AddRange(param);
            np.Add(new KeyValuePair<string, string>(SignatureKey, sig));
            return uri + "?" + JoinParam(np);
        }

        /// <summary>
        /// Create authorization header text
        /// </summary>
        protected string GetAuthorizationHeaderText(string uri, RequestMethod method, IEnumerable<KeyValuePair<string, string>> param, string pin = null)
        {
            string nonce = GetNonce();
            string timestamp = GetTimestamp();
            param = AddOAuthParams(
                param,
                ConsumerKey, Token,
                timestamp, nonce,
                SignatureType, pin);
            string strp = JoinParam(param);
            string sig = GetSignature(
                new Uri(uri),
                ConsumerSecret, Secret,
                strp, SignatureType, method.ToString());

            // generate authorization header
            List<KeyValuePair<string, string>> dict = new List<KeyValuePair<string, string>>();
            dict.Add(new KeyValuePair<string, string>(VersionKey, Version));
            dict.Add(new KeyValuePair<string, string>(NonceKey, nonce));
            dict.Add(new KeyValuePair<string, string>(TimestampKey, timestamp));
            dict.Add(new KeyValuePair<string, string>(SignatureMethodKey, SignatureType.GetString()));
            dict.Add(new KeyValuePair<string, string>(ConsumerKeyKey, ConsumerKey));
            dict.Add(new KeyValuePair<string, string>(SignatureKey, sig));
            if (!String.IsNullOrEmpty(Token))
                dict.Add(new KeyValuePair<string, string>(TokenKey, Token));

            // concatenate parameters
            var concat = from d in dict
                         orderby d.Key
                         select d.Key + "=\"" + d.Value + "\"";
            return String.Join(",", concat);
        }

        /// <summary>
        /// Url encoding some string
        /// </summary>
        /// <param name="value">target</param>
        /// <param name="encoding">using encode</param>
        /// <param name="upper">helix cast to upper</param>
        /// <returns>encoded string</returns>
        public static string UrlEncode(string value, Encoding encoding, bool upper)
        {
            StringBuilder result = new StringBuilder();
            byte[] data = encoding.GetBytes(value);
            int len = data.Length;

            for (int i = 0; i < len; i++)
            {
                int c = data[i];
                if (c < 0x80 && AllowedChars.IndexOf((char)c) != -1)
                {
                    result.Append((char)c);
                }
                else
                {
                    if (upper)
                        result.Append('%' + String.Format("{0:X2}", (int)data[i]));
                    else
                        result.Append('%' + String.Format("{0:x2}", (int)data[i]));
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// Split parameters into dictionary
        /// </summary>
        protected Dictionary<string, string> SplitParamDict(string param)
        {
            var retdict = new Dictionary<string, string>();
            foreach (var p in SplitParam(param))
            {
                if (retdict.ContainsKey(p.Key))
                    throw new InvalidOperationException();
                retdict.Add(p.Key, p.Value);
            }
            return retdict;
        }

        /// <summary>
        /// Split parameters and enumerate results
        /// </summary>
        protected IEnumerable<KeyValuePair<string, string>> SplitParam(string paramstring)
        {
            paramstring.TrimStart('?');
            if (String.IsNullOrEmpty(paramstring))
                yield break;
            var parray = paramstring.Split(new[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var s in parray)
            {
                int idx = -1;
                if ((idx = s.IndexOf('=')) >= 0)
                {
                    yield return new KeyValuePair<string, string>(
                        s.Substring(0, idx), s.Substring(idx + 1));
                }
                else
                {
                    yield return new KeyValuePair<string, string>(s, String.Empty);
                }
            }
        }

        /// <summary>
        /// Join parameters to string
        /// </summary>
        protected string JoinParam(IEnumerable<KeyValuePair<string, string>> param)
        {
            var jparam = from p in param
                         orderby p.Key
                         select p.Key + "=" + p.Value;
            return String.Join("&", jparam.ToArray());
        }

        #endregion

        #region Common method

        private IEnumerable<KeyValuePair<string, string>> AddOAuthParams(IEnumerable<KeyValuePair<string, string>> origParam,
            string consumerKey, string token,
            string timeStamp, string nonce, OAuthSigType sigType, string verifier)
        {
            if (String.IsNullOrEmpty(consumerKey))
                throw new ArgumentNullException("consumerKey");
            var np = new List<KeyValuePair<string, string>>();
            // original parameter
            if (origParam != null)
                np.AddRange(origParam);
            np.Add(new KeyValuePair<string, string>(VersionKey, Version));
            np.Add(new KeyValuePair<string, string>(NonceKey, nonce));
            np.Add(new KeyValuePair<string, string>(TimestampKey, timeStamp));
            np.Add(new KeyValuePair<string, string>(SignatureMethodKey, sigType.GetString()));
            np.Add(new KeyValuePair<string, string>(ConsumerKeyKey, consumerKey));
            if (!String.IsNullOrEmpty(verifier))
                np.Add(new KeyValuePair<string, string>(VerifierKey, verifier));
            if (!String.IsNullOrEmpty(token))
                np.Add(new KeyValuePair<string, string>(TokenKey, token));
            return np;
        }

        private string GetSignature(
            Uri uri, string consumerSecret, string tokenSecret,
            string joinedParam, OAuthSigType sigType,
            string requestMethod)
        {
            switch (sigType)
            {
                case OAuthSigType.PlainText:
                    return HttpUtility.UrlEncode(consumerSecret + "&" + tokenSecret);
                case OAuthSigType.Hmac_Sha1:
                    if (String.IsNullOrEmpty(requestMethod))
                        throw new ArgumentNullException("httpMethod");

                    //URLのフォーマット
                    var regularUrl = uri.Scheme + "://" + uri.Host;
                    if (!((uri.Scheme == "http" && uri.Port == 80) || (uri.Scheme == "https" && uri.Port == 443)))
                        regularUrl += ":" + uri.Port;
                    regularUrl += uri.AbsolutePath;
                    //シグネチャの生成

                    StringBuilder SigSource = new StringBuilder();
                    SigSource.Append(UrlEncode(requestMethod.ToUpper(), Encoding.UTF8, true) + "&");
                    SigSource.Append(UrlEncode(regularUrl, Encoding.UTF8, true) + "&");
                    SigSource.Append(UrlEncode(joinedParam, Encoding.UTF8, true));

                    //ハッシュの計算
                    using (HMACSHA1 hmacsha1 = new HMACSHA1())
                    {
                        hmacsha1.Key = Encoding.ASCII.GetBytes(string.Format("{0}&{1}", UrlEncode(consumerSecret, Encoding.UTF8, true), string.IsNullOrEmpty(tokenSecret) ? "" : UrlEncode(tokenSecret, Encoding.UTF8, true)));
                        return UrlEncode(ComputeHash(hmacsha1, SigSource.ToString()), Encoding.UTF8, false);
                    }
                case OAuthSigType.Rsa_Sha1:
                    throw new NotImplementedException();
                default:
                    throw new ArgumentException("Unknown signature type", "signatureType");
            }
        }

        private string ComputeHash(HashAlgorithm algorithm, string raw)
        {
            if (algorithm == null)
                throw new ArgumentNullException("algorithm");
            if (String.IsNullOrEmpty(raw))
                throw new ArgumentNullException("raw");
            byte[] dat = Encoding.ASCII.GetBytes(raw);
            byte[] hash = algorithm.ComputeHash(dat);
            return Convert.ToBase64String(hash);
        }

        private string GetNonce()
        {
            //Use guid
            return Guid.NewGuid().ToString("N");
        }

        private string GetTimestamp()
        {
            //Timestamp
            return UnixEpoch.GetUnixEpochByDateTime(DateTime.Now).ToString();
        }

        #endregion

        #endregion
    }
}