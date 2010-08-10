using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Std.Network.Xml;

namespace Std.Tweak.Streaming
{
    /// <summary>
    /// Streaming element
    /// </summary>
    public class TwitterStreamingElement
    {
        /// <summary>
        /// Purse xml and create element
        /// </summary>
        public static TwitterStreamingElement CreateByNode(XElement node)
        {
            return new TwitterStreamingElement(node);
        }

        /// <summary>
        /// constructor
        /// </summary>
        protected TwitterStreamingElement(XElement node)
        {
            Kind = ElementKind.Undefined;
            RawXElement = node;
            var eventstr = node.Element("event").ParseString();
            if (String.IsNullOrWhiteSpace(eventstr))
            {
                // Status, Delete, UserEnumerations, or Undefined.
                if (node.Element("delete") != null)
                {
                    // delete
                    ParseDelete(node);
                }
                else if (node.Element("text") != null && node.Element("user") != null)
                {
                    // status
                    ParseStatus(node);
                }
                else if (node.Element("friends") != null)
                {
                    // user enumerations
                    ParseUserEnumerations(node);
                }
                else
                {
                    // undefined
                    ParseUndefined(node);
                }
            }
            else
            {
                // Follow, Favorite, Retweet, ListMemberAdded, or Undefined.
                switch (eventstr)
                {
                    case "follow":
                        // follow
                        ParseFollow(node);
                        break;
                    case "favorite":
                        // favorite
                        ParseFavorite(node);
                        break;
                    case "unfavorite":
                        // unfavorite
                        ParseUnfavorite(node);
                        break;
                    case "retweet":
                        ParseRetweet(node);
                        break;
                    case "list_member_added":
                        // list member added
                        ParseListMemberAdded(node);
                        break;
                    default:
                        ParseUndefined(node);
                        break;
                }
            }
        }

        private void ParseUndefined(XElement node)
        {
            Kind = ElementKind.Undefined;
        }

        #region Implicit switch

        private void ParseDelete(XElement node)
        {
            Kind = ElementKind.Delete;
            DeletedStatusId = node.Element("id").ParseLong();
        }

        private void ParseStatus(XElement node)
        {
            Kind = ElementKind.Status;
            Status = TwitterStatus.CreateByNode(node);
        }

        private void ParseUserEnumerations(XElement node)
        {
            Kind = ElementKind.UserEnumerations;
            UserEnumerations = from item in node.Elements("item")
                               select item.ParseLong();
        }

        #endregion

        #region Explicit switch

        private void ParseFollow(XElement node)
        {
            Kind = ElementKind.Follow;
            ParseSourceDest(node);
        }

        private void ParseFavorite(XElement node)
        {
            Kind = ElementKind.Favorite;
            ParseSourceDest(node);
            ParseTargetStatus(node);
        }

        private void ParseUnfavorite(XElement node)
        {
            Kind = ElementKind.Unfavorite;
            ParseSourceDest(node);
            ParseTargetStatus(node);
        }


        private void ParseRetweet(XElement node)
        {
            Kind = ElementKind.Retweet;
            ParseSourceDest(node);
            ParseTargetStatus(node);
        }

        private void ParseListMemberAdded(XElement node)
        {
            Kind = ElementKind.ListMemberAdded;
            ParseSourceDest(node);
            ParseTargetList(node);
        }

        private void ParseSourceDest(XElement node)
        {
            var source = node.Element("source");
            var target = node.Element("target");
            if (source == null || target == null)
                ParseUndefined(node);
            SourceUser = TwitterUser.CreateByNode(source);
            TargetUser = TwitterUser.CreateByNode(target);
        }

        private void ParseTargetStatus(XElement node)
        {
            var to = node.Element("target_object");
            if (to == null)
                return;
            Status = TwitterStatus.CreateByNode(to);
        }

        private void ParseTargetList(XElement node)
        {
            var to = node.Element("target_object");
            if (to == null)
                return;
            TargetList = TwitterList.CreateByNode(to);
        }

        #endregion

        /// <summary>
        /// Element kind
        /// </summary>
        public enum ElementKind
        {
            /// <summary>
            /// Status
            /// </summary>
            Status,
            /// <summary>
            /// Delete operation
            /// </summary>
            Delete,
            /// <summary>
            /// User enumerations
            /// </summary>
            UserEnumerations,
            /// <summary>
            /// Following
            /// </summary>
            Follow,
            /// <summary>
            /// Favorite
            /// </summary>
            Favorite,
            /// <summary>
            /// Unfavorite
            /// </summary>
            Unfavorite,
            /// <summary>
            /// Retweet
            /// </summary>
            Retweet,
            /// <summary>
            /// List member added
            /// </summary>
            ListMemberAdded,
            /// <summary>
            /// Undefined
            /// </summary>
            Undefined
        }

        /// <summary>
        /// Raw XElement object
        /// </summary>
        public XElement RawXElement { get; private set; }

        /// <summary>
        /// Kind of this element
        /// </summary>
        public ElementKind Kind { get; private set; }

        /// <summary>
        /// Status ID (uses notifing deleted status)
        /// </summary>
        public long DeletedStatusId { get; private set; }

        /// <summary>
        /// Status instance
        /// </summary>
        public TwitterStatus Status { get; private set; }

        /// <summary>
        /// User enumerations
        /// </summary>
        public IEnumerable<long> UserEnumerations { get; private set; }

        /// <summary>
        /// Source user
        /// </summary>
        public TwitterUser SourceUser { get; private set; }

        /// <summary>
        /// Target user
        /// </summary>
        public TwitterUser TargetUser { get; private set; }

        /// <summary>
        /// Target list
        /// </summary>
        public TwitterList TargetList { get; private set; }
    }
}
