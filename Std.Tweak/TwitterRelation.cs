using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Std.Tweak
{
    /// <summary>
    /// Relation descriptor class
    /// </summary>
    public sealed class TwitterRelation
    {
        /// <summary>
        /// Create relation information from xml node
        /// </summary>
        public static TwitterRelation CreateByNode(XElement rNode)
        {
            return new TwitterRelation(rNode);
        }

        private TwitterRelation(XElement elem)
        {
            Target = RelationInfo.CreateByNode(elem.Element("target"));
        }

        /// <summary>
        /// Relation information
        /// </summary>
        public class RelationInfo
        {
            /// <summary>
            /// Create relation information from xml node
            /// </summary>
            public static RelationInfo CreateByNode(XElement elem)
            {
                return new RelationInfo(elem);
            }

            /// <summary>
            /// Create relation information from xml node
            /// </summary>
            protected RelationInfo(XElement elem)
            {
                this.ScreenName = elem.Element("screen_name").ParseString();

                this.UserId = elem.Element("id").ParseLong();

                this.FollowedBy = elem.Element("followed_by").ParseBool();

                this.Following = elem.Element("following").ParseBool();
            }

            /// <summary>
            /// Screen name
            /// </summary>
            public string ScreenName { get; private set; }

            /// <summary>
            /// User id
            /// </summary>
            public long UserId { get; private set; }

            /// <summary>
            /// Follow by other one
            /// </summary>
            public bool FollowedBy { get; private set; }

            /// <summary>
            /// Following other one
            /// </summary>
            public bool Following { get; private set; }
        }

        /// <summary>
        /// Extended relation information
        /// </summary>
        public sealed class RelationInfoEx : RelationInfo
        {
            /// <summary>
            /// Create relation information from xml node
            /// </summary>
            public static new RelationInfoEx CreateByNode(XElement rxNode)
            {
                return new RelationInfoEx(rxNode);
            }

            /// <summary>
            /// Create relation information from xml node
            /// </summary>
            private RelationInfoEx(XElement node)
                : base(node)
            {
                NotificationsEnabled = node.Element("notifications_enabled").ParseBool();

                Blocking = node.Element("blocking").ParseBool();
            }

            /// <summary>
            /// Enable notification of other one's activity
            /// </summary>
            public bool NotificationsEnabled { get; set; }

            /// <summary>
            /// Blocking other one
            /// </summary>
            public bool Blocking { get; set; }
        }

        /// <summary>
        /// Target relation information
        /// </summary>
        public RelationInfo Target { get; private set; }

        /// <summary>
        /// Source relation information
        /// </summary>
        public RelationInfoEx Source { get; private set; }
    }
}
