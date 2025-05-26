using System;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Proxy;

namespace NRPC.Client
{
    public class RpcClientFactory<T, TClientDispatchProxy> : IClientFactory<T>
        where TClientDispatchProxy : ClientDispatchProxy
    {
        private IRpcConnectionFactory m_ConnectionFactory;

        private IRpcCallingAdapter m_RpcCallingAdapter;

        public RpcClientFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter)
        {
            m_ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            m_RpcCallingAdapter = rpcCallingAdapter ?? throw new ArgumentNullException(nameof(rpcCallingAdapter));
        }

        public async Task<T> CreateClient()
        {
            var proxyInstance = RpcProxy.Create<T, TClientDispatchProxy>();
            var rpcConnection = await m_ConnectionFactory.CreateConnection();
            (proxyInstance as ClientDispatchProxy).Initialize(rpcConnection, m_RpcCallingAdapter);
            return (T)proxyInstance;
        }
    }

    public class RpcClientFactory<T> : RpcClientFactory<T, ClientDispatchProxy>
        where T : class
    {
        public RpcClientFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter = null)
            : base(connectionFactory, rpcCallingAdapter ?? DefaultRpcCallingAdapter.Singleton)
        {
        }
    }
}