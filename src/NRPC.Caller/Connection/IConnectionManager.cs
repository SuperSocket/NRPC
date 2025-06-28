using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public interface IConnectionManager<TConnection> : IDisposable, IAsyncDisposable
        where TConnection : IDisposable, IAsyncDisposable
    {
        Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default);
        
        void ReturnConnection(TConnection connection);
    }
}