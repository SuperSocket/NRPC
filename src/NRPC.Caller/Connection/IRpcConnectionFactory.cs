using NRPC.Abstractions;

namespace NRPC.Caller.Connection
{
    public interface IRpcConnectionFactory : IConnectionFactory<IRpcConnection>
    {
    }
}