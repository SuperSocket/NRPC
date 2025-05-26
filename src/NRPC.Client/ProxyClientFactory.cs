using System;
using System.Threading.Tasks;
using NRPC.Proxy;

namespace NRPC.Client
{
    public class ProxyClientFactory<T, TClientDispatchProxy> : IClientFactory<T>
        where TClientDispatchProxy : ClientDispatchProxy
    {
        private IRpcConnectionFactory m_ConnectionFactory;

        public ProxyClientFactory(IRpcConnectionFactory connectionFactory)
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

    public class ProxyClientFactory<T> : ProxyClientFactory<T, ClientDispatchProxy>
        where T : class
    {
        public ProxyClientFactory(IRpcConnectionFactory connectionFactory)
            : base(connectionFactory)
        {
        }
    }
}