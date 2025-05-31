using System;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Proxy;

namespace NRPC.Caller
{
    public class RpcCallerFactory<T, TClientDispatchProxy> : ICallerFactory<T>
        where TClientDispatchProxy : CallerDispatchProxy
    {
        private IRpcConnectionFactory m_ConnectionFactory;

        private IRpcCallingAdapter m_RpcCallingAdapter;

        private IExpressionConverter m_ResultExpressionConverter;

        public RpcCallerFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter expressionConverter)
        {
            m_ConnectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            m_RpcCallingAdapter = rpcCallingAdapter ?? throw new ArgumentNullException(nameof(rpcCallingAdapter));
            m_ResultExpressionConverter = expressionConverter ?? throw new ArgumentNullException(nameof(expressionConverter));
        }

        public async Task<T> CreateCaller(CancellationToken cancellationToken)
        {
            var proxyInstance = RpcProxy.Create<T, TClientDispatchProxy>();
            var rpcConnection = await m_ConnectionFactory.CreateConnection(cancellationToken).ConfigureAwait(false);
            (proxyInstance as CallerDispatchProxy).Initialize(rpcConnection, m_RpcCallingAdapter, m_ResultExpressionConverter);
            return (T)proxyInstance;
        }
    }

    public class RpcCallerFactory<T> : RpcCallerFactory<T, CallerDispatchProxy>
        where T : class
    {
        public RpcCallerFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter = null, IExpressionConverter expressionConverter = null)
            : base(connectionFactory, rpcCallingAdapter ?? DefaultRpcCallingAdapter.Singleton, expressionConverter ?? DirectTypeExpressionConverter.Singleton)
        {
        }
    }
}