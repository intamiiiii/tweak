using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Std.Network;
using System.Net;

namespace Std.Tweak.CredentialProviders
{
    // code base from xrekkusu(xrex)
    /// <summary>
    /// xAuth provider
    /// </summary>
    /// <remarks>
    /// In order to get access to this method, you must apply by sending an email to api@twitter.com<para/>
    /// — all other applications will receive an HTTP 401 error.<para/>
    /// Web-based applications will not be granted access, except on a temporary basis for when they are converting from basic-authentication support to full OAuth support.
    /// </remarks>
    public abstract class xAuth : OAuth
    {
        // Key values
        const string XAuthUsername = "x_auth_username";
        const string XAuthPassword = "x_auth_password";
        const string XAuthMode = "x_auth_mode";

        /// <summary>
        /// Uri for get access token from xAuth provider
        /// </summary>
        protected abstract string XAuthProviderAccessTokenUrl { get; }

        /// <summary>
        /// Get access token via xAuth credential
        /// </summary>
        /// <param name="userName">raw user name</param>
        /// <param name="password">raw password</param>
        /// <returns>succeed to get credential information</returns>
        /// <remarks>
        /// Got token and secret is containered in this instance.
        /// </remarks>
        public bool GetAccessTokenViaXAuth(string userName, string password)
        {

            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            para.Add(new KeyValuePair<string, string>(XAuthUsername, userName));
            para.Add(new KeyValuePair<string, string>(XAuthPassword, UrlEncode(password, Encoding.Default, true)));
            para.Add(new KeyValuePair<string, string>(XAuthMode, "client_auth"));

            var target = CreateUrl(XAuthProviderAccessTokenUrl, RequestMethod.POST, para);
            try
            {
                var ret = Http.WebConnectDownloadString(new Uri(target), "POST", null);
                if (ret.Exception != null)
                    throw ret.Exception;
                if (!ret.Succeeded)
                {
                    return false;
                }
                var rd = SplitParamDict(ret.Data);
                if (rd.ContainsKey("oauth_token") && rd.ContainsKey("oauth_token_secret"))
                {
                    Token = rd["oauth_token"];
                    Secret = rd["oauth_token_secret"];
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (WebException)
            {
                throw;
            }
        }
    }
}
