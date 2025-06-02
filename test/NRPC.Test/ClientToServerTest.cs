using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Caller;
using NRPC.Executor;
using Xunit;

namespace NRPC.Test
{
    public class ClientToServerTest
    {
        // A mock implementation of IRpcConnection for testing
        private class MockRpcConnection : IRpcConnection
        {
            private readonly Channel<RpcResponse> _responses = Channel.CreateUnbounded<RpcResponse>();

            private CompiledServiceHandler<ITestService> _handler = new CompiledServiceHandler<ITestService>();

            private ITestService _testService;

            public MockRpcConnection()
                : this(new TestService())
            {
            }

            public MockRpcConnection(ITestService testService)
            {
                _testService = testService;
            }

            // Implement SendAsync to record sent requests
            public async Task SendAsync(RpcRequest request)
            {
                var response = await _handler.HandleRequestAsync(_testService, request);
                _responses.Writer.TryWrite(response);
            }

            // Implement ReceiveAsync to return queued responses
            public async Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
            {
                return await _responses.Reader.ReadAsync(cancellationToken);
            }
        }

        // A connection factory for testing
        private class TestRpcConnectionFactory : IRpcConnectionFactory
        {
            private readonly IRpcConnection _connection;

            public TestRpcConnectionFactory(IRpcConnection connection)
            {
                _connection = connection;
            }

            public Task<IRpcConnection> CreateConnection(CancellationToken cancellationToken)
            {
                return Task.FromResult(_connection);
            }
        }

        [Fact]
        public async Task TestBasicRpcWorkflow()
        {
            var factory = new RpcCallerFactory<ITestService>(new TestRpcConnectionFactory(new MockRpcConnection()));
            var client = await factory.CreateCaller(TestContext.Current.CancellationToken);

            Assert.Equal(3, await client.Add(1, 2));
            Assert.Equal("123", await client.Concat("1", "23"));
            await client.ExecuteVoid("Test");
        }

        [Fact]
        public async Task TestRpcException()
        {
            var factory = new RpcCallerFactory<ITestService>(new TestRpcConnectionFactory(new MockRpcConnection(new TestServiceWithException())));
            var client = await factory.CreateCaller(TestContext.Current.CancellationToken);

            var rpcException = await Assert.ThrowsAsync<RpcServerException>(async () => await client.ExecuteVoid("Test exception"));
            Assert.NotNull(rpcException.ServerError);
            Assert.Equal(500, rpcException.ServerError.Code);
            Assert.Equal("Test exception", rpcException.ServerError.Message);
        }

        class TestServiceWithException : TestService
        {
            public override Task ExecuteVoid(string command)
            {
                throw new InvalidOperationException(command);
            }
        }
    }
}
