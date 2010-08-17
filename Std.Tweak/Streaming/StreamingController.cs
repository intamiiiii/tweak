using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Xml.Linq;
using System.Runtime.Serialization.Json;
using Std.Network.Xml;


namespace Std.Tweak.Streaming
{
    public class StreamingController : IDisposable
    {
        #region Factory and constructor

        /// <summary>
        /// Starting streaming and get streaming controller.
        /// </summary>
        /// <param name="provider">using credential</param>
        /// <param name="type">type of streaming</param>
        /// <param name="delimitered">delimiter length</param>
        /// <param name="count">backlog count</param>
        /// <param name="follow">following user's id</param>
        /// <param name="track">tracking keywords</param>
        /// <param name="locations">location area of tweet</param>
        public static StreamingController BeginStreaming
            (CredentialProvider provider, StreamingType type,
            int? delimitered = null, int? count = null,
            string follow = null, string track = null, string locations = null)
        {
            CredentialProvider.RequestMethod reqmethod = CredentialProvider.RequestMethod.GET;
            // argument check
            switch (type)
            {
                case StreamingType.firehose:
                    if (!String.IsNullOrWhiteSpace(follow) || !String.IsNullOrWhiteSpace(track) || !String.IsNullOrWhiteSpace(locations))
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.gardenhose:
                    if (count != null && !String.IsNullOrWhiteSpace(follow) || !String.IsNullOrWhiteSpace(track) || !String.IsNullOrWhiteSpace(locations))
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.sample:
                    if (count != null && !String.IsNullOrWhiteSpace(follow) || !String.IsNullOrWhiteSpace(track) || !String.IsNullOrWhiteSpace(locations))
                        throw new ArgumentException("Invalid argument is setted.");
                    break;
                case StreamingType.birddog:
                    if (!String.IsNullOrWhiteSpace(track) || !String.IsNullOrWhiteSpace(locations))
                        throw new ArgumentException("Invalid argument is setted.");
                    reqmethod = CredentialProvider.RequestMethod.POST;
                    break;
                case StreamingType.shadow:
                    if (!String.IsNullOrWhiteSpace(track) || !String.IsNullOrWhiteSpace(locations))
                        throw new ArgumentException("Invalid argument is setted.");
                    reqmethod = CredentialProvider.RequestMethod.POST;
                    break;
                case StreamingType.filter:
                    if (String.IsNullOrWhiteSpace(track) && String.IsNullOrWhiteSpace(follow))
                        throw new ArgumentException("You must set follow or track argument.");
                    reqmethod = CredentialProvider.RequestMethod.POST;
                    break;
            }

            List<KeyValuePair<string, string>> args = new List<KeyValuePair<string, string>>();
            if (delimitered != null)
                args.Add(new KeyValuePair<string, string>("delimitered", delimitered.Value.ToString()));
            if (count != null)
                args.Add(new KeyValuePair<string, string>("count", count.Value.ToString()));
            if (!String.IsNullOrWhiteSpace(follow))
                args.Add(new KeyValuePair<string, string>("follow", follow));
            if (!String.IsNullOrWhiteSpace(track))
                args.Add(new KeyValuePair<string, string>("track", track));
            if (!String.IsNullOrWhiteSpace(locations))
                args.Add(new KeyValuePair<string, string>("locations", locations));

            var strm = provider.RequestStreamingAPI(GetStreamingUri(type), reqmethod, args);
            if (strm != null)
                return new StreamingController(strm);
            else
                return null;
        }

        private StreamingController(Stream stream)
        {
            Disposed = false;

            // clear receiving queue
            recvQueue.Clear();

            streamReceiver = new Thread(new ParameterizedThreadStart(StreamingThread));
            streamReceiver.Start(stream);
        }

        #endregion

        #region Streaming API defined values

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
            retweet,
            /// <summary>
            /// User streaming (preview)<para />
            /// Arguments:<para />
            /// 
            /// </summary>
            user
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
                case StreamingType.user:
                    return "http://betastream.twitter.com/2b/user.json";
                default:
                    throw new ArgumentOutOfRangeException("Invalid streaming value");
            }
        }

        #endregion

        #region Properties

        public bool Disposed { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Thread disconnected
        /// </summary>
        public event Action OnDisconnected;

        #endregion

        #region Internal variables

        /// <summary>
        /// Receiving streaming
        /// </summary>
        Thread streamReceiver = null;

        /// <summary>
        /// Stop until receiving new element
        /// </summary>
        ManualResetEvent queueTracker = new ManualResetEvent(true);

        /// <summary>
        /// Queue for temporarily keeping elements
        /// </summary>
        Queue<string> recvQueue = new Queue<string>();

        #endregion

        /// <summary>
        /// Streaming main thread
        /// </summary>
        private void StreamingThread(object streamarg)
        {

            var str = streamarg as Stream;
            if (str == null)
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

        /// <summary>
        /// Enumerate received XElements with automatic parsing
        /// </summary>
        public IEnumerable<TwitterStreamingElement> EnumerateStreaming()
        {
            return EnumerateStrings().Select((s) =>
            {
                using (var json = JsonReaderWriterFactory.CreateJsonReader(Encoding.UTF8.GetBytes(s), System.Xml.XmlDictionaryReaderQuotas.Max))
                    return TwitterStreamingElement.CreateByNode(XElement.Load(json));
            });
        }

        private IEnumerable<string> EnumerateStrings()
        {
            while (true)
            {
                queueTracker.Reset();
                if (streamReceiver == null)
                    yield break;
                if (recvQueue.Count > 0)
                {
                    var str = recvQueue.Dequeue();
                    if (String.IsNullOrWhiteSpace(str))
                        continue;
                    yield return str;
                }
                else
                {
                    // if not receives any information in 90 seconds,
                    // disconnect immediately.
                    if (!queueTracker.WaitOne(90 * 1000))
                    {
                        EndStreaming();
                        yield break;
                    }
                }
            }
        }

        /// <summary>
        /// End streaming and dispose all resources
        /// </summary>
        public void EndStreaming()
        {
            if (streamReceiver != null)
                streamReceiver.Abort();
            streamReceiver = null;
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        #region IDisposable メンバー

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException("StreamingController");
            EndStreaming();
        }

        ~StreamingController()
        {
            if (streamReceiver != null)
                streamReceiver.Abort();
            streamReceiver = null;
        }

        #endregion
    }
}
