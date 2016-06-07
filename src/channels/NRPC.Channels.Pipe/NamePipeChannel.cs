
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NRPC.Base;

namespace NRPC.Channels.Pipe
{
    public abstract class NamePipeChannel : IRpcChannel
    {
        protected NamePipeConfig Config { get; private set; }

        public NamePipeChannel(IOptions<NamePipeConfig> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
                
            Config = options.Value;
        }

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