using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Std.Tweak
{
    /// <summary>
    /// Twitter search api implementation
    /// </summary>
    public static class SearchApi
    {
        #region URI formatters

        /// <summary>
        /// Twitter api uri (v1)
        /// </summary>
        private static string TwitterSearchUri { get { return "http://search.twitter.com/"; } }

        /// <summary>
        /// Formatting target uri and request api
        /// </summary>
        /// <remarks>
        /// For twitter api version 1
        /// </remarks>
        private static XDocument RequestSearchAPI(this CredentialProvider provider, string partial, CredentialProvider.RequestMethod method, IEnumerable<KeyValuePair<string, string>> param)
        {
            var target = TwitterSearchUri + (partial.EndsWith("/") ? partial.Substring(1) : partial);
            return provider.RequestAPI(target, method, param);
        }

        #endregion

        /// <summary>
        /// Get stauses via search API.
        /// </summary>
        /// <remarks>
        /// This api is special and you can confusing this api's behavior.<para/>
        /// please see Twitter API Documentation and understand how to use.
        /// </remarks>
        /// <param name="provider">credential provider</param>
        /// <param name="query">query string</param>
        /// <param name="phrase">phrase option</param>
        /// <param name="lang">language</param>
        /// <param name="rpp">results per page</param>
        /// <param name="page">page</param>
        /// <param name="sinceId">since id</param>
        /// <param name="geocode">geometrics code</param>
        /// <returns>results</returns>
        public static IEnumerable<TwitterStatus> Search(
            this CredentialProvider provider,
            string query = null, string phrase = null, string lang = null,
            int rpp = 0, int page = 0, long sinceId = 0, string geocode = null)
        {
            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>();
            if (query == null && phrase == null)
                throw new ArgumentException("You must set query or phrase.");
            if (query != null)
                args.Add(new KeyValuePair<string, string>("q", query));
            else if (phrase != null)
                args.Add(new KeyValuePair<string, string>("phrase", phrase));
            if (lang != null)
                args.Add(new KeyValuePair<string, string>("lang", lang));
            if (rpp > 0)
                args.Add(new KeyValuePair<string, string>("rpp", rpp.ToString()));
            if (page > 0)
                args.Add(new KeyValuePair<string, string>("page", page.ToString()));
            if (sinceId > 0)
                args.Add(new KeyValuePair<string, string>("since_id", sinceId.ToString()));
            var nodes = provider.RequestSearchAPI("search.json", CredentialProvider.RequestMethod.GET, args).Elements("results");
            if (nodes != null)
            {
                foreach (var node in nodes)
                    yield return TwitterStatus.CreateBySearchNode(node);
            }
        }
    }
}
