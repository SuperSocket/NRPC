using System;
using System.Threading;

namespace NRPC.Caller
{
    public interface ICallerFactory : IDisposable, IAsyncDisposable
    {
    }
    
    public interface ICallerFactory<T> : ICallerFactory
    {
        T CreateCaller(CancellationToken cancellationToken = default);
    }
}