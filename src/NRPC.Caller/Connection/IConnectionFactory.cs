using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public interface IConnectionFactory<TConnection>
        where TConnection : IDisposable, IAsyncDisposable
    {
        Task<TConnection> CreateConnectionAsync(CancellationToken cancellationToken = default);
    }
}