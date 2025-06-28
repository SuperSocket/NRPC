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
        private IConnectionManager<IRpcConnection> m_ConnectionManager;

        private IInvokeStateManager m_InvokeStateManager;

        private IRpcCallingAdapter m_RpcCallingAdapter;

        private IExpressionConverter m_ResultExpressionConverter;

        private bool m_Disposed = false;

        public RpcCallerFactory(IRpcConnectionFactory connectionFactory, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter expressionConverter)
            : this(new ConnectionFactory<IRpcConnection>(connectionFactory), rpcCallingAdapter, expressionConverter)
        {

        }

        internal RpcCallerFactory(ConnectionFactory<IRpcConnection> connectionFactory, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter expressionConverter)
            : this(new SingleConnectionManager<IRpcConnection>(connectionFactory), connectionFactory, rpcCallingAdapter, expressionConverter)
        {

        }

        internal RpcCallerFactory(IConnectionManager<IRpcConnection> connectionManager, IInvokeStateManager invokeStateManager, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter expressionConverter)
        {
            m_ConnectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            m_InvokeStateManager = invokeStateManager ?? throw new ArgumentNullException(nameof(invokeStateManager));
            m_RpcCallingAdapter = rpcCallingAdapter ?? throw new ArgumentNullException(nameof(rpcCallingAdapter));
            m_ResultExpressionConverter = expressionConverter ?? throw new ArgumentNullException(nameof(expressionConverter));
        }

        public T CreateCaller()
        {
            if (m_Disposed)
                throw new ObjectDisposedException(nameof(RpcCallerFactory<T, TClientDispatchProxy>));

            var proxyInstance = RpcProxy.Create<T, TClientDispatchProxy>();
            (proxyInstance as CallerDispatchProxy).Initialize(m_ConnectionManager, m_InvokeStateManager, m_RpcCallingAdapter, m_ResultExpressionConverter);
            return (T)proxyInstance;
        }

        public void Dispose()
        {
            if (m_Disposed)
                return;

            m_Disposed = true;

            if (m_ConnectionManager is IDisposable disposablePool)
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

            m_ConnectionManager = null;
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

            if (m_ConnectionManager is IAsyncDisposable asyncDisposablePool)
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

            m_ConnectionManager = null;
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