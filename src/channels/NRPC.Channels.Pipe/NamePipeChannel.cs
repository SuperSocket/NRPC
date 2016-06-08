
using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NRPC.Base;

namespace NRPC.Channels.Pipe
{
    public abstract class NamePipeChannel : IRpcChannel
    {
        protected NamePipeConfig Config { get; private set; }

        protected virtual PipeStream PipeStream { get; private set; }

        public NamePipeChannel(IOptions<NamePipeConfig> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
                
            Config = options.Value;
            PipeStream = CreatePipeStream(Config);
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
    }
}