using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

/*
namespace NRPC.Caller.Connection
{
    internal class ConnectionPool<TConnection> : IConnectionPool<TConnection>
        where TConnection : IDisposable, IAsyncDisposable
    {
        private readonly Channel<TConnection> _channel;
        private readonly ChannelWriter<TConnection> _writer;
        private readonly ChannelReader<TConnection> _reader;
        private readonly Func<CancellationToken, Task<TConnection>> _connectionFactory;
        private readonly int _concurrentConnectLimit = 100;
        private readonly SemaphoreSlim _semaphoreForConnect;
        private readonly int _maxConnections;
        private readonly int _minConnections = 10;

        private bool _disposed;
        private readonly ConcurrentQueue<TConnection> _connectionsToDispose = new();
        private Task _regularDisposeTask;
        private CancellationTokenSource _regularDisposeCts;

        private TimeSpan _regularDisposeInterval = TimeSpan.FromMinutes(1);

        public int AvailableConnections => _reader.Count;

        private int _totalConnections;
        public int TotalConnections => _totalConnections;

        private int _inUsedConnections;

        public ConnectionPool(Func<CancellationToken, Task<TConnection>> connectionFactory, int maxConnections = 10)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _maxConnections = maxConnections;

            var options = new BoundedChannelOptions(maxConnections)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false,
            };

            _channel = Channel.CreateBounded<TConnection>(options);
            _writer = _channel.Writer;
            _reader = _channel.Reader;

            _semaphoreForConnect = new SemaphoreSlim(_concurrentConnectLimit, _concurrentConnectLimit);
            _regularDisposeCts = new CancellationTokenSource();
            _regularDisposeTask = DisposeConnectionsToDispose(_regularDisposeCts.Token);
        }

        public async Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ConnectionPool<TConnection>));

            if (_reader.TryRead(out var connection))
            {
                Interlocked.Increment(ref _inUsedConnections);
                return connection;
            }

            if (_totalConnections < _maxConnections)
            {
                await _semaphoreForConnect.WaitAsync(cancellationToken);

                try
                {
                    if (_totalConnections < _maxConnections)
                    {
                        connection = await CreateConnectionAsync(cancellationToken);
                        Interlocked.Increment(ref _inUsedConnections);
                        return connection;
                    }
                }
                finally
                {
                    _semaphoreForConnect.Release();
                }
            }

            connection = await _reader.ReadAsync(cancellationToken);
            Interlocked.Increment(ref _inUsedConnections);
            return connection;
        }

        public void ReturnConnection(TConnection connection)
        {
            if (connection == null)
                return;

            Interlocked.Decrement(ref _inUsedConnections);

            if (_disposed)
            {
                connection.Dispose();
                return;
            }

            var returned = false;

            try
            {
                returned = _writer.TryWrite(connection);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (!returned)
                {
                    _connectionsToDispose.Enqueue(connection);
                    Interlocked.Decrement(ref _totalConnections);
                }
            }
        }

        private async Task<TConnection> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            var connection = await _connectionFactory(cancellationToken);
            Interlocked.Increment(ref _totalConnections);
            return connection;
        }

        private async Task DisposeConnectionsToDispose(CancellationToken cancellationToken, bool isFinalCleanup = false)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                while (!cancellationToken.IsCancellationRequested
                    && _connectionsToDispose.TryDequeue(out var connection))
                {
                    try
                    {
                        await connection.DisposeAsync();
                    }
                    catch (Exception)
                    {
                        // Log or handle the exception as needed
                    }
                }

                if (isFinalCleanup)
                    break;

                await Task
                    .Delay(_regularDisposeInterval, cancellationToken)
                    .ContinueWith(t => { });
            }
        }

        private void ClearResources()
        {
            _writer.Complete();
            _regularDisposeCts.Cancel();
            _semaphoreForConnect.Dispose();

            while (_reader.TryRead(out var connection))
            {
                _connectionsToDispose.Enqueue(connection);
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ClearResources();

            _regularDisposeTask.Wait();
            _regularDisposeCts.Dispose();

            DisposeConnectionsToDispose(CancellationToken.None, isFinalCleanup: true).Wait();
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;
            ClearResources();

            await _regularDisposeTask;
            _regularDisposeCts.Dispose();

            await DisposeConnectionsToDispose(CancellationToken.None, isFinalCleanup: true)
                .ConfigureAwait(false);
        }
    }
}
*/