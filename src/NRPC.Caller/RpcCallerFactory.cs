using System;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Caller.Connection;
using NRPC.Proxy;

namespace NRPC.Caller
{
    public class RpcCallerFactory<T, TClientDispatchProxy> : ICallerFactory<T>
        where TClientDispatchProxy : CallerDispatchProxy
    {
        private IAsyncObjectPool<IRpcConnection> m_ConnectionPool;

        private IInvokeStateManager m_InvokeStateManager;

        private IRpcCallingAdapter m_RpcCallingAdapter;

        private IExpressionConverter m_ResultExpressionConverter;

        private bool m_Disposed = false;

        public RpcCallerFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter expressionConverter)
            : this(new RpcConnectionObjectPolicy(connectionFactory), rpcCallingAdapter, expressionConverter)
        {

        }

        internal RpcCallerFactory(IAsyncPooledObjectPolicy<IRpcConnection> connectionPoolPolicy, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter expressionConverter)
            : this(new AsyncObjectPool<IRpcConnection>(connectionPoolPolicy), connectionPoolPolicy as IInvokeStateManager, rpcCallingAdapter, expressionConverter)
        {

        }

        internal RpcCallerFactory(IAsyncObjectPool<IRpcConnection> connectionPool, IInvokeStateManager invokeStateManager, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter expressionConverter)
        {
            m_ConnectionPool = connectionPool ?? throw new ArgumentNullException(nameof(connectionPool));
            m_InvokeStateManager = invokeStateManager ?? throw new ArgumentNullException(nameof(invokeStateManager));
            m_RpcCallingAdapter = rpcCallingAdapter ?? throw new ArgumentNullException(nameof(rpcCallingAdapter));
            m_ResultExpressionConverter = expressionConverter ?? throw new ArgumentNullException(nameof(expressionConverter));
        }

        public T CreateCaller()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(RpcCallerFactory<T, TClientDispatchProxy>));

            var proxyInstance = RpcProxy.Create<T, TClientDispatchProxy>();
            (proxyInstance as CallerDispatchProxy).Initialize(m_ConnectionPool, m_InvokeStateManager, m_RpcCallingAdapter, m_ResultExpressionConverter);
            return (T)proxyInstance;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            if (m_ConnectionPool is IDisposable disposablePool)
            {
                try
                {
                    disposablePool.Dispose();
                }
                catch (Exception)
                {
                    // Log or handle the exception as needed
                }
            }

            m_ConnectionPool = null;
            m_InvokeStateManager = null;
            m_RpcCallingAdapter = null;
            m_ResultExpressionConverter = null;

            GC.SuppressFinalize(this);
        }

        public ValueTask DisposeAsync()
        {
            if (m_Disposed)
                return ValueTask.CompletedTask;

            m_Disposed = true;

            if (m_ConnectionPool is IAsyncDisposable asyncDisposablePool)
            {
                try
                {
                    return asyncDisposablePool.DisposeAsync();
                }
                catch (Exception)
                {
                    // Log or handle the exception as needed
                }
            }

            m_ConnectionPool = null;
            m_InvokeStateManager = null;
            m_RpcCallingAdapter = null;
            m_ResultExpressionConverter = null;

            GC.SuppressFinalize(this);
            return ValueTask.CompletedTask;
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