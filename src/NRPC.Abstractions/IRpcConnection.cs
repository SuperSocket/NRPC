using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Abstractions
{
    /// <summary>
    /// The interface for RPC connection
    /// </summary>
    public interface IRpcConnection
    {

        Task SendAsync(RpcRequest request);        

        Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default);
    }
}