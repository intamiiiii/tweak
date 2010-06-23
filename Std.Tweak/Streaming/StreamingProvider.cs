using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Std.Tweak.Streaming
{
    /// <summary>
    /// Streaming API provider
    /// </summary>
    public static class StreamingProvider
    {
        /// <summary>
        /// Using streaming interface type
        /// </summary>
        public enum StreamingType
        {
            /// <summary>
            /// firehose, All public information (limited)
            /// </summary>
            firehose,
            /// <summary>
            /// gardenhose, Optimized public information (limited)
            /// </summary>
            gardenhose,
            /// <summary>
            /// sample, Partial public information (public)
            /// </summary>
            sample,
            /// <summary>
            /// birddog, Specified user's public information (limited, max 400000 users)
            /// </summary>
            birddog,
            /// <summary>
            /// shadow, Specified user's public information (limited, max 80000 users)
            /// </summary>
            shadow,
            /// <summary>
            /// filter, Filtered public information<para/>
            /// Filter implies user id or keywords.<para/>
            /// User id: max 400, Keyword: max 200(restricted: 10000 partner: 200000)
            /// </summary>
            fliter,
            /// <summary>
            /// link, tweets contains http(s) (private)
            /// </summary>
            links,
            /// <summary>
            /// retweet, retweeted tweets (private)
            /// </summary>
            retweet,
            /// <summary>
            /// chirp, contains following user's activity (beta)
            /// </summary>
            chirp

        }
    }
}
