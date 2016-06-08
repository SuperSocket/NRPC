
using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NRPC.Base;

namespace NRPC.Channels.Pipe
{
    public abstract class NamePipeChannel : IRpcChannel, IDisposable
    {
        protected NamePipeConfig Config { get; private set; }

        private PipeStream m_PipeStream;
        protected virtual PipeStream PipeStream
        {
            get { return m_PipeStream; }
        }

        public NamePipeChannel(IOptions<NamePipeConfig> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
                
            Config = options.Value;
            m_PipeStream = CreatePipeStream(Config);
        }

        protected abstract PipeStream CreatePipeStream(NamePipeConfig config);

        public Task<ArraySegment<byte>> ReceiveAsync()
        {
            throw new NotImplementedException();
        }

        public Task SendAsync(ArraySegment<byte> data)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            var pipeStream = m_PipeStream;

            if (pipeStream == null)
                return;

            if(Interlocked.CompareExchange(ref m_PipeStream, null, pipeStream) == pipeStream)
            {
                pipeStream.Dispose();
            }
        }
    }
}