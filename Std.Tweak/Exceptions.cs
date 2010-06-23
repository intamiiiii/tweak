using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Std.Tweak.Exceptions
{
    [Serializable]
    public class TwitterException : System.Net.WebException
    {
        /// <summary>
        /// Twitter common exception
        /// </summary>
        public TwitterException() { }

        /// <summary>
        /// Twitter common exception
        /// </summary>
        public TwitterException(string message) : base(message) { }

        /// <summary>
        /// Twitter common exception
        /// </summary>
        public TwitterException(string message, Exception inner) : base(message, inner) { }

        protected TwitterException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// Twitter xml analyze error
    /// </summary>
    [Serializable]
    public class TwitterXmlParseException : TwitterException
    {
        /// <summary>
        /// XML Parse error
        /// </summary>
        /// <param name="detail">description</param>
        public TwitterXmlParseException(string detail) : base("Twitter xml analyzing error:" + detail) { }

        /// <summary>
        /// XML Parse error
        /// </summary>
        /// <param name="excp">internal exception</param>
        public TwitterXmlParseException(Exception excp) : base("Twitter xml analyzing error:" + excp.Message, excp) { }

        /// <summary>
        /// XML Parse error
        /// </summary>
        /// <param name="xobj">bad xml object</param>
        public TwitterXmlParseException(XObject xobj) : base("Twitter xml analyzing error at:" + xobj == null || xobj.Document == null ? "(NULL object)" : xobj.Document.ToString()) { }

        protected TwitterXmlParseException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// Twitter api request error
    /// </summary>
    [Serializable]
    public class TwitterRequestException : TwitterException
    {
        /// <summary>
        /// Twitter requesting error
        /// </summary>
        /// <param name="detail">description</param>
        public TwitterRequestException(string detail) : base("Twitter api request error:" + detail) { }

        /// <summary>
        /// Twitter requesting error
        /// </summary>
        /// <param name="excp">internal exception</param>
        public TwitterRequestException(Exception excp) : base("Twitter api request error:" + excp.Message, excp) { }

        /// <summary>
        /// Twitter requesting error
        /// </summary>
        /// <param name="xobj">bad xml object</param>
        public TwitterRequestException(XObject xobj) : base("Twitter api request error (XML Error at:" + xobj == null || xobj.Document == null ? "(NULL object)" : xobj.Document.ToString() + ")") { }

        protected TwitterRequestException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    /// <summary>
    /// Twitter oauth authentication error
    /// </summary>
    [Serializable]
    public class TwitterOAuthRequestException : TwitterException
    {
        /// <summary>
        /// OAuth request exception
        /// </summary>
        /// <param name="detail">description</param>
        public TwitterOAuthRequestException(string detail) : base("Twitter OAuth request error:" + detail) { }

        protected TwitterOAuthRequestException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
