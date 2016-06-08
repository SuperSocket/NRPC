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
            var serverChannel = m_WaitingChannel = new NamePipeServerChannel(m_Options);
            await serverChannel.AcceptAsync();
            m_WaitingChannel = null;
            return serverChannel;
        }

        public void Dispose()
        {
            var waitingChannel = m_WaitingChannel;

            if (waitingChannel == null)
                return;

            if(Interlocked.CompareExchange(ref m_WaitingChannel, null, waitingChannel) == waitingChannel)
            {
                waitingChannel.Dispose();
            }
        }
    }
}