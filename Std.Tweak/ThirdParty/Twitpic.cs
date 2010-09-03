using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Std.Network;
using Std.Network.Xml;

namespace Std.Tweak.ThirdParty
{
    /// <summary>
    /// TwitPic API Implementation
    /// </summary>
    public static class TwitPicApi
    {
        static string UploadApiUrl = "http://api.twitpic.com/2/upload.xml";

        /// <summary>
        /// Upload picture to TwitPic
        /// </summary>
        public static bool UploadToTwitpic(this CredentialProviders.OAuth provider, string apiKey, string message, string mediaFilePath, out string url)
        {
            url  = null;
            var doc = provider.UploadToTwitpic(apiKey, message, mediaFilePath);
            if (doc == null || doc.Element("image") == null)
            {
                return false;
            }
            else
            {
                url = doc.Element("image").Element("url").ParseString();
                return true;
            }
        }

        /// <summary>
        /// Upload picture to TwitPic
        /// </summary>
        public static XDocument UploadToTwitpic(this CredentialProviders.OAuth provider, string apiKey, string message, string mediaFilePath)
        {
            var req = HttpWeb.CreateRequest(new Uri(UploadApiUrl), "POST");

            // use OAuth Echo
            provider.MakeOAuthEchoRequest(ref req);

            List<SendData> sd = new List<SendData>();
            sd.Add(new SendData("key", apiKey));
            sd.Add(new SendData("message", message));
            sd.Add(new SendData("media", file: mediaFilePath));

            var doc = HttpWeb.WebUpload<XDocument>(req, sd, Encoding.UTF8, (s) => XDocument.Load(s));
            if (doc.Exception != null)
                throw doc.Exception;
            if (doc.Succeeded == false)
                return null;
            return doc.Data;
        }

    }
}
