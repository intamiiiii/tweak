using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Std.Network.Xml;

namespace Std.Tweak
{
    /// <summary>
    /// Twitter list data class
    /// </summary>
    public class TwitterList
    {
        /// <summary>
        /// List id
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// List name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// List full-name
        /// </summary>
        public string FullName { get; set; }

        /// <summary>
        /// List slug
        /// </summary>
        public string Slug { get; set; }

        /// <summary>
        /// List description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// List subscriber count
        /// </summary>
        public long SubscriberCount { get; set; }

        /// <summary>
        /// List member count
        /// </summary>
        public long MemberCount { get; set; }

        /// <summary>
        /// List partial uri
        /// </summary>
        public string PartialUri { get; set; }

        /// <summary>
        /// List open mode
        /// </summary>
        public string Mode { get; set; }

        /// <summary>
        /// Is private this list
        /// </summary>
        public bool Private
        {
            get { return this.Mode == "private"; }
        }

        /// <summary>
        /// Parent user
        /// </summary>
        public TwitterUser User { get; set; }

        /// <summary>
        /// Members enumerator
        /// </summary>
        public TwitterUser[] Members { get; set; }

        /// <summary>
        /// Twitter list instance from XML nodes
        /// </summary>
        /// <param name="lNode">source</param>
        /// <returns>instance</returns>
        public static TwitterList CreateByNode(XElement lNode)
        {
            return new TwitterList(lNode);
        }

        /// <summary>
        /// Twitter list instance
        /// </summary>
        public TwitterList() { }

        /// <summary>
        /// Create list information from xml node
        /// </summary>
        protected TwitterList(XElement node)
        {
            this.Id = node.Element("id").ParseLong();

            this.Name = node.Element("name").ParseString();

            this.FullName = node.Element("full_name").ParseString();

            this.Slug = node.Element("slug").ParseString();

            this.Description = node.Element("description").ParseString();

            this.SubscriberCount = node.Element("subscriber_count").ParseLong();

            this.MemberCount = node.Element("member_count").ParseLong();

            this.PartialUri = node.Element("uri").ParseString();

            this.Mode = node.Element("mode").ParseString();

            this.User = TwitterUser.CreateByNode(node.Element("user"));
        }

        /// <summary>
        /// Set users collection into this list
        /// </summary>
        /// <param name="members">users collection</param>
        public void SetUsers(IEnumerable<TwitterUser> members)
        {
            this.Members = members.ToArray();
        }

        /// <summary>
        /// Get list id
        /// </summary>
        public static string GetListId(XElement node)
        {
            if (node == null ||  node.Element("id") == null)
                return null;
            return node.Element("id").Value;
        }

        /// <summary>
        /// Get list fullname
        /// </summary>
        public override string ToString()
        {
            return this.FullName;
        }

        /// <summary>
        /// Compare by ID.
        /// </summary>
        public override bool Equals(object obj)
        {
            var list = obj as TwitterList;
            if (list == null)
                return false;
            else
                return list.Id == this.Id;
        }

        /// <summary>
        /// Equals id of myself
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return (int)this.Id;
        }

        /// <summary>
        /// Common object holder
        /// </summary>
        public object Tag { get; set; }
    }
}
