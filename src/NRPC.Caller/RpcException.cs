using System;
using NRPC.Abstractions;

namespace NRPC.Caller
{
    public class RpcException : Exception
    {
        public RpcException(string message)
            : base(message)
        {
        }

        public RpcException(string message, Exception exception)
            : base(message, exception)
        {
        }
    }
}