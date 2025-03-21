using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private IPipelineFilter m_PipeLineProcessor;

        private PipeStream m_PipeStream;

        public event Action<RpcChannelPackageInfo> NewPackageReceived;

        private CancellationTokenSource m_ChannelCancelToken = new CancellationTokenSource();

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
            m_PipeLineProcessor = new DefaultPipelineProcessor<PipePackageInfo>(new PipeReceiveFilter(), 1024 * 64);
            StartReceive();
        }

        private async void StartReceive()
        {
            var buffer = new byte[m_PipeStream.InBufferSize];

            var cancellationToken = m_ChannelCancelToken.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await m_PipeStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                var processResult = m_PipeLineProcessor.Process(new ArraySegment<byte>(buffer, 0, result));
                
                if (processResult.State == ProcessState.Error)
                {
                    Debug.Fail("Pipeline processing failed.");
                    break;
                }
                else if(processResult.State == ProcessState.Cached)
                {
                    buffer = new byte[m_PipeStream.InBufferSize];
                }

                if (processResult.Packages != null && processResult.Packages.Count > 0)
                {
                    foreach (var item in processResult.Packages)
                    {
                        HandlePipePackage(item as PipePackageInfo);
                    }
                }
            }
        }

        public Task SendAsync(IList<ArraySegment<byte>> data)
        {
            throw new NotImplementedException();
        }

        void HandlePipePackage(PipePackageInfo package)
        {
            var handler = NewPackageReceived;

            if (handler != null)
                handler.Invoke(package);
        }

        public void Dispose()
        {
            var pipeStream = m_PipeStream;

            if (pipeStream == null)
                return;

            if(Interlocked.CompareExchange(ref m_PipeStream, null, pipeStream) == pipeStream)
            {
                m_ChannelCancelToken.Cancel();
                pipeStream.Dispose();
            }
        }
    }
}