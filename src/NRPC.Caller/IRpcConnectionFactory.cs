using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller
{
    public interface IRpcConnectionFactory
    {
        Task<IRpcConnection> CreateConnection(CancellationToken cancellationToken = default);
    }
}