using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public class AsyncObjectPool<T> : IAsyncObjectPool<T> where T : class
    {
        private readonly ConcurrentQueue<T> _objects = new ConcurrentQueue<T>();

        private readonly IAsyncPooledObjectPolicy<T> _objectPolicy;
        
        private readonly int _maxSize;
        private int _currentCount;
        private bool _disposed;

        private readonly bool _isAsyncDisposable = typeof(T).IsAssignableTo(typeof(IAsyncDisposable));

        public AsyncObjectPool(IAsyncPooledObjectPolicy<T> objectPolicy, int maxSize = 100)
        {
            _objectPolicy = objectPolicy ?? throw new ArgumentNullException(nameof(objectPolicy));
            _maxSize = maxSize;
        }

        public async ValueTask<T> GetAsync(CancellationToken cancellationToken)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(AsyncObjectPool<T>));

            if (_objects.TryDequeue(out T item))
            {
                Interlocked.Decrement(ref _currentCount);
                return item;
            }

            return await _objectPolicy
                .CreateAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public void Return(T item)
        {
            if (_disposed || item == null)
                return;

            if (_currentCount < _maxSize && _objectPolicy.Return(item))
            {
                _objects.Enqueue(item);
                Interlocked.Increment(ref _currentCount);
            }
            else if (item is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception)
                {
                }
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                while (_objects.TryDequeue(out T item))
                {
                    if (item is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception)
                        {
                            // Handle exceptions from disposal if necessary
                        }
                    }
                }

                _currentCount = 0;
                GC.SuppressFinalize(this);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (!_isAsyncDisposable)
            {
                Dispose();
                return;
            }

            if (!_disposed)
            {
                _disposed = true;

                var tasks = new List<Task>(_objects.Count);

                while (_objects.TryDequeue(out T item))
                {
                    tasks.Add(
                        item is IAsyncDisposable asyncDisposable
                            ? asyncDisposable.DisposeAsync().AsTask()
                            : Task.CompletedTask);
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Handle exceptions from disposal if necessary
                }
                finally
                {
                    _currentCount = 0;
                    GC.SuppressFinalize(this);
                }
            }
        }
    }
}