using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public interface IAsyncObjectPool<T> : IDisposable
    {
        ValueTask<T> GetAsync(CancellationToken cancellationToken = default);
        
        void Return(T item);
    }
}