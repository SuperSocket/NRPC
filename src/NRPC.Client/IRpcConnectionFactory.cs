using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Client
{
    public interface IRpcConnectionFactory
    {
        Task<IRpcConnection> CreateConnection();
    }
}