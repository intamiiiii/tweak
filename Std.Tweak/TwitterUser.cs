﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Drawing;
using Std.Network.Xml;

namespace Std.Tweak
{
    /// <summary>
    /// Twitter user data
    /// </summary>
    public class TwitterUser
    {
        /// <summary>
        /// Get user id from node
        /// </summary>
        public static string GetUserIDByNode(XElement uNode)
        {
            if (uNode == null || uNode.Element("screen_name") == null)
                return null;
            return uNode.Element("screen_name").ParseString();
        }
        
        /// <summary>
        /// Create user data from node
        /// </summary>
        /// <param name="uNode">XElement node</param>
        /// <returns>User instance</returns>
        public static TwitterUser CreateByNode(XElement uNode)
        {
            //稀にIDだけが取得できて、screen_nameが取得できないことがある
            if (GetUserIDByNode(uNode) == null)
                return null;
            return new TwitterUser(uNode);
        }

        /// <summary>
        /// Create twitter user data from search node
        /// </summary>
        public static TwitterUser CreateBySearchNode(XElement sNode)
        {
            var ret = new TwitterUser();
            ret.Id = sNode.Element("from_user_id").ParseLong();
            ret.ScreenName = sNode.Element("from_user").ParseString();
            ret.Lang = sNode.Element("iso_language_code").ParseString();
            ret.Protected = false;
            ret.Profile = new ProfileInformation();
            return ret;
        }

        /// <summary>
        /// A class for twitter user data.<para/>
        /// Use CreateByNode(XElement), if you want to create instance with analying xml.
        /// </summary>
        public TwitterUser() : base() { }

        /// <summary>
        /// Create twitter user class with XElements
        /// </summary>
        /// <param name="uNode"></param>
        protected TwitterUser(XElement uNode)
        {
            this.Id = uNode.Element("id").ParseLong();

            this.Name = uNode.Element("name").ParseString();

            this.ScreenName = uNode.Element("screen_name").ParseString();

            this.Location = uNode.Element("location").ParseString();

            this.Description = uNode.Element("description").ParseString();

            this.ProfileImageUrl = uNode.Element("profile_image_url").ParseUri();

            this.Url = uNode.Element("url").ParseString();

            this.Protected = uNode.Element("protected").ParseBool(true);

            this.FriendsCount = uNode.Element("friends_count").ParseLong();

            this.FollowersCount = uNode.Element("followers_count").ParseLong();

            this.FriendsCount = uNode.Element("friends_count").ParseLong();

            this.CreatedAt = uNode.Element("created_at").ParseDateTime("ddd MMM d HH':'mm':'ss zzz yyyy");

            this.FavouritesCount = uNode.Element("favourites_count").ParseLong();

            this.UtcOffset = uNode.Element("utc_offset").ParseLong();

            this.TimeZone = uNode.Element("time_zone").ParseString();

            this.Notifications = uNode.Element("notifications").ParseBool();

            this.Verified = uNode.Element("verified").ParseBool();

            this.Following = uNode.Element("following").ParseBool();

            this.StatusesCount = uNode.Element("statuses_count").ParseLong();

            this.Lang = uNode.Element("lang").ParseString();

            this.ContributorsEnabled = uNode.Element("contributors_enabled").ParseBool();

            this.ListedCount = uNode.Element("listed_count").ParseLong();

            if (uNode.Element("status") != null)
            {
                this.LastTweeted = uNode.Element("status").Element("created_at").ParseDateTime("ddd MMM d HH':'mm':'ss zzz yyyy");
                this.LastTweetText = uNode.Element("status").Element("text").ParseString();
            }

            this.Profile = new ProfileInformation()
            {
                BackgroundColor = uNode.Element("profile_background_color").ParseColor(),
                TextColor = uNode.Element("profile_text_color").ParseColor(),
                LinkColor = uNode.Element("profile_link_color").ParseColor(),
                SidebarFillColor = uNode.Element("profile_sidebar_fill_color").ParseColor(),
                SidebarBorderColor = uNode.Element("profile_sidebar_border_color").ParseColor(),
                BackgroundImageUrl = uNode.Element("profile_background_image_url").ParseUri(),
                BackgroundTile = uNode.Element("profile_background_tile").ParseBool()
            };
        }

        /// <summary>
        /// User id<para/>
        /// If you get user information via search api, this value is different of<para/>
        /// other apis(ex: statuses, users, etc).<para/>
        /// You can use ScreenName as general ID.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// User name<para/>
        /// this is NOT identification name(@...), but profile name.<para/>
        /// if you want identification name, please see ScreenName.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User screen name<para/>
        /// Identification name is this.
        /// </summary>
        public string ScreenName { get; set; }

        /// <summary>
        /// User location
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// User description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// User profile_image_url
        /// </summary>
        public Uri ProfileImageUrl { get; set; }

        /// <summary>
        /// User url
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Is protected this user
        /// </summary>
        public bool Protected { get; set; }

        /// <summary>
        /// Followers count
        /// </summary>
        public long FollowersCount { get; set; }

        /// <summary>
        /// Friends count
        /// </summary>
        public long FriendsCount { get; set; }

        /// <summary>
        /// Created at of this count
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Favourites count
        /// </summary>
        public long FavouritesCount { get; set; }

        /// <summary>
        /// Listed count
        /// </summary>
        public long ListedCount { get; set; }

        /// <summary>
        /// Statuses count
        /// </summary>
        public long StatusesCount { get; set; }

        /// <summary>
        /// User utc offset
        /// </summary>
        public long UtcOffset { get; set; }

        /// <summary>
        /// User time zone
        /// </summary>
        public string TimeZone { get; set; }

        /// <summary>
        /// notifications property(?)
        /// </summary>
        [Obsolete("Use friendships/show API")]
        public bool Notifications { get; set; }

        /// <summary>
        /// Is geometry information enabled
        /// </summary>
        public bool GeoEnabled { get; set; }

        /// <summary>
        /// Is user is verified from offical
        /// </summary>
        public bool Verified { get; set; }

        /// <summary>
        /// Are you following this user
        /// </summary>
        [Obsolete("Use friendships/exists API")]
        public bool Following { get; set; }

        /// <summary>
        /// User's language
        /// </summary>
        public string Lang { get; set; }

        /// <summary>
        /// contributors_enabled property(?)
        /// </summary>
        public bool ContributorsEnabled { get; set; }

        /// <summary>
        /// Profile information
        /// </summary>
        public ProfileInformation Profile { get; set; }

        /// <summary>
        /// User profile information structure
        /// </summary>
        public struct ProfileInformation
        {
            /// <summary>
            /// User's background color
            /// </summary>
            public Color BackgroundColor { get; set; }

            /// <summary>
            /// User's text color
            /// </summary>
            public Color TextColor { get; set; }

            /// <summary>
            /// User's link color
            /// </summary>
            public Color LinkColor { get; set; }

            /// <summary>
            /// User's sidebar fill color
            /// </summary>
            public Color SidebarFillColor { get; set; }

            /// <summary>
            /// User's sidebar border color
            /// </summary>
            public Color SidebarBorderColor { get; set; }

            /// <summary>
            /// User's background image url
            /// </summary>
            public Uri BackgroundImageUrl { get; set; }

            /// <summary>
            /// 
            /// </summary>
            public bool BackgroundTile { get; set; }
        }

        /// <summary>
        /// Get user's name
        /// </summary>
        public override string ToString()
        {
            return "@" + ScreenName + "(" + Name + ")";
        }

        /// <summary>
        /// Check equalness with Id
        /// </summary>
        public override bool Equals(object obj)
        {
            var usr = obj as TwitterUser;
            if (usr == null)
                return false;
            return usr.Id == this.Id;
        }

        /// <summary>
        /// Equals Id property.
        /// </summary>
        public override int GetHashCode()
        {
            return (int)this.Id;
        }

        /// <summary>
        /// Common object holder
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// Last tweeted DateTime
        /// </summary>
        public DateTime LastTweeted { get; set; }

        /// <summary>
        /// Last tweeted text
        /// </summary>
        public string LastTweetText { get; set; }
    }
}
