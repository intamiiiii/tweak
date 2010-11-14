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
        static string MediaShowUrl = "http://api.twitpic.com/2/media/show.xml?id={0}";
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
            var req = HttpWeb.CreateRequest(new Uri(UploadApiUrl), "POST", contentType: "application/x-www-form-urlencoded");

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

        /// <summary>
        /// Get detail XML of image data
        /// </summary>
        /// <param name="id">picture id</param>
        /// <returns>XML document</returns>
        public static XDocument GetDetail(string id)
        {
            var dat = HttpWeb.WebConnect<XDocument>(
                HttpWeb.CreateRequest(new Uri(String.Format(MediaShowUrl, id)), contentType: "application/x-www-form-urlencoded"),
                HttpWeb.StreamConverters.ReadXml);
            if (dat.Exception != null)
                throw dat.Exception;
            else if (!dat.Succeeded)
                return null;
            else
                return dat.Data;
        }
    }
}
