using System;
using System.Collections.Generic;

namespace NRPC.Base
{
    public class RpcChannelPackageInfo
    {
        public IList<ArraySegment<byte>> Data { get; set; }
    }
}