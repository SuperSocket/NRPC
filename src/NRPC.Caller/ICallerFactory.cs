using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller
{
    public interface ICallerFactory
    {
    }
    
    public interface ICallerFactory<T> : ICallerFactory
    {
        T CreateCaller(CancellationToken cancellationToken = default);
    }
}