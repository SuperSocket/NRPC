using System;
using NRPC.Abstractions;

namespace NRPC.Caller
{
    public class RpcClientException : RpcException
    {
        public RpcClientException(string message, RpcError serverError)
            : base(message, serverError)
        {
        }
    }
}