using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller.Connection
{
    public class ConnectionManager<TConnection> : IConnectionManager<TConnection>, IInvokeStateManager
        where TConnection : class, IDisposable, IAsyncDisposable, IRpcConnection
    {
        private readonly IConnectionManager<TConnection> _connectionManager;

        private ConcurrentDictionary<string, InvokeState> _invokeStates = new ConcurrentDictionary<string, InvokeState>(StringComparer.OrdinalIgnoreCase);

        public ConnectionManager(IConnectionManager<TConnection> connectionManager)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
        }

        public void Dispose()
        {
            _connectionManager.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            return _connectionManager.DisposeAsync();
        }

        public Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            return _connectionManager.GetConnectionAsync(cancellationToken);
        }

        public void ReturnConnection(TConnection connection)
        {
            _connectionManager.ReturnConnection(connection);
        }

        private async Task ReadResponseAsync(IRpcConnection connection, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var rpcResponse = await connection.ReceiveAsync(cancellationToken);

                if (rpcResponse == null)
                    continue;

                if (!_invokeStates.TryRemove(rpcResponse.Id, out var invokeState))
                    return;

                invokeState.ResponseHandler.HandleResponse(invokeState.TaskCompletionSource, rpcResponse);
            }
        }

        bool IInvokeStateManager.TrySaveInvokeState(string requestId, InvokeState invokeState)
        {
            return _invokeStates.TryAdd(requestId, invokeState);
        }
    }
}