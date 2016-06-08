using System;
using System.Threading.Tasks;

namespace NRPC.Base
{
    /// <summary>
    /// The interface for the RPC Server channel listener
    /// </summary>
    public interface IRpcServerChannelListener
    {
        /// <summary>
        /// Listen to handle the requests from client
        /// </summary>
        Task<IRpcChannel> ListenAsync();
    }
}