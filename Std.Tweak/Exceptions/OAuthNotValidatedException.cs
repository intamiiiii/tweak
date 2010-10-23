using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Std.Tweak.Exceptions
{
    /// <summary>
    /// OAuth validation exception
    /// </summary>
    [Serializable]
    public class OAuthNotValidatedException : System.Security.SecurityException
    {
        /// <summary>
        /// OAuth validation exception
        /// </summary>
        public OAuthNotValidatedException() { }
        /// <summary>
        /// OAuth validation exception
        /// </summary>
        public OAuthNotValidatedException(string message) : base(message) { }
        /// <summary>
        /// OAuth validation exception
        /// </summary>
        public OAuthNotValidatedException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// OAuth validation exception
        /// </summary>
        protected OAuthNotValidatedException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }
}
