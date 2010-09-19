using System;
using System.Xml.Linq;
using Std.Network.Xml;

namespace Std.Tweak
{
    /// <summary>
    /// Twitter status class
    /// </summary>
    public class TwitterStatus : TwitterStatusBase
    {
        /// <summary>
        /// Create twitter status from XML node
        /// </summary>
        public static TwitterStatus CreateByNode(XElement sNode)
        {
            return new TwitterStatus(sNode);
        }

        /// <summary>
        /// Create twitter status from search api node
        /// </summary>
        public static TwitterStatus CreateBySearchNode(XElement sNode)
        {
            var ts = new TwitterStatus();
            ts.Id = sNode.Element("id").ParseLong();
            ts.Truncated = false;
            ts.Text = sNode.Element("text").ParseString();
            ts.Source = sNode.Element("source").ParseString();
            ts.Favorited = false;
            ts.CreatedAt = sNode.Element("created_at").ParseDateTime("ddd MMM d HH':'mm':'ss zzz yyyy");
            ts.InReplyToStatusId = 0;
            ts.InReplyToUserId = sNode.Element("to_user_id").ParseLong();
            ts.InReplyToScreenName = sNode.Element("to_user").ParseString();
            ts.Kind = StatusKind.SearchResult;
            ts.User = TwitterUser.CreateBySearchNode(sNode);
            return ts;
        }

        private TwitterStatus() { }

        /// <summary>
        /// Twitter status constructor with XML data
        /// </summary>
        /// <param name="node"></param>
        protected TwitterStatus(XElement node)
            : base()
        {
            this.Id = node.Element("id").ParseLong();

            this.Truncated = node.Element("truncated").ParseBool();

            this.Text = node.Element("text").ParseString();

            this.Source = node.Element("source").ParseString();

            this.Favorited = node.Element("favorited").ParseBool();

            this.Retweeted = node.Element("retweeted").ParseBool();

            this.CreatedAt = node.Element("created_at").ParseDateTime("ddd MMM d HH':'mm':'ss zzz yyyy");

            this.InReplyToStatusId = node.Element("in_reply_to_status_id").ParseLong();

            this.InReplyToUserId = node.Element("in_reply_to_user_id").ParseLong();

            this.InReplyToScreenName = node.Element("in_reply_to_screen_name").ParseString();

            if (node.Element("retweeted_status") != null)
            {
                this.Kind = StatusKind.Retweeted;
                this.RetweetedOriginal = TwitterStatus.CreateByNode(node.Element("retweeted_status"));
            }
            else
            {
                this.Kind = StatusKind.Normal;
            }

            this.User = TwitterUser.CreateByNode(node.Element("user"));
        }

        /// <summary>
        /// Truncated status
        /// </summary>
        public bool Truncated { get; set; }

        /// <summary>
        /// Favorited this by authenticated user
        /// </summary>
        public bool Favorited { get; set; }

        /// <summary>
        /// Retweeted this by authenticated user
        /// </summary>
        public bool Retweeted { get; set; }

        /// <summary>
        /// Source param
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Status id which mentioned this
        /// </summary>
        public long InReplyToStatusId { get; set; }

        /// <summary>
        /// User id who mentioned this
        /// </summary>
        public long InReplyToUserId { get; set; }

        /// <summary>
        /// User screen name who mentioned this
        /// </summary>
        public string InReplyToScreenName { get; set; }

        /// <summary>
        /// Original status in this if this tweet is official-retweet some tweet.
        /// </summary>
        public TwitterStatus RetweetedOriginal { get; set; }

        /// <summary>
        /// Check published as retweet
        /// </summary>
        public bool PublishedByRetweet
        {
            get { return RetweetedOriginal != null; }
        }
    }
}
