using System;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller.Connection
{
    public class SingleConnectionManager : IConnectionManager<IRpcConnection>
    {
        private readonly IConnectionFactory<IRpcConnection> _connectionFactory;

        private readonly IConnectionValidator<IRpcConnection> _connectionValidator;

        private IRpcConnection _connection;

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public SingleConnectionManager(IConnectionFactory<IRpcConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
            _connectionValidator = RpcConnectionValidator.Instance;
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _connection = default;
        }

        public async ValueTask DisposeAsync()
        {
            if (_connection is IRpcConnection connection)
            {
                _connection = default;
                await connection.DisposeAsync();
            }
        }

        public Task<IRpcConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
        {
            if (_connection is IRpcConnection connection)
            {
                return Task.FromResult(connection);
            }

            _semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_connection is IRpcConnection existingConnection)
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

        public void ReturnConnection(IRpcConnection connection)
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