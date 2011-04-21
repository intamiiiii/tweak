using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace Std.Tweak.Streaming
{
    public class StreamingConnection : IDisposable
    {
        /// <summary>
        /// Parent streaming core
        /// </summary>
        protected readonly StreamingCore parentCore;

        /// <summary>
        /// Connection disconnected
        /// </summary>
        public Action<bool> OnDisconnected = _ => { };

        /// <summary>
        /// Current credential provider
        /// </summary>
        public CredentialProvider Provider;

        private Stream receiveStream;
        private Thread streamReceiver;

        internal StreamingConnection(StreamingCore core, CredentialProvider provider, Stream strm)
        {
            if (core == null)
                throw new ArgumentNullException("core");
            if (provider == null)
                throw new ArgumentNullException("provider");
            if (strm == null)
                throw new ArgumentNullException("strm");
            this.parentCore = core;
            this.Provider = provider;
            this.receiveStream = strm;
            this.streamReceiver = new Thread(StreamingThread);
            this.streamReceiver.Start();
        }

        private void StreamingThread()
        {
            try
            {
                using (var sr = new StreamReader(this.receiveStream))
                {
                    while (!sr.EndOfStream)
                    {
                        this.parentCore.EnqueueReceivedObject(this.Provider, sr.ReadLine());
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (Exception e)
            {
                /// ignore all errors when disposing
                if (disposed) return;
                parentCore.RaiseOnExceptionThrown(e);
            }
            finally
            {
                try
                {
                    this.receiveStream.Close();
                }
                catch { }
                finally
                {
                    this.parentCore.UnregisterConnection(this);
                    OnDisconnected(disposed);
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~StreamingConnection()
        {
            this.Dispose(false);
        }

        bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed) return;
            this.disposed = true;
            streamReceiver.Abort();
            streamReceiver = null;
            this.parentCore.UnregisterConnection(this);
        }

    }
}
