using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Client
{
    public interface ICallerFactory
    {
    }
    
    public interface ICallerFactory<T> : ICallerFactory
    {
        Task<T> CreateCaller();
    }
}