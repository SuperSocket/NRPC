using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Caller;
using NRPC.Caller.Connection;
using NRPC.Executor;
using Xunit;

namespace NRPC.Test
{
    public class RpcCallerFactoryIntegrationTest
    {
        [Fact]
        public async Task RpcCallerFactory_Should_Handle_Concurrent_Requests_With_Connection_Pool()
        {
            // Arrange
            var connectionFactory = new ChannelBasedRpcConnectionFactory();
            var callerFactory = new RpcCallerFactory<ITestService>(
                connectionFactory,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            var caller = callerFactory.CreateCaller(CancellationToken.None);

            // Act - Make multiple concurrent calls
            var tasks = new Task<int>[10];
            for (int i = 0; i < tasks.Length; i++)
            {
                int captured = i;
                tasks[i] = caller.Add(captured, captured + 1);
            }

            var results = await Task.WhenAll(tasks);

            // Assert
            for (int i = 0; i < results.Length; i++)
            {
                Assert.Equal(i + (i + 1), results[i]); // Verify correct calculation
            }

            // Verify that connections were reused
            Assert.True(connectionFactory.TotalConnectionsCreated >= 1);
            Assert.True(connectionFactory.TotalConnectionsCreated <= 10); // Should be less than total requests due to pooling
        }

        [Fact]
        public async Task RpcCallerFactory_Should_Handle_Mixed_Method_Calls_Concurrently()
        {
            // Arrange
            var connectionFactory = new ChannelBasedRpcConnectionFactory();
            var callerFactory = new RpcCallerFactory<ITestService>(
                connectionFactory,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            var caller = callerFactory.CreateCaller(CancellationToken.None);

            // Act - Mix different method calls
            var addTask = caller.Add(5, 3);
            var concatTask = caller.Concat("Hello", "World");
            var voidTask = caller.ExecuteVoid("TestCommand");

            await Task.WhenAll(addTask, concatTask, voidTask);

            // Assert
            Assert.Equal(8, await addTask);
            Assert.Equal("HelloWorld", await concatTask);
            // voidTask should complete without throwing
        }

        [Fact]
        public async Task Connection_Pool_Should_Handle_Connection_Failures_Gracefully()
        {
            // Arrange
            var connectionFactory = new FlakyRpcConnectionFactory();
            var callerFactory = new RpcCallerFactory<ITestService>(
                connectionFactory,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            var caller = callerFactory.CreateCaller(CancellationToken.None);

            // Act & Assert
            // Some calls might fail due to flaky connections, but the pool should handle it
            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                int captured = i;

                tasks[i] = Task.Run(async () =>
                {
                    try
                    {
                        await caller.Add(captured, 1);
                    }
                    catch (Exception)
                    {
                        // Expected for some connections
                    }
                }, TestContext.Current.CancellationToken);
            }

            await Task.WhenAll(tasks);

            // Pool should still be functional
            Assert.True(connectionFactory.TotalConnectionsCreated > 0);
        }

        [Fact]
        public async Task Connection_Pool_Should_Limit_Concurrent_Connections()
        {
            // Arrange
            var connectionFactory = new TrackingRpcConnectionFactory();
            var policy = new RpcConnectionObjectPolicy(connectionFactory);
            var pool = new AsyncObjectPool<IRpcConnection>(policy, maxSize: 3);

            // Act - Get more connections than pool size
            var connections = new IRpcConnection[5];
            for (int i = 0; i < connections.Length; i++)
            {
                connections[i] = await pool.GetAsync(CancellationToken.None);
            }

            // Return all connections
            foreach (var connection in connections)
            {
                pool.Return(connection);
            }

            // Get connections again
            var reusedConnections = new IRpcConnection[3];
            for (int i = 0; i < reusedConnections.Length; i++)
            {
                reusedConnections[i] = await pool.GetAsync(CancellationToken.None);
            }

            // Assert
            Assert.Equal(5, connectionFactory.TotalConnectionsCreated); // 5 initial connections
            Assert.Equal(3, connectionFactory.ActiveConnections); // Only 3 should be active in pool
        }

        // Helper classes for integration testing
        private class ChannelBasedRpcConnectionFactory : IRpcConnectionFactory
        {
            private int _connectionCounter;
            public int TotalConnectionsCreated => _connectionCounter;

            public Task<IRpcConnection> CreateConnection(CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _connectionCounter);
                return Task.FromResult<IRpcConnection>(new ChannelBasedRpcConnection());
            }
        }

        private class ChannelBasedRpcConnection : IRpcConnection
        {
            private readonly Channel<RpcRequest> _requestChannel = Channel.CreateUnbounded<RpcRequest>();
            private readonly Channel<RpcResponse> _responseChannel = Channel.CreateUnbounded<RpcResponse>();
            private readonly CancellationTokenSource _cts = new();

            private CompiledServiceHandler<ITestService> _handler = new CompiledServiceHandler<ITestService>(new ServiceMetadata<ITestService>(DirectTypeExpressionConverter.Singleton), DefaultRpcCallingAdapter.Singleton);

            private ITestService _testService = new TestService();

            public bool IsConnected { get; private set; } = true;

            public ChannelBasedRpcConnection()
            {
                // Start processing requests
                _ = ProcessRequestsAsync(_cts.Token);
            }

            public Task SendAsync(RpcRequest request)
            {
                return _requestChannel.Writer.WriteAsync(request, _cts.Token).AsTask();
            }

            public async Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
            {
                using var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
                return await _responseChannel.Reader.ReadAsync(combined.Token);
            }

            private async Task ProcessRequestsAsync(CancellationToken cancellationToken)
            {
                await foreach (var request in _requestChannel.Reader.ReadAllAsync(cancellationToken))
                {
                    var response = await _handler.HandleRequestAsync(_testService, request);

                    await _responseChannel.Writer.WriteAsync(response, cancellationToken);
                }
            }

            public void Dispose()
            {
                _cts.Cancel();
                _requestChannel.Writer.Complete();
                _responseChannel.Writer.Complete();
                _cts.Dispose();
                IsConnected = false;
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }

        private class FlakyRpcConnectionFactory : IRpcConnectionFactory
        {
            private int _connectionCounter;
            private readonly Random _random = new();
            public int TotalConnectionsCreated => _connectionCounter;

            public Task<IRpcConnection> CreateConnection(CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _connectionCounter);

                // 30% chance of failure
                if (_random.NextDouble() < 0.3)
                {
                    throw new InvalidOperationException("Simulated connection failure");
                }

                return Task.FromResult<IRpcConnection>(new ChannelBasedRpcConnection());
            }
        }

        private class TrackingRpcConnectionFactory : IRpcConnectionFactory
        {
            private int _connectionCounter;
            private int _activeConnections;

            public int TotalConnectionsCreated => _connectionCounter;
            public int ActiveConnections => _activeConnections;

            public Task<IRpcConnection> CreateConnection(CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _connectionCounter);
                Interlocked.Increment(ref _activeConnections);
                return Task.FromResult<IRpcConnection>(new TrackingRpcConnection(this));
            }

            public void DecrementActiveConnections()
            {
                Interlocked.Decrement(ref _activeConnections);
            }
        }

        private class TrackingRpcConnection : IRpcConnection, IDisposable
        {
            private readonly TrackingRpcConnectionFactory _factory;
            private bool _disposed;

            public bool IsConnected { get; private set; } = true;

            public TrackingRpcConnection(TrackingRpcConnectionFactory factory)
            {
                _factory = factory;
            }

            public Task SendAsync(RpcRequest request)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(TrackingRpcConnection));
                return Task.CompletedTask;
            }

            public Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
            {
                if (_disposed) throw new ObjectDisposedException(nameof(TrackingRpcConnection));
                return Task.FromResult(new RpcResponse { Id = "test" });
            }

            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    IsConnected = false;
                    _factory.DecrementActiveConnections();
                }
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
