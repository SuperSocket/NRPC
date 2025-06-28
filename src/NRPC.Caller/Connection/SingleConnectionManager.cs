using System;
using System.Threading;
using System.Threading.Tasks;

namespace NRPC.Caller.Connection
{
    public class SingleConnectionManager<TConnection> : IConnectionManager<TConnection>
        where TConnection : IDisposable, IAsyncDisposable
    {
        private readonly IConnectionFactory<TConnection> _connectionFactory;

        private readonly IConnectionValidator<TConnection> _connectionValidator;

        private TConnection _connection;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SingleConnectionManager(IConnectionFactory<TConnection> connectionFactory, IConnectionValidator<TConnection> connectionValidator = null)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _connectionValidator = connectionValidator ?? NullConnectionValidator<TConnection>.Instance;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = default;
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection is TConnection connection)
            {
                _connection = default;
                await connection.DisposeAsync();
            }
        }

        public Task<TConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_connection is TConnection connection)
            {
                return Task.FromResult(connection);
            }

            _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_connection is TConnection existingConnection)
                {
                    return Task.FromResult(existingConnection);
                }

                _connection = _connectionFactory.CreateConnectionAsync(cancellationToken).GetAwaiter().GetResult();
                return Task.FromResult(_connection);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void ReturnConnection(TConnection connection)
        {
            if (connection == null)
                return;

            if (_connectionValidator.Validate(connection))
            {
                _connection = connection;
            }
            else
            {
                _connection = default;
                
                try
                {
                    connection.Dispose();
                }
                catch (Exception)
                {
                    // Ignore exceptions during disposal
                }
            }
        }
    }
}