using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using System.Xml.Linq;


namespace Std.Tweak.Streaming
{
    /// <summary>
    /// Streaming controller class
    /// </summary>
    [Obsolete("Use StreamingCore class.")]
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
        /// <param name="repliesAll">use @replies=all option</param>
        public static StreamingController BeginStreaming
            (CredentialProvider provider, StreamingType type,
            int? delimitered = null, int? count = null,
            string follow = null, string track = null, string locations = null, bool repliesAll = false)
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
                args.Add(new KeyValuePair<string, string>("follow", Tweak.CredentialProviders.OAuth.UrlEncode(follow, Encoding.UTF8, true)));
            if (!String.IsNullOrWhiteSpace(track))
                args.Add(new KeyValuePair<string, string>("track", Tweak.CredentialProviders.OAuth.UrlEncode(track, Encoding.UTF8, true)));
            if (!String.IsNullOrWhiteSpace(locations))
                args.Add(new KeyValuePair<string, string>("locations", Tweak.CredentialProviders.OAuth.UrlEncode(locations, Encoding.UTF8, true)));
            if (repliesAll)
                args.Add(new KeyValuePair<string, string>("replies", "all"));
            var strm = provider.RequestStreamingAPI(GetStreamingUri(type), reqmethod, args);
            if (strm != null)
                return new StreamingController(strm);
            else
                return null;
        }

        private Stream currentReceivingStream;

        private StreamingController(Stream stream)
        {
            Disposed = false;

            currentReceivingStream = stream;

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
            ///   count: backlog length<para/>
            ///   delimited: data length(byte)<para/>
            ///   track: tracking keyword(each keywords max 60 bytes, separated comma.)<para/>
            ///   locations: location area of tweet<para/>
            ///   replies: if you set this parameter as &quot;all&quot; twitter deriver all mentions of followings.<para />
            ///   with: if you set this parameter as user, twitter deriver limited events. See API document.
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
                    return "https://userstream.twitter.com/2/user.json";
                default:
                    throw new ArgumentOutOfRangeException("Invalid streaming value");
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Is disposed this object
        /// </summary>
        public bool Disposed { get; private set; }

        #endregion

        #region Events

        /// <summary>
        /// Thread disconnected
        /// </summary>
        public event Action OnDisconnected = () => { };

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
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                // On disconnecting, catch "IOException", but this exception can ignore.
                if (Disposed) return;
                // This method runs on another thread, so must treat ALL exceptions!
                OnStreamingErrorThrown.Invoke(e);
            }
            finally
            {
                // Close stream
                try
                {
                    str.Close();
                    if (OnDisconnected != null)
                        OnDisconnected.Invoke();
                }
                catch { }
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
                    bool flag = false;
                    for (int i = 0; i < 90; i++)
                    {
                        flag = queueTracker.WaitOne(1000);
                        if (streamReceiver == null) break; // stopped receiving
                        if (flag) break;
                    }
                    if (!flag)
                    {
                        if (!Disposed)
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
            if (currentReceivingStream != null)
            {
                try
                {
                    currentReceivingStream.Dispose();
                }
                catch { }
            }
            streamReceiver = null;
            Disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose streaming controller
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException("StreamingController");
            EndStreaming();
        }

        /// <summary>
        /// Finalizer
        /// </summary>
        ~StreamingController()
        {
            if (streamReceiver != null)
                streamReceiver.Abort();
            streamReceiver = null;
        }

        /// <summary>
        /// ストリーミングスレッドで例外がスローされた場合に呼び出されます。
        /// </summary>
        public event Action<Exception> OnStreamingErrorThrown = (e) => { };
    }
}
