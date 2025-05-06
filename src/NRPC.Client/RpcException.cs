using System;
using NRPC.Abstractions;

namespace NRPC.Client
{
    public class RpcException : Exception
    {
        public RpcError ServerError { get; }

        public RpcException(string message, RpcError serverError)
            : base(message)
        {
            this.ServerError = serverError;
        }
    }
}