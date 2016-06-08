
using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace NRPC.Channels.Pipe
{
    public class NamePipeClientChannel : NamePipeChannel
    {
        public NamePipeClientChannel(IOptions<NamePipeConfig> options)
            : base(options)
        {
            
        }

        protected new NamedPipeClientStream PipeStream
        {
            get
            {
                return base.PipeStream as NamedPipeClientStream;
            }
        }

        public override Task Start()
        {
            return PipeStream.ConnectAsync();
        }

        protected override PipeStream CreatePipeStream(NamePipeConfig config)
        {
            return new NamedPipeClientStream(config.ServerName, config.PipeName, PipeDirection.InOut);
        }
    }
}