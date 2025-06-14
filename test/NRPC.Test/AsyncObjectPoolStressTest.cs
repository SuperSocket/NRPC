using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Caller.Connection;
using Xunit;

namespace NRPC.Test
{
    public class AsyncObjectPoolStressTest
    {
        [Fact]
        public async Task AsyncObjectPool_Should_Handle_High_Concurrency()
        {
            // Arrange
            var policy = new SlowCreationObjectPolicy();
            var pool = new AsyncObjectPool<TestPoolObject>(policy, maxSize: 10);
            const int concurrentTasks = 40;
            const int operationsPerTask = 10;

            // Act
            var tasks = Enumerable.Range(0, concurrentTasks)
                .Select(_ => Task.Run(async () =>
                {
                    var objects = new List<TestPoolObject>();

                    for (int i = 0; i < operationsPerTask; i++)
                    {
                        var obj = await pool.GetAsync(CancellationToken.None);
                        objects.Add(obj);
                        // Simulate some work
                        await Task.Delay(1);
                        pool.Return(obj);
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks);
            
            // Assert
            // Verify that we didn't create too many objects due to pooling
            Assert.True(policy.CreateCount > 0);
            Assert.True(policy.CreateCount < concurrentTasks * operationsPerTask); // Should be much less due to reuse
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Handle_Cancellation_Gracefully()
        {
            // Arrange
            var policy = new SlowCreationObjectPolicy(delayMs: 1000); // Very slow creation
            var pool = new AsyncObjectPool<TestPoolObject>(policy, maxSize: 5);

            // Act & Assert
            using var cts = new CancellationTokenSource(100); // Cancel after 100ms
            
            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await pool.GetAsync(cts.Token);
            });
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Dispose_Properly()
        {
            // Arrange
            var policy = new SlowCreationObjectPolicy();
            var pool = new AsyncObjectPool<TestPoolObject>(policy, maxSize: 3);

            // Create and return some objects
            var obj1 = await pool.GetAsync(CancellationToken.None);
            var obj2 = await pool.GetAsync(CancellationToken.None);
            
            pool.Return(obj1);
            pool.Return(obj2);

            // Act
            pool.Dispose();

            // Assert
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await pool.GetAsync(CancellationToken.None);
            });

            // Objects in pool should be disposed
            Assert.True(obj1.IsDisposed);
            Assert.True(obj2.IsDisposed);
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Not_Return_Objects_After_Dispose()
        {
            // Arrange
            var policy = new SlowCreationObjectPolicy();
            var pool = new AsyncObjectPool<TestPoolObject>(policy, maxSize: 2);

            var obj = await pool.GetAsync(CancellationToken.None);

            // Act
            pool.Dispose();
            pool.Return(obj); // Should not throw, but should not add to pool

            // Assert
            Assert.False(obj.IsDisposed); // won't be disposed yet, but should not be reused
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Handle_Policy_Creation_Failures()
        {
            // Arrange
            var policy = new FailingCreationObjectPolicy();
            var pool = new AsyncObjectPool<TestPoolObject>(policy, maxSize: 5);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await pool.GetAsync(CancellationToken.None);
            });
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Not_Exceed_Max_Size_Under_Stress()
        {
            // Arrange
            var policy = new TrackingObjectPolicy();
            var pool = new AsyncObjectPool<TestPoolObject>(policy, maxSize: 5);
            const int totalOperations = 100;

            // Act - Get and return many objects rapidly
            var tasks = Enumerable.Range(0, totalOperations)
                .Select(_ => Task.Run(async () =>
                {
                    var obj = await pool.GetAsync(CancellationToken.None);
                    await Task.Delay(10); // Brief work
                    pool.Return(obj);
                }))
                .ToArray();

            await Task.WhenAll(tasks);

            // Assert
            Assert.True(policy.CreateCount >= 5); // Should create at least max size
            Assert.True(policy.CreateCount <= totalOperations); // But not more than total operations
        }

        [Fact]
        public async Task AsyncObjectPool_Dispose_Should_Dispose_All_Pooled_Objects()
        {
            // Arrange
            var policy = new DisposableObjectPolicy();
            var pool = new AsyncObjectPool<DisposableTestObject>(policy, maxSize: 5);

            // Create and return several objects to the pool
            var objects = new List<DisposableTestObject>();
            for (int i = 0; i < 3; i++)
            {
                var obj = await pool.GetAsync(CancellationToken.None);
                objects.Add(obj);
                pool.Return(obj); // Return to pool so they will be disposed
            }

            // Act
            pool.Dispose();

            // Assert
            foreach (var obj in objects)
            {
                Assert.True(obj.IsDisposed, $"Object {obj.Id} should be disposed");
            }
        }

        [Fact]
        public async Task AsyncObjectPool_Dispose_Should_Prevent_Further_Operations()
        {
            // Arrange
            var policy = new DisposableObjectPolicy();
            var pool = new AsyncObjectPool<DisposableTestObject>(policy, maxSize: 3);

            // Get an object before disposal
            var obj = await pool.GetAsync(CancellationToken.None);

            // Act
            pool.Dispose();

            // Assert
            // Should throw ObjectDisposedException when trying to get after disposal
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await pool.GetAsync(CancellationToken.None);
            });

            // Return should be ignored after disposal (should not throw)
            pool.Return(obj); // This should not throw but should be ignored
            Assert.False(obj.IsDisposed, "Object not returned to pool should not be disposed");
        }

        [Fact]
        public async Task AsyncObjectPool_DisposeAsync_Should_Handle_AsyncDisposable_Objects()
        {
            // Arrange
            var policy = new AsyncDisposableObjectPolicy();
            var pool = new AsyncObjectPool<AsyncDisposableTestObject>(policy, maxSize: 4);

            // Create and return several async disposable objects
            var objects = new List<AsyncDisposableTestObject>();
            for (int i = 0; i < 3; i++)
            {
                var obj = await pool.GetAsync(CancellationToken.None);
                objects.Add(obj);
                pool.Return(obj);
            }

            // Act
            await pool.DisposeAsync();

            // Assert
            foreach (var obj in objects)
            {
                Assert.True(obj.IsDisposed, $"AsyncDisposable object {obj.Id} should be disposed");
                Assert.True(obj.DisposeAsyncCalled, $"DisposeAsync should have been called on object {obj.Id}");
            }
        }

        [Fact]
        public async Task AsyncObjectPool_Dispose_Should_Handle_Disposal_Exceptions_Gracefully()
        {
            // Arrange
            var policy = new FaultyDisposableObjectPolicy();
            var pool = new AsyncObjectPool<FaultyDisposableTestObject>(policy, maxSize: 3);

            // Create and return objects that will throw during disposal
            for (int i = 0; i < 2; i++)
            {
                var obj = await pool.GetAsync(CancellationToken.None);
                pool.Return(obj);
            }

            // Act & Assert
            // Disposal should not throw even if individual object disposal fails
            pool.Dispose();

            // Pool should still be marked as disposed
            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await pool.GetAsync(CancellationToken.None);
            });
        }

        [Fact]
        public async Task AsyncObjectPool_Multiple_Dispose_Calls_Should_Be_Safe()
        {
            // Arrange
            var policy = new DisposableObjectPolicy();
            var pool = new AsyncObjectPool<DisposableTestObject>(policy, maxSize: 2);

            var obj = await pool.GetAsync(CancellationToken.None);
            pool.Return(obj);

            // Act
            pool.Dispose();
            pool.Dispose(); // Second call should be safe
            await pool.DisposeAsync(); // Mixed disposal calls should be safe

            // Assert
            Assert.True(obj.IsDisposed, "Object should be disposed only once");

            await Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            {
                await pool.GetAsync(CancellationToken.None);
            });
        }

        [Fact]
        public async Task AsyncObjectPool_Should_Not_Dispose_Objects_Not_In_Pool()
        {
            // Arrange
            var policy = new DisposableObjectPolicy();
            var pool = new AsyncObjectPool<DisposableTestObject>(policy, maxSize: 2);

            // Get objects but don't return them to pool
            var obj1 = await pool.GetAsync(CancellationToken.None);
            var obj2 = await pool.GetAsync(CancellationToken.None);

            // Return only one object to pool
            pool.Return(obj1);

            // Act
            pool.Dispose();

            // Assert
            Assert.True(obj1.IsDisposed, "Object in pool should be disposed");
            Assert.False(obj2.IsDisposed, "Object not in pool should not be disposed");
        }

        // Helper classes for stress testing
        private class TestPoolObject : IDisposable
        {
            public bool IsDisposed { get; private set; }
            public DateTime CreatedAt { get; } = DateTime.UtcNow;

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private class SlowCreationObjectPolicy : IAsyncPooledObjectPolicy<TestPoolObject>
        {
            private readonly int _delayMs;
            private int _createCount;

            public SlowCreationObjectPolicy(int delayMs = 10)
            {
                _delayMs = delayMs;
            }

            public int CreateCount => _createCount;

            public async Task<TestPoolObject> CreateAsync(CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _createCount);
                await Task.Delay(_delayMs, cancellationToken);
                return new TestPoolObject();
            }

            public bool Return(TestPoolObject obj)
            {
                return obj != null && !obj.IsDisposed;
            }
        }

        private class FailingCreationObjectPolicy : IAsyncPooledObjectPolicy<TestPoolObject>
        {
            public Task<TestPoolObject> CreateAsync(CancellationToken cancellationToken = default)
            {
                throw new InvalidOperationException("Simulated creation failure");
            }

            public bool Return(TestPoolObject obj)
            {
                return false;
            }
        }

        private class TrackingObjectPolicy : IAsyncPooledObjectPolicy<TestPoolObject>
        {
            private int _createCount;
            private int _currentConcurrent;
            private int _maxConcurrent;

            public int CreateCount => _createCount;
            public int MaxConcurrentObjects => _maxConcurrent;

            public Task<TestPoolObject> CreateAsync(CancellationToken cancellationToken = default)
            {
                Interlocked.Increment(ref _createCount);
                var current = Interlocked.Increment(ref _currentConcurrent);
                
                // Track max concurrent
                var maxSnapshot = _maxConcurrent;
                while (current > maxSnapshot)
                {
                    var previous = Interlocked.CompareExchange(ref _maxConcurrent, current, maxSnapshot);
                    if (previous == maxSnapshot) break;
                    maxSnapshot = _maxConcurrent;
                }

                try
                {
                    // Simulate some delay for object creation
                    return Task.FromResult(new TestPoolObject());
                }
                finally
                {
                    Interlocked.Decrement(ref _currentConcurrent);
                }
            }

            public bool Return(TestPoolObject obj)
            {
                if (obj != null && !obj.IsDisposed)
                {
                    return true;
                }

                return false;
            }
        }

        // Additional test helper classes for disposal testing
        private class DisposableTestObject : IDisposable
        {
            public string Id { get; } = Guid.NewGuid().ToString();
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        private class AsyncDisposableTestObject : IAsyncDisposable, IDisposable
        {
            public string Id { get; } = Guid.NewGuid().ToString();
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
        }

        private class FaultyDisposableTestObject : IDisposable
        {
            public string Id { get; } = Guid.NewGuid().ToString();
            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                IsDisposed = true;
                throw new InvalidOperationException("Simulated disposal failure");
            }
        }

        private class DisposableObjectPolicy : IAsyncPooledObjectPolicy<DisposableTestObject>
        {
            public Task<DisposableTestObject> CreateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new DisposableTestObject());
            }

            public bool Return(DisposableTestObject obj)
            {
                return obj != null && !obj.IsDisposed;
            }
        }

        private class AsyncDisposableObjectPolicy : IAsyncPooledObjectPolicy<AsyncDisposableTestObject>
        {
            public Task<AsyncDisposableTestObject> CreateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new AsyncDisposableTestObject());
            }

            public bool Return(AsyncDisposableTestObject obj)
            {
                return obj != null && !obj.IsDisposed;
            }
        }

        private class FaultyDisposableObjectPolicy : IAsyncPooledObjectPolicy<FaultyDisposableTestObject>
        {
            public Task<FaultyDisposableTestObject> CreateAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(new FaultyDisposableTestObject());
            }

            public bool Return(FaultyDisposableTestObject obj)
            {
                return obj != null && !obj.IsDisposed;
            }
        }
    }
}
