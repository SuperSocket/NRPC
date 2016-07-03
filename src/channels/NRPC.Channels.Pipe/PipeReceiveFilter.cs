using System;
using SuperSocket.ProtoBase;

namespace NRPC.Channels.Pipe
{
    public class PipeReceiveFilter : FixedHeaderReceiveFilter<PipePackageInfo>
    {
        public PipeReceiveFilter()
            : base(2)
        {

        }

        public override PipePackageInfo ResolvePackage(IBufferStream bufferStream)
        {
            bufferStream.Skip(2);
            return new PipePackageInfo { Data = bufferStream.Buffers  };
        }

        protected override int GetBodyLengthFromHeader(IBufferStream bufferStream, int length)
        {
            return bufferStream.ReadInt16();
        }
    }
}