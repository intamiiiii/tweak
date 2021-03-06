﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Std.Tweak
{
    /// <summary>
    /// Abstracted twitter status data class
    /// </summary>
    public abstract class TwitterStatusBase
    {
        /// <summary>
        /// Get status id from XML node
        /// </summary>
        public static string GetStatusId(XElement node)
        {
            if (node == null || node.Element("id") == null)
                return null;
            return node.Element("id").Value;
        }

        /// <summary>
        /// Status id
        /// </summary>
        public virtual long Id { get; set; }

        /// <summary>
        /// Status body text
        /// </summary>
        public virtual string Text { get; set; }

        /// <summary>
        /// Tweeted user
        /// </summary>
        public virtual TwitterUser User { get; set; }

        /// <summary>
        /// Created time
        /// </summary>
        public virtual DateTime CreatedAt { get; set; }

        /// <summary>
        /// Status kind enumerate
        /// </summary>
        public enum StatusKind {
            /// <summary>
            /// Normal
            /// </summary>
            Normal, 
            /// <summary>
            /// This tweet official-retweeted other tweet.
            /// </summary>
            Retweeted,
            /// <summary>
            /// This tweet is kind of direct message.
            /// </summary>
            DirectMessage,
            /// <summary>
            /// This tweet got from search API, so few informations is sometimes not correct.
            /// </summary>
            SearchResult
        }

        /// <summary>
        /// Status kind
        /// </summary>
        public virtual StatusKind Kind { get; set; }

        /// <summary>
        /// Common object holder
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Show formatted status
        /// </summary>
        public override string ToString()
        {
            return String.Format("{0}:{1}", User.ToString(), Text);
        }

        /// <summary>
        /// Check equalness with Id
        /// </summary>
        public override bool Equals(object obj)
        {
            var status = obj as TwitterStatusBase;
            if (status == null)
                return false;
            return status.Id == this.Id;
        }

        /// <summary>
        /// Equals Id property
        /// </summary>
        public override int GetHashCode()
        {
            return (int)this.Id;
        }
    }
}
