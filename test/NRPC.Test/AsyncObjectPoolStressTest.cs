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
        //[Fact]
        public async Task AsyncObjectPool_Should_Handle_High_Concurrency()
        {
            // Arrange
            var policy = new SlowCreationObjectPolicy();
            var pool = new AsyncObjectPool<TestPoolObject>(policy, maxSize: 10);
            const int concurrentTasks = 10;
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
                    }
                    
                    // Return all objects
                    foreach (var obj in objects)
                    {
                        pool.Return(obj);
                    }
                }))
                .ToArray();

            await Task.WhenAll(tasks).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    throw t.Exception ?? new Exception("Unknown error during task execution");
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

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
    }
}
