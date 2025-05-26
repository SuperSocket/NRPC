using System.Threading.Tasks;

namespace NRPC.Client
{
    public interface IClientFactory
    {
    }
    
    public interface IClientFactory<T> : IClientFactory
    {
        Task<T> CreateClient();
    }
}