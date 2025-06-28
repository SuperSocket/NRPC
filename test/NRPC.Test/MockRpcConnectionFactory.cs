using System;
using NRPC.Abstractions;
using NRPC.Caller.Connection;

namespace NRPC.Test
{
    internal class MockRpcConnectionFactory : IRpcConnectionFactory
    {
        private readonly MockRpcConnection mockRpcConnection;

        public MockRpcConnectionFactory()
           : this(new MockRpcConnection())
        {
        }

        public MockRpcConnectionFactory(MockRpcConnection mockRpcConnection)
        {
            this.mockRpcConnection = mockRpcConnection;
        }

        public Task<IRpcConnection> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<IRpcConnection>(mockRpcConnection);
        }
    }
}