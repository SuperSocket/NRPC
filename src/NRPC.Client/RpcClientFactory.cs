using System;
using System.Threading.Tasks;
using NRPC.Proxy;

namespace NRPC.Client
{
    public class RpcClientFactory<T, TClientDispatchProxy> : IClientFactory<T>
        where TClientDispatchProxy : ClientDispatchProxy
    {
        private IRpcConnectionFactory m_ConnectionFactory;

        public RpcClientFactory(IRpcConnectionFactory connectionFactory)
        {
            m_ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<T> CreateClient()
        {
            var proxyInstance = RpcProxy.Create<T, TClientDispatchProxy>();
            var rpcConnection = await m_ConnectionFactory.CreateConnection();
            (proxyInstance as ClientDispatchProxy).Initialize(rpcConnection);
            return (T)proxyInstance;
        }
    }

    public class RpcClientFactory<T> : RpcClientFactory<T, ClientDispatchProxy>
        where T : class
    {
        public RpcClientFactory(IRpcConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}