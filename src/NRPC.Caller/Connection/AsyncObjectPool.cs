using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public class AsyncObjectPool<T> : IAsyncObjectPool<T>, IDisposable where T : class
    {
        private readonly ConcurrentQueue<T> _objects = new ConcurrentQueue<T>();

        private readonly IAsyncPooledObjectPolicy<T> _objectPolicy;
        
        private readonly int _maxSize;
        private int _currentCount;
        private bool _disposed;

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
                disposable.Dispose();
            }
        }

        public void Clear()
        {
            while (_objects.TryDequeue(out T item))
            {
                if (item is IDisposable disposable)
                    disposable.Dispose();
            }

            _currentCount = 0;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Clear();
                _disposed = true;
            }
        }
    }
}