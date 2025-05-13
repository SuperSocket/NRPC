using System;
using NRPC.Proxy;

namespace NRPC.Client
{
    public class ProxyClientFactory<T> : IClientFactory<T>
    {
        private IRpcConnectionFactory m_ConnectionFactory;

        public ProxyClientFactory(IRpcConnectionFactory connectionFactory)
        {
            m_ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public T CreateClient()
        {
            var proxyInstance = RpcProxy.Create<T, ClientDispatchProxy>();
            var rpcConnection = m_ConnectionFactory.CreateConnection();
            (proxyInstance as ClientDispatchProxy).Initialize(rpcConnection);
            return (T)proxyInstance;
        }
    }
}