
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller.Connection
{
    public class RpcConnectionObjectPolicy : IAsyncPooledObjectPolicy<IRpcConnection>, IInvokeStateManager
    {
        private readonly IRpcConnectionFactory _connectionFactory;

        private ConcurrentDictionary<string, InvokeState> _invokeStates = new ConcurrentDictionary<string, InvokeState>(StringComparer.OrdinalIgnoreCase);

        public RpcConnectionObjectPolicy(IRpcConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public virtual async Task<IRpcConnection> CreateAsync(CancellationToken cancellationToken = default)
        {
            var connection = await _connectionFactory
                .CreateConnection(cancellationToken)
                .ConfigureAwait(false);

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

        public virtual bool Return(IRpcConnection obj)
        {
            return true;
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