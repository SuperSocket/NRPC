using System;
using NRPC.Abstractions;

namespace NRPC.Caller
{
    public class RpcClientException : RpcException
    {
        public RpcClientException(string message, Exception exception)
            : base(message, exception)    
        {
        }
    }
}