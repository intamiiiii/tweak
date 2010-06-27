using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;


namespace Std.Tweak.Streaming
{
    /// <summary>
    /// Streaming API provider
    /// </summary>
    public static class StreamingApi
    {
        static Thread streamThread = null;
        static Thread queueTreatingThread = null;

        /// <summary>
        /// Parsing status with multi-threading
        /// </summary>
        static bool ParseWithMultiThreading = true;

        static Queue<string> recvQueue = new Queue<string>();

        /// <summary>
        /// Received status
        /// </summary>
        public static event Action<TwitterStatus> OnReceivedStatus;

        /// <summary>
        /// Thread disconnected
        /// </summary>
        public static event Action OnDisconnected;

        /// <summary>
        /// Delete required
        /// </summary>
        public static event Action<long> OnDeleteRequired;

        /// <summary>
        /// Track information received
        /// </summary>
        public static event Action<long> OnTrackReceived;

        /// <summary>
        /// Undefined xml received
        /// </summary>
        public static event Action<XElement> OnUndefinedXMLReceived;

        #region Public methods

        /// <summary>
        /// Start streaming receive
        /// </summary>
        /// <param name="provider">using credential</param>
        /// <param name="type">type of streaming</param>
        /// <param name="delimitered">delimiter length</param>
        /// <param name="count">backlog count</param>
        /// <param name="follow">following user's id</param>
        /// <param name="track">tracking keywords</param>
        /// <param name="locations">location area of tweet</param>
        public static void StartStreaming(this CredentialProvider provider, StreamingType type, int? delimitered = null, int? count = null, string follow = null, string track = null, string locations = null)
        {
            // argument check
            switch (type)
            {
                case StreamingType.firehose:
                    if (follow != null || track != null || locations != null)
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.gardenhose:
                    if (count != null && follow != null || track != null || locations != null)
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.sample:
                    if (count != null && follow != null || track != null || locations != null)
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.birddog:
                    if (track != null || locations != null)
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.shadow:
                    if (track != null || locations != null)
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.filter:
                    if (track == null && follow == null)
                        throw new ArgumentException("You must set follow or track argument.");
                    break;
            }

            List<KeyValuePair<string,string>> args = new List<KeyValuePair<string,string>>();
            if(delimitered != null)
                args.Add(new KeyValuePair<string,string>("delimitered", delimitered.Value.ToString()));
            if (count != null)
                args.Add(new KeyValuePair<string, string>("count", count.Value.ToString()));
            if(follow != null)
                args.Add(new KeyValuePair<string,string>("follow", follow));
            if(track != null)
                args.Add(new KeyValuePair<string,string>("track", track));
            if (locations != null)
                args.Add(new KeyValuePair<string, string>("locations", locations));
            queueTreatingThread = new Thread(new ThreadStart(QueueDequeueThread));
            queueTreatingThread.Start();
            streamThread = new Thread(new ParameterizedThreadStart(StreamingThread));
            System.Diagnostics.Debug.WriteLine("URL:" + GetStreamingUri(type) + ", arg:" + args.ToString());
            var strm = provider.RequestStreamingAPI(GetStreamingUri(type), CredentialProvider.RequestMethod.POST, args);
            streamThread.Start(strm);
        }

        /// <summary>
        /// End streaming receive
        /// </summary>
        public static void EndStreaming()
        {
            streamThread.Abort();
            queueTreatingThread.Abort();
        }

        /// <summary>
        /// End streaming receive
        /// </summary>
        /// <remarks>
        /// For supporting extension method. (Same StreamingProvider.EndStreaming())
        /// </remarks>
        /// <param name="provider">Not used.</param>
        public static void EndStreaming(this CredentialProvider provider)
        {
            EndStreaming();
        }

        #endregion

        private static void StreamingThread(object streamarg)
        {
            var str = streamarg as Stream;
            if(str == null)
                return;
            try
            {
                using (var sr = new StreamReader(str))
                {
                    while (!sr.EndOfStream)
                    {
                        recvQueue.Enqueue(sr.ReadLine());
                        queueTracker.Set();
                    }
                }
            }
            finally
            {
                str.Close();
                if (OnDisconnected != null)
                    OnDisconnected.Invoke();
            }
        }

        static ManualResetEvent queueTracker = new ManualResetEvent(true);
        /// <summary>
        /// queue deque &amp; treating thread
        /// </summary>
        private static void QueueDequeueThread()
        {
            while (true)
            {
                queueTracker.Reset();
                if (recvQueue.Count > 0)
                {
                    var str = recvQueue.Dequeue();
                    if (String.IsNullOrEmpty(str) || String.IsNullOrEmpty(str.Trim()))
                        continue;
                    if (ParseWithMultiThreading)
                    {
                        // send childmethod
                        var act = new Action<string>(ParseJson);
                        act.BeginInvoke(str, (iar) => ((Action<string>)iar.AsyncState).EndInvoke(iar), act);
                    }
                    else
                    {
                        ParseJson(str);
                    }
                }
                else
                {
                    queueTracker.WaitOne();
                }
            }
        }

        /// <summary>
        /// text perser child method
        /// </summary>
        private static void ParseJson(string jsonText)
        {
            using (var json = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(jsonText), System.Xml.XmlDictionaryReaderQuotas.Max))
            {
                var xe = XElement.Load(json);
                if (xe.Element("text") != null && xe.Element("user") != null) // 多分普通のステータスじゃねーの
                    ParseNormalStatus(xe);
                else if (xe.Element("delete") != null) // 削除通知じゃねーの
                    ParseDeleteNotify(xe);
                else if (xe.Element("limit") != null) // リミット変更通知じゃねーの
                    ParseLimitationNotify(xe);
                else
                    ParseUndefined(xe);
            }
        }

        /// <summary>
        /// Status information
        /// </summary>
        /// <param name="elem"></param>
        private static void ParseNormalStatus(XElement elem)
        {
            var ts = TwitterStatus.CreateByNode(elem);
            if (ts != null)
                OnReceivedStatus.Invoke(ts);
        }

        /// <summary>
        /// Status delete information
        /// </summary>
        private static void ParseDeleteNotify(XElement elem)
        {
            var sid = elem.Element("id").ParseLong();
            if (sid > 0)
                OnDeleteRequired.Invoke(sid);
        }

        /// <summary>
        /// Parse limit changed information
        /// </summary>
        /// <param name="elem"></param>
        private static void ParseLimitationNotify(XElement elem)
        {
            var track = elem.Element("track").ParseLong();
            if (track != 0)
                OnTrackReceived.Invoke(track);
        }

        /// <summary>
        /// Undefined information
        /// </summary>
        private static void ParseUndefined(XElement elem)
        {
            if (elem != null)
                OnUndefinedXMLReceived.Invoke(elem);
        }

        /// <summary>
        /// Using streaming interface type
        /// </summary>
        public enum StreamingType
        {
            /// <summary>
            /// firehose, All public information (limited)<para/>
            /// Arguments:<para/>
            ///   count: backlog length<para/>
            ///   delimited: data length(byte)
            /// </summary>
            firehose,
            /// <summary>
            /// gardenhose, Optimized public information (limited)<para/>
            /// Arguments:<para/>
            ///   delimited: data length(byte)
            /// </summary>
            gardenhose,
            /// <summary>
            /// sample, Partial public information (public)<para/>
            /// Arguments:<para/>
            ///   delimited: data length(byte)
            /// </summary>
            sample,
            /// <summary>
            /// birddog, Specified user's public information (limited, max 400000 users)<para/>
            /// Arguments:<para/>
            ///   count: backlog length<para/>
            ///   delimited: data length(byte)<para/>
            ///   follow: following user's id (delimiter: space)
            /// </summary>
            birddog,
            /// <summary>
            /// shadow, Specified user's public information (limited, max 80000 users)<para/>
            /// Arguments:<para/>
            ///   count: backlog length<para/>
            ///   delimited: data length(byte)<para/>
            ///   follow: following user's id (delimiter: space)
            /// </summary>
            shadow,
            /// <summary>
            /// filter, Filtered public information<para/>
            /// Filter implies user id or keywords.<para/>
            /// User id: max 400, Keyword: max 200(restricted: 10000 partner: 200000)<para/>
            /// Arguments:<para/>
            ///   count: backlog length<para/>
            ///   delimited: data length(byte)<para/>
            ///   follow: following user's id (delimiter: space)<para/>
            ///   track: tracking keyword(each keywords max 60 bytes, separated comma.)<para/>
            ///   locations: location area of tweet<para/>
            /// you must set follow or track argument.
            /// </summary>
            filter,
            /// <summary>
            /// link, tweets contains http(s) (private)<para/>
            /// Arguments:<para/>
            ///   delimited: data length(byte)
            /// </summary>
            links,
            /// <summary>
            /// retweet, retweeted tweets (private)<para/>
            /// Arguments:<para/>
            ///   delimited: data length(byte)
            /// </summary>
            retweet
        }

        const string SapiV1 = "http://stream.twitter.com/1/statuses/{0}.json";
        private static string GetStreamingUri(StreamingType type)
        {
            switch (type)
            {
                case StreamingType.firehose:
                case StreamingType.gardenhose:
                case StreamingType.sample:
                case StreamingType.birddog:
                case StreamingType.shadow:
                case StreamingType.links:
                case StreamingType.retweet:
                case StreamingType.filter:
                    return String.Format(SapiV1, type.ToString());
                default:
                    throw new ArgumentOutOfRangeException("Invalid enum value");
            }
        }
    }
}
