using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public interface IAsyncPooledObjectPolicy<T>
    {
        Task<T> CreateAsync(CancellationToken cancellationToken = default);

        bool Return(T obj);
    }
}