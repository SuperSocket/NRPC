using System;
using System.Buffers;
using SuperSocket.ProtoBase;

namespace NRPC.Channels.Pipe
{
    public class PipePipelineFilter : FixedHeaderPipelineFilter<PipePackageInfo>
    {
        public PipePipelineFilter()
            : base(2)
        {

        }

        protected override int GetBodyLengthFromHeader(ref ReadOnlySequence<byte> buffer)
        {
            var sequnceReader = new SequenceReader<byte>(buffer);
            sequnceReader.TryReadBigEndian(out short length);
            return length;
        }
    }
}