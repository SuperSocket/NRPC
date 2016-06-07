using System;
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
        Task SendAsync(ArraySegment<byte> data);
        
        /// <summary>
        /// Receive data from the channnel
        /// </summary>
        /// <returns>A task that represents the asynchronous receive operation. The task result is the received binary data.</returns>
        Task<ArraySegment<byte>> ReceiveAsync();
    }
}