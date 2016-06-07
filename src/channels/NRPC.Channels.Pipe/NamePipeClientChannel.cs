
using System;
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
    }
}