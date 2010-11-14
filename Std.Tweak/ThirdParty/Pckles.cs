using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using Std.Network;
using Std.Network.Xml;

namespace Std.Tweak.ThirdParty
{
    /// <summary>
    /// Pckles API Implementation<para />
    /// Requires configure to allow twitter relation before use this implementation.
    /// </summary>
    public static class Pckles
    {
        static string UploadApiUrl = "http://pckles.com/upload.api";

        /// <summary>
        /// Upload image to pckles
        /// </summary>
        public static bool UploadToPckles(this CredentialProviders.OAuth provider, string apiKey, string message,
            string mediaFilePath, out string url)
        {
            url = null;
            var doc = provider.UploadToPckles(apiKey, message, mediaFilePath);
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
        /// Upload image to pckles and get full informations
        /// </summary>
        public static XDocument UploadToPckles(this CredentialProviders.OAuth provider, string apiKey, string message,
            string mediaFilePath)
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
    }
}
