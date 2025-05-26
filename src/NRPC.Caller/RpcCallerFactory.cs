using System;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Proxy;

namespace NRPC.Caller
{
    public class RpcCallerFactory<T, TClientDispatchProxy> : ICallerFactory<T>
        where TClientDispatchProxy : CallerDispatchProxy
    {
        private IRpcConnectionFactory m_ConnectionFactory;

        private IRpcCallingAdapter m_RpcCallingAdapter;

        public RpcCallerFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter)
        {
            m_ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            m_RpcCallingAdapter = rpcCallingAdapter ?? throw new ArgumentNullException(nameof(rpcCallingAdapter));
        }

        public async Task<T> CreateCaller()
        {
            var proxyInstance = RpcProxy.Create<T, TClientDispatchProxy>();
            var rpcConnection = await m_ConnectionFactory.CreateConnection();
            (proxyInstance as CallerDispatchProxy).Initialize(rpcConnection, m_RpcCallingAdapter);
            return (T)proxyInstance;
        }
    }

    public class RpcCallerFactory<T> : RpcCallerFactory<T, CallerDispatchProxy>
        where T : class
    {
        public RpcCallerFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter = null)
            : base(connectionFactory, rpcCallingAdapter ?? DefaultRpcCallingAdapter.Singleton)
        {
        }
    }
}