using NRPC.Abstractions;

namespace NRPC.Client
{
    public interface IRpcConnectionFactory
    {
        IRpcConnection CreateConnection();
    }
}