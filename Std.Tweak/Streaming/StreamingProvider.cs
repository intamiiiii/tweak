using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;


namespace Std.Tweak.Streaming
{
    /// <summary>
    /// Streaming API provider
    /// </summary>
    public static class StreamingProvider
    {

        static Thread streamThread = null;
        static Thread queueTreatingThread = null;

        static Queue<string> recvQueue;
        public static event Action<TwitterStatus> OnReceivedStatus;
               

        /// <summary>
        /// Start streaming receive
        /// </summary>
        /// <param name="provider">using credential</param>
        /// <param name="type">type of streaming</param>
        public static void StartStreaming(this CredentialProvider provider, StreamingType type, int? delimitered = 0, int? count = 0, string follow = null, string track = null)
        {
            List<KeyValuePair<string,string>> args = new List<KeyValuePair<string,string>>();
            if(delimitered != null)
                args.Add(new KeyValuePair<string,string>("delimitered", delimitered.Value.ToString()));
            if (count != null)
                args.Add(new KeyValuePair<string, string>("count", count.Value.ToString()));
            if(follow != null)
                args.Add(new KeyValuePair<string,string>("follow", follow));
            if(track != null)
                args.Add(new KeyValuePair<string,string>("track", track));
            provider.RequestStreamingAPI(GetStreamingUri(type), CredentialProvider.RequestMethod.GET, args);

        }

        /// <summary>
        /// End streaming receive
        /// </summary>
        public static void EndStreaming()
        {
        }

        private static void StreamingThread(object streamarg)
        {
            var str = streamarg as Stream;
            if(str == null)
                return;
        }

        private static void QueueDequeueThread()
        {
            while (true)
            {
                if (recvQueue.Count > 0)
                {
                    var str = recvQueue.Dequeue();

                }
                else
                {
                    Thread.Sleep(100);
                }
                Thread.Sleep(0);
            }
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
            ///   track: tracking keyword(max 30 bytes)
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
        public static string GetStreamingUri(StreamingType type)
        {
            switch (type)
            {
                case StreamingType.firehose:
                case StreamingType.gardenhose:
                case StreamingType.sample:
                case StreamingType.birddog:
                case StreamingType.shadow:
                case StreamingType.filter:
                case StreamingType.links:
                case StreamingType.retweet:
                    return String.Format(SapiV1, type.ToString());
                default:
                    throw new ArgumentOutOfRangeException("Invalid enum value");
            }
        }
    }
}
