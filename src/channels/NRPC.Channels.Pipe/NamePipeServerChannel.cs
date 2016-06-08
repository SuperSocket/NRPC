
using System;
using System.IO.Pipes;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace NRPC.Channels.Pipe
{
    public class NamePipeServerChannel : NamePipeChannel
    {
        public NamePipeServerChannel(IOptions<NamePipeConfig> options)
            : base(options)
        {

        }

        protected new NamedPipeServerStream PipeStream
        {
            get
            {
                return base.PipeStream as NamedPipeServerStream;
            }
        }

        protected override PipeStream CreatePipeStream(NamePipeConfig config)
        {
            return new NamedPipeServerStream(config.PipeName, PipeDirection.InOut);
        }
    }
}