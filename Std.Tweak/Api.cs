using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Drawing;
using System.Net;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

namespace Std.Tweak
{
    /// <summary>
    /// Twitter API (version 1)
    /// </summary>
    public static class Api
    {
        #region URI formatters

        /// <summary>
        /// Twitter api uri (v1)
        /// </summary>
        private static string TwitterUri { get { return "http://api.twitter.com/1/"; } }

        /// <summary>
        /// Formatting target uri and request api
        /// </summary>
        /// <remarks>
        /// For twitter api version 1
        /// </remarks>
        private static XDocument RequestAPIv1(this CredentialProvider provider, string partial, CredentialProvider.RequestMethod method, IEnumerable<KeyValuePair<string, string>> param)
        {
            var target = TwitterUri + (partial.EndsWith("/") ? partial.Substring(1) : partial);
            return provider.RequestAPI(target, method, param);
        }

        #endregion

        #region Timelines

        /// <summary>
        /// Get twitter timeline
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="partialUri">partial uri</param>
        /// <param name="param">parameters</param>
        private static IEnumerable<TwitterStatus> GetTimeline(this CredentialProvider provider, string partialUri, IEnumerable<KeyValuePair<string, string>> param)
        {
            var doc = provider.RequestAPIv1(partialUri, CredentialProvider.RequestMethod.GET, param);
            if (doc == null)
                return null;
            List<TwitterStatus> statuses = new List<TwitterStatus>();
            HashSet<string> hashes = new HashSet<string>();
            return from n in doc.Descendants("status")
                   let s = TwitterStatus.CreateByNode(n)
                   where s != null
                   select s;
        }

        /// <summary>
        /// Get timeline with full parameters
        /// </summary>
        public static IEnumerable<TwitterStatus> GetTimeline(this CredentialProvider provider, string partialUri, long? sinceId, long? maxId, long? count, long? page, string userId, string screenName)
        {
            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            if (sinceId != null && sinceId.HasValue)
                para.Add(new KeyValuePair<string, string>("since_id", sinceId.Value.ToString()));

            if (maxId != null && maxId.HasValue)
                para.Add(new KeyValuePair<string, string>("max_id", maxId.Value.ToString()));

            if (count != null)
                para.Add(new KeyValuePair<string, string>("count", count.ToString()));

            if (page != null)
                para.Add(new KeyValuePair<string, string>("page", page.ToString()));

            if (!String.IsNullOrEmpty(userId))
                para.Add(new KeyValuePair<string, string>("user_id", userId.ToString()));

            if (!String.IsNullOrEmpty(screenName))
                para.Add(new KeyValuePair<string, string>("screen_name", screenName));

            return provider.GetTimeline(partialUri, para);
        }

        /// <summary>
        /// Get public timeline<para />
        /// This result will caching while 60 seconds in Twitter server.
        /// </summary>
        /// <param name="provider">credential provider</param>
        public static IEnumerable<TwitterStatus> GetPublicTimeline(this CredentialProvider provider)
        {
            return provider.GetTimeline("statuses/public_timeline.xml", null);
        }

        /// <summary>
        /// Get home timeline (it contains following users' tweets)
        /// </summary>
        /// <param name="provider">credential provider</param>
        public static IEnumerable<TwitterStatus> GetHomeTimeline(this CredentialProvider provider)
        {
            return provider.GetTimeline("statuses/home_timeline.xml", null);
        }

        /// <summary>
        /// Get home timeline with full params (it contains following users' tweets)
        /// </summary>
        public static IEnumerable<TwitterStatus> GetHomeTimeline(this CredentialProvider provider, long? sinceId, long? maxId, long? count, long? page)
        {
            return provider.GetTimeline("statuses/home_timeline.xml", sinceId, maxId, count, page, null, null);
        }

        /// <summary>
        /// Get mentions
        /// </summary>
        /// <param name="provider">credential provider</param>
        public static IEnumerable<TwitterStatus> GetMentions(this CredentialProvider provider)
        {
            return provider.GetTimeline("statuses/mentions.xml", null);
        }

        /// <summary>
        /// Get mentions with full params
        /// </summary>
        public static IEnumerable<TwitterStatus> GetMentions(this CredentialProvider provider, long? sinceId, long? maxId, long? count, long? page)
        {
            return provider.GetTimeline("statuses/mentions.xml", sinceId, maxId, count, page, null, null);
        }

        #endregion

        #region Status methods

        /// <summary>
        /// Get status
        /// </summary>
        private static TwitterStatus GetStatus(this CredentialProvider provider, string partialUriFormat, CredentialProvider.RequestMethod method, long id)
        {
            string partialUri = string.Format(partialUriFormat, id);
            var doc = provider.RequestAPIv1(partialUri, method, null);
            if (doc == null)
                return null;
            TwitterStatus s = TwitterStatus.CreateByNode(doc.Element("status"));
            if (s == null)
                throw new Exceptions.TwitterXmlParseException("status can't read.");
            return s;
        }

        /// <summary>
        /// Get status from id
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="id">user id</param>
        public static TwitterStatus GetStatus(this CredentialProvider provider, long id)
        {
            return provider.GetStatus("statuses/show/{0}.xml", CredentialProvider.RequestMethod.GET, id);
        }

        /// <summary>
        /// Update new status
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="body">body</param>
        /// <param name="inReplyToStatusId">tweet id which be replied this tweet</param>
        public static TwitterStatus UpdateStatus(this CredentialProvider provider, string body, long? inReplyToStatusId)
        {
            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            para.Add(new KeyValuePair<string, string>("status", Tweak.CredentialProviders.OAuth.UrlEncode(body, Encoding.UTF8, true)));
            if (inReplyToStatusId != null && inReplyToStatusId.HasValue)
            {
                para.Add(new KeyValuePair<string, string>("in_reply_to_status_id", inReplyToStatusId.Value.ToString()));
            }
            var doc = provider.RequestAPIv1("statuses/update.xml", CredentialProvider.RequestMethod.POST, para);
            if (doc != null)
                return TwitterStatus.CreateByNode(doc.Element("status"));
            else
                return null;
        }

        /// <summary>
        /// Delete your tweet
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="id">tweet id</param>
        public static TwitterStatus DestroyStatus(this CredentialProvider provider, long id)
        {
            return provider.GetStatus("statuses/destroy/{0}.xml", CredentialProvider.RequestMethod.POST, id);
        }

        #endregion

        #region Direct message methods

        /// <summary>
        /// Get direct messages
        /// </summary>
        private static IEnumerable<TwitterDirectMessage> GetDirectMessages(this CredentialProvider provider, string partialUri, IEnumerable<KeyValuePair<string, string>> param)
        {
            var doc = provider.RequestAPIv1(partialUri, CredentialProvider.RequestMethod.GET, param);
            if (doc == null)
                return null;
            List<TwitterStatus> statuses = new List<TwitterStatus>();
            HashSet<string> hashes = new HashSet<string>();
            return from n in doc.Descendants("direct_message")
                   let dm = TwitterDirectMessage.CreateByNode(n)
                   where dm != null
                   select dm;
        }

        /// <summary>
        /// Get direct messages with full params
        /// </summary>
        private static IEnumerable<TwitterDirectMessage> GetDirectMessages(this CredentialProvider provider, string partialUri, long? sinceId, long? maxId, long? count, long? page)
        {
            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            if (sinceId != null && sinceId.HasValue)
            {
                para.Add(new KeyValuePair<string, string>("since_id", sinceId.Value.ToString()));
            }
            if (maxId != null && maxId.HasValue)
            {
                para.Add(new KeyValuePair<string, string>("max_id", maxId.Value.ToString()));
            }
            if (count != null && count.HasValue)
            {
                para.Add(new KeyValuePair<string, string>("count", count.Value.ToString()));
            }
            if (page != null && page.HasValue)
            {
                para.Add(new KeyValuePair<string, string>("page", page.Value.ToString()));
            }

            return provider.GetDirectMessages(partialUri, para);
        }

        /// <summary>
        /// Get direct messages
        /// </summary>
        /// <param name="provider">credential provider</param>
        public static IEnumerable<TwitterDirectMessage> GetDirectMessages(this CredentialProvider provider)
        {
            return provider.GetDirectMessages("direct_messages.xml", null);
        }

        /// <summary>
        /// Get direct messages with full params
        /// </summary>
        public static IEnumerable<TwitterDirectMessage> GetDirectMessages(this CredentialProvider provider, long? sinceId, long? maxId, long? count, long? page)
        {
            return provider.GetDirectMessages("direct_messages.xml", sinceId, maxId, count, page);
        }

        /// <summary>
        /// Get direct messages you sent
        /// </summary>
        /// <param name="provider">credential provider</param>
        public static IEnumerable<TwitterDirectMessage> GetSentDirectMessages(this CredentialProvider provider)
        {
            return provider.GetDirectMessages("direct_messages/sent.xml", null);
        }

        /// <summary>
        /// Get direct messages you sent with full params
        /// </summary>
        public static IEnumerable<TwitterDirectMessage> GetSentDirectMessages(this CredentialProvider provider, long? sinceId, long? maxId, long? count, long? page)
        {
            return provider.GetDirectMessages("direct_messages/sent.xml", sinceId, maxId, count, page);
        }

        /// <summary>
        /// Send new direct message
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="user">target user id or name</param>
        /// <param name="text">send body</param>
        public static TwitterDirectMessage SendDirectMessage(this CredentialProvider provider, string user, string text)
        {
            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            para.Add(new KeyValuePair<string, string>("text", Tweak.CredentialProviders.OAuth.UrlEncode(text, Encoding.UTF8, true)));
            para.Add(new KeyValuePair<string, string>("user", user));

            var xmlDoc = provider.RequestAPIv1("direct_messages/new.xml", CredentialProvider.RequestMethod.POST, para);
            if (xmlDoc == null)
                return null;

            var sent = TwitterDirectMessage.CreateByNode(xmlDoc.Element("direct_message"));
            if (sent == null)
                throw new Exceptions.TwitterRequestException(xmlDoc);

            return sent;
        }

        /// <summary>
        /// Delete a direct message which you sent
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="id">destroy id</param>
        public static TwitterDirectMessage DestroyDirectMessage(this CredentialProvider provider, string id)
        {
            string partialUri = string.Format("direct_messages/destroy/{0}.xml", id);
            var xmlDoc = provider.RequestAPIv1(partialUri, CredentialProvider.RequestMethod.POST, null);
            if (xmlDoc == null)
                return null;
            var destroyed = TwitterDirectMessage.CreateByNode(xmlDoc.Element("direct_message"));
            if (destroyed == null)
                throw new Exceptions.TwitterRequestException(xmlDoc);
            return destroyed;
        }

        #endregion

        #region User methods

        /// <summary>
        /// Get user with full params
        /// </summary>
        private static TwitterUser GetUser(this CredentialProvider provider, string partialUri, CredentialProvider.RequestMethod method, string userId, string screenName)
        {
            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            if (userId != null)
            {
                para.Add(new KeyValuePair<string,string>("user_id", userId.ToString()));
            }
            if (screenName != null)
            {
                para.Add(new KeyValuePair<string,string>("screen_name", screenName));
            }
            var doc = provider.RequestAPIv1(partialUri, method, para);
            if (doc == null)
                return null;
            return TwitterUser.CreateByNode(doc.Element("user"));
        }

        /// <summary>
        /// Get user information
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static TwitterUser GetUser(this CredentialProvider provider, string userId)
        {
            return provider.GetUser("users/show.xml", CredentialProvider.RequestMethod.GET, userId, null);
        }

        /// <summary>
        /// Get user by screen name
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="screenName">target user screen name</param>
        public static TwitterUser GetUserByScreenName(this CredentialProvider provider, string screenName)
        {
            return provider.GetUser("users/show.xml", CredentialProvider.RequestMethod.GET, null, screenName);
        }

        /// <summary>
        /// Get users
        /// </summary>
        private static IEnumerable<TwitterUser> GetUsers(this CredentialProvider provider, string partialUri, IEnumerable<KeyValuePair<string, string>> para, out long prevCursor, out long nextCursor)
        {
            prevCursor = 0;
            nextCursor = 0;
            var doc = provider.RequestAPIv1(partialUri, CredentialProvider.RequestMethod.GET, para);
            if (doc == null)
                return null;
            List<TwitterUser> users = new List<TwitterUser>();
            var ul = doc.Element("users_list");
            if (ul != null)
            {
                var nc = ul.Element("next_cursor");
                if (nc != null)
                    nextCursor = (long)nc.ParseLong();
                var pc = ul.Element("previous_cursor");
                if (pc != null)
                    prevCursor = (long)pc.ParseLong();
            }
            return from n in doc.Descendants("user")
                   let usr = TwitterUser.CreateByNode(n)
                   where usr != null
                   select usr;
        }

        /// <summary>
        /// Get users with full params
        /// </summary>
        private static IEnumerable<TwitterUser> GetUsers(this CredentialProvider provider, string partialUri, string userId, string screenName, long? cursor, out long prevCursor, out long nextCursor)
        {
            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            if (userId != null)
            {
                para.Add(new KeyValuePair<string, string>("user_id", userId.ToString()));
            }
            if (screenName != null)
            {
                para.Add(new KeyValuePair<string, string>("screen_name", screenName));
            }
            if (cursor != null)
            {
                para.Add(new KeyValuePair<string, string>("cursor", cursor.ToString()));
            }
            return provider.GetUsers(partialUri, para, out prevCursor, out nextCursor);
        }

        /// <summary>
        /// Get users with use cursor params
        /// </summary>
        private static IEnumerable<TwitterUser> GetUsersAll(this CredentialProvider provider, string partialUri, string userId, string screenName)
        {
            long n_cursor = -1;
            long c_cursor = -1;
            long p;
            while (n_cursor != 0)
            {
                var users = provider.GetUsers(partialUri, userId, screenName, c_cursor, out p, out n_cursor);
                if (users != null)
                    foreach (var u in users)
                        yield return u;
                c_cursor = n_cursor;
            }
        }

        /// <summary>
        /// Get friends all
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterUser> GetFriendsAll(this CredentialProvider provider, string userId)
        {
            return provider.GetUsersAll("statuses/friends.xml", userId, null);
        }

        /// <summary>
        /// Get friends all
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="screenName">target user's screen name</param>
        public static IEnumerable<TwitterUser> GetFriendsAllByScreenName(this CredentialProvider provider, string screenName)
        {
            return provider.GetUsersAll("statuses/friends.xml", null, screenName);
        }


        /// <summary>
        /// Get friends with full params
        /// </summary>
        public static IEnumerable<TwitterUser> GetFriends(this CredentialProvider provider, string userId, string screenName, long? cursor, out long prevCursor, out long nextCursor)
        {
            if (cursor == null)
                cursor = -1;
            return provider.GetUsers("statuses/friends.xml", userId, screenName, cursor, out prevCursor, out nextCursor);
        }


        /// <summary>
        /// Get followers all
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterUser> GetFollowersAll(this CredentialProvider provider, string userId)
        {
            return provider.GetUsersAll("statuses/followers.xml", userId, null);
        }

        /// <summary>
        /// Get followers all
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="screenName">target user's screen name</param>
        public static IEnumerable<TwitterUser> GetFollowersAllByScreenName(this CredentialProvider provider, string screenName)
        {
            return provider.GetUsersAll("statuses/followers.xml", null, screenName);
        }

        /// <summary>
        /// Get followers with full params
        /// </summary>
        public static IEnumerable<TwitterUser> GetFollowers(this CredentialProvider provider, string userId, string screenName, long? page, out long prevCursor, out long nextCursor)
        {
            return provider.GetUsers("statuses/followers.xml", userId, screenName, page, out prevCursor, out nextCursor);
        }

        #endregion

        #region Favorite methods

        /// <summary>
        /// Favorites a tweet
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="id">the id of the tweet to favorite.</param>
        public static TwitterStatus CreateFavorites(this CredentialProvider provider, long id)
        {
            return provider.GetStatus("favorites/create/{0}.xml", CredentialProvider.RequestMethod.POST, id);
        }

        /// <summary>
        /// Unfavorites a tweet
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="id">the id of the tweet to unffavorite.</param>
        public static TwitterStatus DestroyFavorites(this CredentialProvider provider, long id)
        {
            return provider.GetStatus("favorites/destroy/{0}.xml", CredentialProvider.RequestMethod.POST, id);
        }

        #endregion

        #region Retweet methods
        
        //http://twitter.com/statuses/retweet
        /// <summary>
        /// Retweet status
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="id">status id</param>
        public static TwitterStatus Retweet(this CredentialProvider provider, long id)
        {
            return provider.GetStatus("statuses/retweet/{0}.xml", CredentialProvider.RequestMethod.POST, id);
        }

        #endregion

        #region List methods

        /// <summary>
        /// Get list statuses
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">list owner user id</param>
        /// <param name="listId">list id</param>
        public static IEnumerable<TwitterStatus> GetListStatuses(this CredentialProvider provider, string userId, string listId)
        {
            return provider.GetListStatuses(userId, listId, null, null, null, null);
        }

        /// <summary>
        /// Get list statuses with full params
        /// </summary>
        public static IEnumerable<TwitterStatus> GetListStatuses(this CredentialProvider provider, string userId, string listId, string sinceId, string maxId, long? perPage, long? page)
        {
            listId = listId.Replace("_", "-");
            var partialUri = userId + "/lists/" + listId + "/statuses.xml";

            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();

            if (!String.IsNullOrEmpty(sinceId))
                para.Add(new KeyValuePair<string, string>("since_id", sinceId.ToString()));

            if (!String.IsNullOrEmpty(maxId))
                para.Add(new KeyValuePair<string, string>("max_id", maxId.ToString()));

            if(perPage != null)
                para.Add(new KeyValuePair<string, string>("per_page", perPage.ToString()));

            if (page != null)
                para.Add(new KeyValuePair<string, string>("page", page.ToString()));

            return provider.GetTimeline(partialUri, para);
        }

        /// <summary>
        /// Get list members
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">list owner user's id</param>
        /// <param name="listId">list id</param>
        public static IEnumerable<TwitterUser> GetListMembersAll(this CredentialProvider provider, string userId, string listId)
        {
            long n_cursor = -1;
            long c_cursor = -1;
            long p;
            while (n_cursor != 0)
            {
                foreach (var m in provider.GetListMembers(userId, listId, c_cursor, out p, out n_cursor))
                    yield return m;
                c_cursor = n_cursor;
            }
        }

        /// <summary>
        /// Get list members with full params
        /// </summary>
        public static IEnumerable<TwitterUser> GetListMembers(this CredentialProvider provider, string userId, string listId, long? cursor, out long prevCursor, out long nextCursor)
        {
            listId = listId.Replace("_", "-");

            var partialUri = userId + "/" + listId + "/members.xml";

            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            if (cursor != null)
            {
                para.Add(new KeyValuePair<string, string>("cursor", cursor.ToString()));
            }

            prevCursor = -1;
            nextCursor = -1;

            var doc = provider.RequestAPIv1(partialUri, CredentialProvider.RequestMethod.GET, para);
            if (doc == null)
                return null;

            var ul = doc.Element("users_list");
            if (ul != null)
            {
                var nc = ul.Element("next_cursor");
                if (nc != null)
                    nextCursor = (long)nc.ParseLong();
                var pc = ul.Element("previous_cursor");
                if (pc != null)
                    prevCursor = (long)pc.ParseLong();
            }
            List<TwitterUser> users = new List<TwitterUser>();
            return from n in doc.Descendants("user")
                   let u = TwitterUser.CreateByNode(n)
                   where u != null
                   select u;
        }

        /// <summary>
        /// Get list subscribers
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">list owner user's id</param>
        /// <param name="listId">list id</param>
        public static IEnumerable<TwitterUser> GetListSubscribersAll(this CredentialProvider provider, string userId, string listId)
        {
            long n_cursor = -1;
            long c_cursor = -1;
            long p;
            while (n_cursor != 0)
            {
                foreach (var m in provider.GetListSubscribers(userId, listId, c_cursor, out p, out n_cursor))
                    yield return m;
                c_cursor = n_cursor;
            }
        }

        /// <summary>
        /// Get list subscribers with full params
        /// </summary>
        public static IEnumerable<TwitterUser> GetListSubscribers(this CredentialProvider provider, string userId, string listId, long? cursor, out long prevCursor, out long nextCursor)
        {
            listId = listId.Replace("_", "-");

            var partialUri = userId + "/" + listId + "/subscribers.xml";

            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            if (cursor != null)
            {
                para.Add(new KeyValuePair<string, string>("cursor", cursor.ToString()));
            }

            prevCursor = -1;
            nextCursor = -1;

            var doc = provider.RequestAPIv1(partialUri, CredentialProvider.RequestMethod.GET, para);
            if (doc == null)
                return null;

            var ul = doc.Element("users_list");
            if (ul != null)
            {
                var nc = ul.Element("next_cursor");
                if (nc != null)
                    nextCursor = (long)nc.ParseLong();
                var pc = ul.Element("previous_cursor");
                if (pc != null)
                    prevCursor = (long)pc.ParseLong();
            }
            List<TwitterUser> users = new List<TwitterUser>();
            return from n in doc.Descendants("user")
                   let u = TwitterUser.CreateByNode(n)
                   where u != null
                   select u;
        }

        /// <summary>
        /// Get list with full params
        /// </summary>
        private static IEnumerable<TwitterList> GetListData(this CredentialProvider provider, string partialUri, long? cursor, out long prevCursor, out long nextCursor)
        {
            List<KeyValuePair<string, string>> para = new List<KeyValuePair<string, string>>();
            if (cursor != null)
            {
                para.Add(new KeyValuePair<string, string>("cursor", cursor.ToString()));
            }

            prevCursor = 0;
            nextCursor = 0;

            var doc = provider.RequestAPIv1(partialUri, CredentialProvider.RequestMethod.GET, para);
            if (doc == null)
                return null;
            var ll = doc.Element ("lists_list");
            if (ll != null)
            {
                var nc = ll.Element("next_cursor");
                if (nc != null)
                    nextCursor = (long)nc.ParseLong();
                var pc = ll.Element("previous_cursor");
                if (pc != null)
                    prevCursor = (long)pc.ParseLong();
            }


            return from n in doc.Descendants("list")
                   let l = TwitterList.CreateByNode(n)
                   where l != null
                   select l;
        }

        /// <summary>
        /// Get lists you following
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterList> GetFollowingListsAll(this CredentialProvider provider, string userId)
        {
            foreach (var l in provider.GetListsAll(userId))
                yield return l;
            foreach (var l in provider.GetSubscribedListsAll(userId))
                yield return l;
        }

        /// <summary>
        /// Get all lists someone created
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterList> GetListsAll(this CredentialProvider provider, string userId)
        {
            long n_cursor = -1;
            long c_cursor = -1;
            long p;
            while (n_cursor != 0)
            {
                var lists = provider.GetLists(userId, c_cursor, out p, out n_cursor);
                if (lists != null)
                    foreach (var l in lists)
                        yield return l;
                c_cursor = n_cursor;
            }
        }

        /// <summary>
        /// Get lists someone created 
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterList> GetLists(this CredentialProvider provider, string userId)
        {
            long n, p;
            return provider.GetLists(userId, -1, out p, out n);
        }

        /// <summary>
        /// Get lists someone created with full params
        /// </summary>
        public static IEnumerable<TwitterList> GetLists(this CredentialProvider provider, string userId, long? cursor, out long prevCursor, out long nextCursor)
        {
            var partialUri = userId + "/lists.xml";
            return provider.GetListData(partialUri, cursor, out prevCursor, out nextCursor);
        }

        /// <summary>
        /// Get all lists which member contains someone
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterList> GetMembershipListsAll(this CredentialProvider provider, string userId)
        {
            long n_cursor = -1;
            long c_cursor = -1;
            long p;
            while (n_cursor != 0)
            {
                var lists = provider.GetMembershipLists(userId, c_cursor, out p, out n_cursor);
                if (lists != null)
                    foreach (var l in lists)
                        yield return l;
                c_cursor = n_cursor;
            }
        }

        /// <summary>
        /// Get lists which member contains someone
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterList> GetMembershipLists(this CredentialProvider provider, string userId)
        {
            long n, p;
            return provider.GetMembershipLists(userId, null, out p, out n);
        }

        /// <summary>
        /// Get lists which member contains someone with full params
        /// </summary>
        public static IEnumerable<TwitterList> GetMembershipLists(this CredentialProvider provider, string userId, long? cursor, out long prevCursor, out long nextCursor)
        {
            var partialUri = userId + "/lists/memberships.xml";
            return provider.GetListData(partialUri, cursor, out prevCursor, out nextCursor);
        }

        /// <summary>
        /// Get lists all someone subscribed
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterList> GetSubscribedListsAll(this CredentialProvider provider, string userId)
        {
            long n_cursor = -1;
            long c_cursor = -1;
            long p;
            while (n_cursor != 0)
            {
                var lists = provider.GetSubscribedLists(userId, c_cursor, out p, out n_cursor);
                if (lists != null)
                    foreach (var l in lists)
                        yield return l;
                c_cursor = n_cursor;
            }
        }

        /// <summary>
        /// Get lists someone subscribed
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">target user id</param>
        public static IEnumerable<TwitterList> GetSubscribedLists(this CredentialProvider provider, string userId)
        {
            long n, p;
            return provider.GetSubscribedLists(userId, null, out p, out n);
        }

        /// <summary>
        /// Get lists someone subscribed with full params
        /// </summary>
        public static IEnumerable<TwitterList> GetSubscribedLists(this CredentialProvider provider, string userId, long? cursor, out long prevCursor, out long nextCursor)
        {
            var partialUri = userId + "/lists/subscriptions.xml";
            return provider.GetListData(partialUri, cursor, out prevCursor, out nextCursor);
        }

        /// <summary>
        /// Get single list data
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">list owner user's id</param>
        /// <param name="listId">list is</param>
        public static TwitterList GetList(this CredentialProvider provider, string userId, string listId)
        {
            var list = provider.RequestAPIv1(userId + "/lists/" + listId + ".xml",
                 CredentialProvider.RequestMethod.GET, null).Element("list");
            if (list != null)
                return TwitterList.CreateByNode(list);
            else
                return null;
        }

        /// <summary>
        /// Create or update list
        /// </summary>
        private static TwitterList CreateOrUpdateList(this CredentialProvider provider, string id, string name, string description, bool? inPrivate)
        {
            var kvp = new List<KeyValuePair<string, string>>();
            if (id != null)
                kvp.Add(new KeyValuePair<string, string>("id", id));
            if (name != null)
                kvp.Add(new KeyValuePair<string, string>("name", Tweak.CredentialProviders.OAuth.UrlEncode(name, Encoding.UTF8, true)));
            if (description != null)
                kvp.Add(new KeyValuePair<string, string>("description", Tweak.CredentialProviders.OAuth.UrlEncode(description, Encoding.UTF8, true)));
            if (inPrivate != null)
                kvp.Add(new KeyValuePair<string, string>("mode", inPrivate.Value ? "private" : "public"));
            var list = provider.RequestAPIv1(
                "user/lists.xml",
                 CredentialProvider.RequestMethod.POST,
                 kvp).Element("list");
            if (list != null)
                return TwitterList.CreateByNode(list);
            else
                return null;
        }

        /// <summary>
        /// Create new list
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="name">list name</param>
        /// <param name="description">list description</param>
        /// <param name="inPrivate">private mode</param>
        public static TwitterList CreateList(this CredentialProvider provider, string name, string description, bool? inPrivate)
        {
            return provider.CreateOrUpdateList(null, name, description, inPrivate);
        }

        /// <summary>
        /// Update list information
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="id">list id</param>
        /// <param name="newName">list's new name</param>
        /// <param name="description">new description</param>
        /// <param name="inPrivate">private mode</param>
        public static TwitterList UpdateList(this CredentialProvider provider, string id, string newName, string description, bool? inPrivate)
        {
            return provider.CreateOrUpdateList(id, newName, description, inPrivate);
        }

        /// <summary>
        /// Delete list you created
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="userId">your id</param>
        /// <param name="listId">list id</param>
        public static TwitterList DeleteList(this CredentialProvider provider, string userId, string listId)
        {
            var kvp = new[] { new KeyValuePair<string, string>("_method", "DELETE") };
            var list = provider.RequestAPIv1(
                 userId + "/lists/" + listId + ".xml",
                  CredentialProvider.RequestMethod.POST,
                  kvp).Element("list");
            if (list != null)
                return TwitterList.CreateByNode(list);
            else
                return null;
        }

        /// <summary>
        /// Add user into your list
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="yourScreenName">your screen name</param>
        /// <param name="listId">list id</param>
        /// <param name="addUser">adding user id or screen name</param>
        public static TwitterList AddUserIntoList(this CredentialProvider provider, string yourScreenName, string listId, string addUser)
        {
            var kvp = new[] { new KeyValuePair<string, string>("id", addUser) };
            var list = provider.RequestAPIv1(
                yourScreenName + "/" + listId + ".xml",
                CredentialProvider.RequestMethod.POST,
                kvp).Element("list");
            if (list != null)
                return TwitterList.CreateByNode(list);
            else
                return null;
        }

        /// <summary>
        /// Delete user from your list
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="yourScreenName">your screen name</param>
        /// <param name="listId">list id</param>
        /// <param name="deleteUserId">deleting user id</param>
        public static TwitterList DeleteUserFromList(this CredentialProvider provider, string yourScreenName, string listId, string deleteUserId)
        {
            var kvp = new[] { new KeyValuePair<string, string>("id", deleteUserId), new KeyValuePair<string, string>("_method", "DELETE") };
            var list = provider.RequestAPIv1(
                yourScreenName + "/" + listId + ".xml",
                CredentialProvider.RequestMethod.POST,
                kvp).Element("list");
            if (list != null)
                return TwitterList.CreateByNode(list);
            else
                return null;
        }

        /// <summary>
        /// Get user information in list<para />
        /// You can use this method for check someone existing in list.
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="user">list owner user id or screen name</param>
        /// <param name="listId">list id</param>
        /// <param name="queryUserId">query user id</param>
        public static TwitterUser GetListMember(this CredentialProvider provider, string user, string listId, string queryUserId)
        {
            var query = provider.RequestAPIv1(
                user + "/" + listId + "/members/" + queryUserId + ".xml",
                CredentialProvider.RequestMethod.GET, null).Element("user");
            if (user != null)
                return TwitterUser.CreateByNode(query);
            else
                return null;
        }

        /// <summary>
        /// Subscribe list
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="screenName">list owner user screen name</param>
        /// <param name="listId">list id</param>
        public static bool SubscribeList(this CredentialProvider provider, string screenName, string listId)
        {
            var users = provider.RequestAPIv1(
                screenName + "/" + listId + "/subscribers.xml",
                CredentialProvider.RequestMethod.POST, null).Element("users");
            if (users == null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// UnSubscribe list
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="screenName">list owner user screen name</param>
        /// <param name="listId">list id</param>
        public static bool UnsubscribeList(this CredentialProvider provider, string screenName, string listId)
        {
            var kvp = new[] { new KeyValuePair<string, string>("_method", "DELETE") };
            var users = provider.RequestAPIv1(
                screenName + "/" + listId + "/subscribers.xml",
                CredentialProvider.RequestMethod.POST, kvp).Element("users");
            if (users == null)
                return false;
            else
                return true;
        }

        /// <summary>
        /// Get subscriber information in list<para />
        /// You can use this method for check someone subscribing a list.
        /// </summary>
        /// <param name="provider">credential provider</param>
        /// <param name="screenName">list owner user id</param>
        /// <param name="listId">list id</param>
        /// <param name="queryUserId">query user id</param>
        public static TwitterUser GetListSubscriber(this CredentialProvider provider, string screenName, string listId, string queryUserId)
        {
            var query = provider.RequestAPIv1(
                screenName + "/" + listId + "/subscribers/" + queryUserId + ".xml",
                CredentialProvider.RequestMethod.GET, null).Element("user");
            if (query != null)
                return TwitterUser.CreateByNode(query);
            else
                return null;
        }

        #endregion

    }
}
