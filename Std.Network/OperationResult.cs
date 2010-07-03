
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace Std.Network
{
    /// <summary>
    /// Result of operation
    /// </summary>
    /// <typeparam name="T">Data type</typeparam>
    public class OperationResult<T>
    {
        /// <summary>
        /// Target uri
        /// </summary>
        public Uri Target { get; private set; }

        /// <summary>
        /// Is succeeded operation
        /// </summary>
        public bool Succeeded { get; private set; }

        /// <summary>
        /// Thrown exception
        /// </summary>
        public Exception Exception { get; private set; }

        /// <summary>
        /// Description message (optional)
        /// </summary>
        public string Message { get; private set; }

        /// <summary>
        /// Http status code
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// Returned data
        /// </summary>
        public T Data { get; private set; }

        /// <summary>
        /// Operation result
        /// </summary>
        public OperationResult(Uri target, bool succeeded, HttpStatusCode statusCode, T data, string message, Exception thrown)
        {
            this.Target = target;
            this.Succeeded = succeeded;
            this.StatusCode = statusCode;
            this.Data = data;
            this.Message = message;
            this.Exception = thrown;
        }

        /// <summary>
        /// Succeeded result
        /// </summary>
        /// <param name="target">target</param>
        /// <param name="data">return data</param>
        /// <param name="message">message</param>
        public OperationResult(Uri target, HttpStatusCode statusCode, T data, string message = null)
            : this(target, true, statusCode, data, message, null) { }

        /// <summary>
        /// Succeeded result(old)
        /// </summary>
        [Obsolete("Please set status code.")]
        public OperationResult(Uri target, T data, string message = null)
            : this(target, (HttpStatusCode)0, data , message) { }


        /// <summary>
        /// Failed result
        /// </summary>
        /// <param name="target">target uri</param>
        /// <param name="thrown">thrown exception</param>
        /// <param name="message">returning message</param>
        public OperationResult(Uri target, Exception thrown, HttpStatusCode statusCode = 0, string message = null)
            : this(target, false,  statusCode, default(T), message, thrown) { }
    }
}
