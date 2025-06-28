using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller.Connection
{
    internal class ConnectionFactory<TConnection> : IConnectionFactory<TConnection>, IInvokeStateManager
        where TConnection : IDisposable, IAsyncDisposable, IRpcConnection
    {
        private readonly IConnectionFactory<TConnection> _connectionFactory;

        private ConcurrentDictionary<string, InvokeState> _invokeStates = new ConcurrentDictionary<string, InvokeState>(StringComparer.OrdinalIgnoreCase);

        public ConnectionFactory(IConnectionFactory<TConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public async Task<TConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
        {
            var connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            _ = ReadResponseAsync(connection, cancellationToken)
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        // Handle the error, e.g., log it
                        Console.WriteLine($"Error reading response: {t.Exception?.GetBaseException().Message}");
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);

            return connection;
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
        
        public bool TrySaveInvokeState(string requestId, InvokeState invokeState)
        {
            return _invokeStates.TryAdd(requestId, invokeState);
        }
    }
}