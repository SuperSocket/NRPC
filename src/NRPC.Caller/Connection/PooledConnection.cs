using System;
using System.Threading;
using System.Threading.Tasks;

/*

namespace NRPC.Caller.Connection
{
    public class PooledConnection<TConnection> : IDisposable
    {
        private readonly TConnection _connection;
        private readonly Func<TConnection, bool> _canReturnToPool;
        private bool _disposed;
        private bool _returned;
        public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;

        private readonly ConnectionPool<TConnection> _pool;

        public PooledConnection(TConnection connection, Func<TConnection, bool> canReturnToPool, ConnectionPool<TConnection> pool)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _canReturnToPool = canReturnToPool ?? throw new ArgumentNullException(nameof(canReturnToPool));
            _pool = pool ?? throw new ArgumentNullException(nameof(pool));
        }

        public void ReturnToPool()
        {
            if (!_returned && !_disposed)
            {
                _returned = true;
                _returnToPool(this);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                if (!_returned)
                {
                    _connection?.Dispose();
                }
            }
        }
    }
}

*/