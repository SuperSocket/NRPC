using System;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Caller;
using NRPC.Caller.Connection;
using Xunit;

namespace NRPC.Test
{
    public class CallerFactoryDisposalTest
    {
        [ServiceContractAtribute]
        public interface ITestCallerService
        {
            Task<int> Add(int x, int y);
            Task<string> GetMessage(string input);
        }

        [Fact]
        public void CallerFactory_Dispose_Should_Prevent_Further_Caller_Creation()
        {
            // Arrange
            var connectionFactory = new TestRpcConnectionFactory();
            var factory = new RpcCallerFactory<ITestCallerService>(connectionFactory);

            // Create a caller to verify factory works initially
            var caller = factory.CreateCaller();
            Assert.NotNull(caller);

            // Act
            factory.Dispose();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public async Task CallerFactory_DisposeAsync_Should_Prevent_Further_Caller_Creation()
        {
            // Arrange
            var connectionFactory = new TestRpcConnectionFactory();
            var factory = new RpcCallerFactory<ITestCallerService>(connectionFactory);

            // Create a caller to verify factory works initially
            var caller = factory.CreateCaller();
            Assert.NotNull(caller);

            // Act
            await factory.DisposeAsync();

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public void CallerFactory_Multiple_Dispose_Calls_Should_Be_Safe()
        {
            // Arrange
            var connectionFactory = new TestRpcConnectionFactory();
            var factory = new RpcCallerFactory<ITestCallerService>(connectionFactory);

            // Act
            factory.Dispose();
            factory.Dispose(); // Second call should be safe

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public async Task CallerFactory_Mixed_Disposal_Calls_Should_Be_Safe()
        {
            // Arrange
            var connectionFactory = new TestRpcConnectionFactory();
            var factory = new RpcCallerFactory<ITestCallerService>(connectionFactory);

            // Act
            factory.Dispose();
            await factory.DisposeAsync(); // Mixed calls should be safe

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public async Task CallerFactory_DisposeAsync_Then_Dispose_Should_Be_Safe()
        {
            // Arrange
            var connectionFactory = new TestRpcConnectionFactory();
            var factory = new RpcCallerFactory<ITestCallerService>(connectionFactory);

            // Act
            await factory.DisposeAsync();
            factory.Dispose(); // Should be safe after async disposal

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public void CallerFactory_Should_Dispose_Connection_Pool_On_Dispose()
        {
            // Arrange
            var disposablePool = new DisposableConnectionManager();
            var invokeStateManager = new TestInvokeStateManager();
            var factory = new RpcCallerFactory<ITestCallerService, CallerDispatchProxy>(
                disposablePool, 
                invokeStateManager, 
                DefaultRpcCallingAdapter.Singleton, 
                DirectTypeExpressionConverter.Singleton);

            // Act
            factory.Dispose();

            // Assert
            Assert.True(disposablePool.IsDisposed, "Connection pool should be disposed");
        }

        [Fact]
        public async Task CallerFactory_Should_Dispose_Connection_Pool_On_DisposeAsync()
        {
            // Arrange
            var asyncDisposablePool = new AsyncDisposableConnectionManager();
            var invokeStateManager = new TestInvokeStateManager();
            var factory = new RpcCallerFactory<ITestCallerService, CallerDispatchProxy>(
                asyncDisposablePool,
                invokeStateManager,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            // Act
            await factory.DisposeAsync();

            // Assert
            Assert.True(asyncDisposablePool.IsDisposed, "Connection pool should be disposed");
            Assert.True(asyncDisposablePool.DisposeAsyncCalled, "DisposeAsync should have been called");
        }

        [Fact]
        public void CallerFactory_Should_Handle_Connection_Pool_Disposal_Exceptions()
        {
            // Arrange
            var faultyPool = new FaultyDisposableConnectionManager();
            var invokeStateManager = new TestInvokeStateManager();
            var factory = new RpcCallerFactory<ITestCallerService, CallerDispatchProxy>(
                faultyPool,
                invokeStateManager,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            // Act & Assert
            // Should not throw even if pool disposal fails
            factory.Dispose();

            // Factory should still be marked as disposed
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public async Task CallerFactory_Should_Handle_Connection_Pool_Async_Disposal_Exceptions()
        {
            // Arrange
            var faultyAsyncPool = new FaultyAsyncDisposableConnectionManager();
            var invokeStateManager = new TestInvokeStateManager();
            var factory = new RpcCallerFactory<ITestCallerService, CallerDispatchProxy>(
                faultyAsyncPool,
                invokeStateManager,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            // Act & Assert
            // Should not throw even if pool async disposal fails
            await factory.DisposeAsync();

            // Factory should still be marked as disposed
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public void CallerFactory_Dispose_Should_Clear_References()
        {
            // Arrange
            var connectionFactory = new TestRpcConnectionFactory();
            var factory = new RpcCallerFactory<ITestCallerService>(connectionFactory);

            // Act
            factory.Dispose();

            // Assert
            // We can't directly test if references are cleared (they're private)
            // but we can ensure the factory behaves correctly after disposal
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public async Task CallerFactory_DisposeAsync_Should_Clear_References()
        {
            // Arrange
            var connectionFactory = new TestRpcConnectionFactory();
            var factory = new RpcCallerFactory<ITestCallerService>(connectionFactory);

            // Act
            await factory.DisposeAsync();

            // Assert
            // We can't directly test if references are cleared (they're private)
            // but we can ensure the factory behaves correctly after disposal
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public void CallerFactory_Should_Support_Using_Statement()
        {
            // Arrange & Act
            RpcCallerFactory<ITestCallerService> factory;
            using (factory = new RpcCallerFactory<ITestCallerService>(new TestRpcConnectionFactory()))
            {
                var caller = factory.CreateCaller();
                Assert.NotNull(caller);
            }

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        [Fact]
        public async Task CallerFactory_Should_Support_Await_Using_Statement()
        {
            // Arrange & Act
            RpcCallerFactory<ITestCallerService> factory;
            await using (factory = new RpcCallerFactory<ITestCallerService>(new TestRpcConnectionFactory()))
            {
                var caller = factory.CreateCaller();
                Assert.NotNull(caller);
            }

            // Assert
            Assert.Throws<ObjectDisposedException>(() => factory.CreateCaller());
        }

        // Helper classes for testing
        private class TestRpcConnectionFactory : IRpcConnectionFactory
        {
            public Task<IRpcConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IRpcConnection>(new TestRpcConnection());
            }
        }

        private class TestRpcConnection : IRpcConnection
        {
            public bool IsConnected { get; private set; } = true;

            public Task SendAsync(RpcRequest request)
            {
                return Task.CompletedTask;
            }

            public Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new RpcResponse { Id = "test", Result = "test" });
            }

            public void Dispose()
            {
                IsConnected = false;
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }
        }

        private class TestInvokeStateManager : IInvokeStateManager
        {
            public bool TrySaveInvokeState(string requestId, InvokeState invokeState)
            {
                // For testing, we can just return true to simulate saving state
                return true;
            }

            public Task HandleResponseAsync(RpcResponse response)
            {
                return Task.CompletedTask;
            }
        }

        private class DisposableConnectionManager : IConnectionManager<IRpcConnection>, IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public Task<IRpcConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IRpcConnection>(new TestRpcConnection());
            }

            public void ReturnConnection(IRpcConnection connection)
            {
            }
        }

        private class AsyncDisposableConnectionManager : IConnectionManager<IRpcConnection>, IAsyncDisposable, IDisposable
        {
            public bool IsDisposed { get; private set; }
            public bool DisposeAsyncCalled { get; private set; }

            public ValueTask DisposeAsync()
            {
                DisposeAsyncCalled = true;
                IsDisposed = true;
                return ValueTask.CompletedTask;
            }

            public void Dispose()
            {
                IsDisposed = true;
            }

            public Task<IRpcConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IRpcConnection>(new TestRpcConnection());
            }

            public void ReturnConnection(IRpcConnection connection)
            {
            }
        }

        private class FaultyDisposableConnectionManager : IConnectionManager<IRpcConnection>, IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
                throw new InvalidOperationException("Simulated disposal failure");
            }

            public ValueTask DisposeAsync()
            {
                Dispose();
                return ValueTask.CompletedTask;
            }

            public Task<IRpcConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IRpcConnection>(new TestRpcConnection());
            }

            public void ReturnConnection(IRpcConnection connection)
            {
            }
        }

        private class FaultyAsyncDisposableConnectionManager : IConnectionManager<IRpcConnection>, IAsyncDisposable, IDisposable
        {
            public bool IsDisposed { get; private set; }

            public ValueTask DisposeAsync()
            {
                IsDisposed = true;
                throw new InvalidOperationException("Simulated async disposal failure");
            }

            public void Dispose()
            {
                IsDisposed = true;
            }

            public Task<IRpcConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IRpcConnection>(new TestRpcConnection());
            }

            public void ReturnConnection(IRpcConnection connection)
            {
            }
        }
    }
}
