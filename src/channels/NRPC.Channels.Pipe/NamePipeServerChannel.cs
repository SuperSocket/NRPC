
using System;
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
    }
}