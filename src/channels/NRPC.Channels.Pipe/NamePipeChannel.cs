using System;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NRPC.Base;
using SuperSocket.ProtoBase;

namespace NRPC.Channels.Pipe
{
    public abstract class NamePipeChannel : IRpcChannel, IDisposable
    {
        protected NamePipeConfig Config { get; private set; }

        private IPipelineProcessor m_PipeLineProcessor;

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


        protected virtual void OnChannelReady()
        {
            //m_PipeLineProcessor = new DefaultPipelineProcessor<PipePackageInfo>(new PipeReceiveFilter(), 1024 * 64);
        }

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