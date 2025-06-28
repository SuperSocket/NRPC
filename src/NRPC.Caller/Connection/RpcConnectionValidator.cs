using System;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller.Connection
{
    internal class RpcConnectionValidator : IConnectionValidator<IRpcConnection>
    {
        public static readonly IConnectionValidator<IRpcConnection> Instance = new RpcConnectionValidator();

        private RpcConnectionValidator()
        {
        }
        
        public bool Validate(IRpcConnection connection)
        {
            return connection != null && connection.IsConnected;
        }
    }
}