using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NRPC.Base
{
    /// <summary>
    /// The interface for RPC channel
    /// </summary>
    public interface IRpcChannel
    {
        /// <summary>
        /// Send the data through the channel
        /// </summary>
        /// <param name="data"></param>
        /// <returns>A task that represents the asynchronous send operation</returns>
        Task SendAsync(IList<ArraySegment<byte>> data);
        

        /// <summary>
        /// event for new package info is received from the channel
        /// </summary>
        event Action<RpcChannelPackageInfo> NewPackageReceived;
    }
}