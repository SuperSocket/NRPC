using System;
using System.Threading;
using System.Threading.Tasks;

/*
namespace NRPC.Caller.Connection
{
    internal interface IConnectionPool<TConnection> : IDisposable, IAsyncDisposable
        where TConnection : IDisposable, IAsyncDisposable
    {
        Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default);

        void ReturnConnection(TConnection connection);

        int AvailableConnections { get; }

        int TotalConnections { get; }
    }
}
*/