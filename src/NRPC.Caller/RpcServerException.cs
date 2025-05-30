using System;
using NRPC.Abstractions;

namespace NRPC.Caller
{
    /// <summary>
    /// Exception thrown when an RPC server returns an error response.
    /// </summary>
    public class RpcServerException
        : RpcException
    {
        public RpcError ServerError { get; }

        public RpcServerException(string message, RpcError serverError)
            : base(message)
        {
            ServerError = serverError;
        }
    }
}