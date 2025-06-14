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
    public class ConnectionPoolTest
    {
        [Fact]
        public async Task AsyncObjectPool_Should_Create_New_Object_When_Empty()
        {
            // Arrange
            var policy = new TestObjectPolicy();
            var pool = new AsyncObjectPool<TestObject>(policy, maxSize: 5);

            // Act
            var obj = await pool.GetAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(obj);
            Assert.Equal(1, policy.CreateCount);
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Reuse_Returned_Objects()
        {
            // Arrange
            var policy = new TestObjectPolicy();
            var pool = new AsyncObjectPool<TestObject>(policy, maxSize: 5);

            // Act
            var obj1 = await pool.GetAsync(CancellationToken.None);
            pool.Return(obj1);
            var obj2 = await pool.GetAsync(CancellationToken.None);

            // Assert
            Assert.Same(obj1, obj2);
            Assert.Equal(1, policy.CreateCount); // Should only create once
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Respect_Max_Size()
        {
            // Arrange
            var policy = new TestObjectPolicy();
            var pool = new AsyncObjectPool<TestObject>(policy, maxSize: 2);

            // Act
            var obj1 = await pool.GetAsync(CancellationToken.None);
            var obj2 = await pool.GetAsync(CancellationToken.None);
            var obj3 = await pool.GetAsync(CancellationToken.None);

            pool.Return(obj1);
            pool.Return(obj2);
            pool.Return(obj3); // This should be discarded due to max size

            var reused1 = await pool.GetAsync(CancellationToken.None);
            var reused2 = await pool.GetAsync(CancellationToken.None);
            var newObj = await pool.GetAsync(CancellationToken.None);

            // Assert
            Assert.True(ReferenceEquals(obj1, reused1) || ReferenceEquals(obj1, reused2));
            Assert.True(ReferenceEquals(obj2, reused1) || ReferenceEquals(obj2, reused2));
            Assert.False(ReferenceEquals(obj3, newObj)); // obj3 should have been discarded
            Assert.Equal(4, policy.CreateCount); // obj1, obj2, obj3, newObj
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Dispose_Objects_When_Not_Returned()
        {
            // Arrange
            var policy = new TestObjectPolicy();
            var pool = new AsyncObjectPool<TestObject>(policy, maxSize: 1);

            // Act
            var obj1 = await pool.GetAsync(CancellationToken.None);
            var obj2 = await pool.GetAsync(CancellationToken.None);

            pool.Return(obj1);
            pool.Return(obj2); // This should dispose obj2 since pool is full

            // Assert
            Assert.False(obj1.IsDisposed);
            Assert.True(obj2.IsDisposed);
        }

        [Fact]
        public async Task RpcConnectionObjectPolicy_Should_Create_Connection_And_Start_Reading()
        {
            // Arrange
            var connectionFactory = new MockRpcConnectionFactory();
            var policy = new RpcConnectionObjectPolicy(connectionFactory);

            // Act
            var connection = await policy.CreateAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(connection);
            Assert.Equal(1, connectionFactory.CreateConnectionCallCount);
        }

        [Fact]
        public void RpcConnectionObjectPolicy_Should_Return_True_For_Valid_Connection()
        {
            // Arrange
            var connectionFactory = new MockRpcConnectionFactory();
            var policy = new RpcConnectionObjectPolicy(connectionFactory);
            var connection = new MockRpcConnection();

            // Act
            var result = policy.Return(connection);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RpcConnectionObjectPolicy_Should_Store_Invoke_State()
        {
            // Arrange
            var connectionFactory = new MockRpcConnectionFactory();
            var policy = new RpcConnectionObjectPolicy(connectionFactory) as IInvokeStateManager;
            var invokeState = new InvokeState
            {
                TaskCompletionSource = new TaskCompletionSource<object>(),
                ResponseHandler = new VoidResponseHandler()
            };

            // Act
            var result = policy.TrySaveInvokeState("test-id", invokeState);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RpcConnectionObjectPolicy_Should_Not_Allow_Duplicate_Invoke_State_Ids()
        {
            // Arrange
            var connectionFactory = new MockRpcConnectionFactory();
            var policy = new RpcConnectionObjectPolicy(connectionFactory) as IInvokeStateManager;
            var invokeState1 = new InvokeState
            {
                TaskCompletionSource = new TaskCompletionSource<object>(),
                ResponseHandler = new VoidResponseHandler()
            };
            var invokeState2 = new InvokeState
            {
                TaskCompletionSource = new TaskCompletionSource<object>(),
                ResponseHandler = new VoidResponseHandler()
            };

            // Act
            var result1 = policy.TrySaveInvokeState("test-id", invokeState1);
            var result2 = policy.TrySaveInvokeState("test-id", invokeState2);

            // Assert
            Assert.True(result1);
            Assert.False(result2);
        }

        [Fact]
        public void RpcCallerFactory_Should_Create_Caller_With_Connection_Pool()
        {
            // Arrange
            var connectionFactory = new MockRpcConnectionFactory();
            var callerFactory = new RpcCallerFactory<ITestService>(
                connectionFactory,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            // Act
            var caller = callerFactory.CreateCaller();

            // Assert
            Assert.NotNull(caller);
            Assert.IsAssignableFrom<ITestService>(caller);
        }

        [Fact]
        public async Task RpcCallerFactory_Should_Use_Connection_Pool_For_Multiple_Calls()
        {
            // Arrange
            var connectionFactory = new MockRpcConnectionFactory();
            var callerFactory = new RpcCallerFactory<ITestService>(
                connectionFactory,
                DefaultRpcCallingAdapter.Singleton,
                DirectTypeExpressionConverter.Singleton);

            var caller = callerFactory.CreateCaller();

            // Act
            // Make multiple concurrent calls to verify connection pooling
            var tasks = new Task[5];
            for (int i = 0; i < tasks.Length; i++)
            {
                tasks[i] = caller.Add(i, i + 1);
            }

            await Task.WhenAll(tasks);

            // Assert
            // Verify that connections were created (may be reused)
            Assert.True(connectionFactory.CreateConnectionCallCount >= 1);
            Assert.True(connectionFactory.CreateConnectionCallCount <= 5); // Should not exceed the number of calls
        }

        [Fact]
        public async Task Connection_Pool_Should_Handle_Connection_Disposal_Gracefully()
        {
            // Arrange
            var connectionFactory = new MockRpcConnectionFactory();
            var policy = new RpcConnectionObjectPolicy(connectionFactory);
            var pool = new AsyncObjectPool<IRpcConnection>(policy, maxSize: 2);

            // Act
            var connection1 = await pool.GetAsync(CancellationToken.None);
            var connection2 = await pool.GetAsync(CancellationToken.None);

            // Simulate connection disposal
            if (connection1 is IDisposable disposable1)
                disposable1.Dispose();

            pool.Return(connection1); // Should handle disposed connection gracefully
            pool.Return(connection2);

            var connection3 = await pool.GetAsync(CancellationToken.None);

            // Assert
            Assert.NotNull(connection3);
            Assert.Same(connection2, connection3); // Should reuse the valid connection
        }

        // Helper classes for testing
        private class TestObject : IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private class TestObjectPolicy : IAsyncPooledObjectPolicy<TestObject>
        {
            public int CreateCount { get; private set; }

            public Task<TestObject> CreateAsync(CancellationToken cancellationToken = default)
            {
                CreateCount++;
                return Task.FromResult(new TestObject());
            }

            public bool Return(TestObject obj)
            {
                return obj != null && !obj.IsDisposed;
            }
        }

        private class MockRpcConnectionFactory : IRpcConnectionFactory
        {
            public int CreateConnectionCallCount { get; private set; }

            public Task<IRpcConnection> CreateConnection(CancellationToken cancellationToken = default)
            {
                CreateConnectionCallCount++;
                return Task.FromResult<IRpcConnection>(new MockRpcConnection());
            }
        }
    }
}
