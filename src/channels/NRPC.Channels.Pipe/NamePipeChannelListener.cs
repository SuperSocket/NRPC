using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NRPC.Base;

namespace NRPC.Channels.Pipe
{
    public class NamePipeChannelListener : IRpcServerChannelListener, IDisposable
    {
        private IOptions<NamePipeConfig> m_Options;

        private NamePipeServerChannel m_WaitingChannel;

        public NamePipeChannelListener(IOptions<NamePipeConfig> options)
        {
            Debug.Assert(options != null, "options is required");
            m_Options = options;
        }

        public async Task<IRpcChannel> ListenAsync()
        {
            if (disposedValue)
                throw new ObjectDisposedException("NamePipeChannelListener");

            var serverChannel = m_WaitingChannel = new NamePipeServerChannel(m_Options);
            await serverChannel.AcceptAsync();
            m_WaitingChannel = null;
            return serverChannel;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    var waitingChannel = m_WaitingChannel;

                    if (waitingChannel == null)
                        return;

                    if(Interlocked.CompareExchange(ref m_WaitingChannel, null, waitingChannel) == waitingChannel)
                    {
                        waitingChannel.Dispose();
                    }
                }

                // free unmanaged resources (unmanaged objects) and override a finalizer below.
                // no unmanaged resources to release

                // set large fields to null.
                m_Options = null;

                disposedValue = true;
            }
        }

        ~NamePipeChannelListener()
        {
           Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}