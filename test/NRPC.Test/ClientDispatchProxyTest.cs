using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NRPC.Abstractions;
using NRPC.Caller;
using Xunit;
using System.Collections.Generic;
using System.Threading.Channels;

namespace NRPC.Test
{

    public class MockRpcConnection : IRpcConnection
    {
        private readonly Channel<RpcRequest> _requestChannel = Channel.CreateUnbounded<RpcRequest>();
        private readonly Action<RpcRequest> _requestAction;

        public MockRpcConnection(Action<RpcRequest> requestAction = null)
        {
            _requestAction = requestAction;
        }

        public Task SendAsync(RpcRequest request)
        {
            _requestChannel.Writer.TryWrite(request);
            _requestAction?.Invoke(request);
            return Task.CompletedTask;
        }

        public async Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            var request = await _requestChannel.Reader.ReadAsync(cancellationToken);

            var response = new RpcResponse();

            response.Id = request.Id;

            if (request.Method == "Add")
            {
                int x = (int)request.Parameters[0];
                int y = (int)request.Parameters[1];
                response.Result = x + y;
            }
            else if (request.Method == "Concat")
            {
                string x = (string)request.Parameters[0];
                string y = (string)request.Parameters[1];
                response.Result = x + y;
            }
            else if (request.Method == "ExecuteVoid")
            {
                // No result for void
            }
            else
            {
                response.Error = new RpcError(404, "Method not found");
            }

            return response;
        }
    }

    public class MockRpcConnectionFactory : IRpcConnectionFactory
    {
        private readonly MockRpcConnection mockRpcConnection;

        public MockRpcConnectionFactory(MockRpcConnection mockRpcConnection)
        {
            this.mockRpcConnection = mockRpcConnection;
        }

        public Task<IRpcConnection> CreateConnection()
        {
            return Task.FromResult<IRpcConnection>(mockRpcConnection);
        }
    }

    public class ClientDispatchProxyTest
    {
        [Fact]
        public async Task TestDispatchProxyCreation()
        {
            // Arrange
            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(new MockRpcConnection()));
            
            // Act
            var client = await clientFactory.CreateCaller();
            
            // Assert
            Assert.NotNull(client);
            Assert.IsAssignableFrom<ITestService>(client);
        }

        [Fact]
        public async Task TestDispatchProxyInvokesMethodWithCorrectArguments()
        {
            // Arrange
            RpcRequest capturedRequest = null;
            var mockRpcConnection = new MockRpcConnection(req => capturedRequest = req);
            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = await clientFactory.CreateCaller();
            // Invoke the method
            var result = await client.Add(5, 10);
            
            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("Add", capturedRequest.Method);
            Assert.Equal(2, capturedRequest.Parameters.Length);
            Assert.Equal(5, capturedRequest.Parameters[0]);
            Assert.Equal(10, capturedRequest.Parameters[1]);
            Assert.Equal(15, result);
        }

        [Fact]
        public async Task TestDispatchProxyStringOperation()
        {
            // Arrange
            RpcRequest capturedRequest = null;
            
            var mockRpcConnection = new MockRpcConnection(req => capturedRequest = req);

            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = await clientFactory.CreateCaller();

            // Invoke the method
            var result = await client.Concat("Hello, ", "World!");
            
            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("Concat", capturedRequest.Method);
            Assert.Equal(2, capturedRequest.Parameters.Length);
            Assert.Equal("Hello, ", capturedRequest.Parameters[0]);
            Assert.Equal("World!", capturedRequest.Parameters[1]);
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public async Task TestDispatchProxyVoidMethod()
        {
            // Arrange
            RpcRequest capturedRequest = null;
            
            var mockRpcConnection = new MockRpcConnection(req => capturedRequest = req);

            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = await clientFactory.CreateCaller();
            
            // Invoke the void method
            await client.ExecuteVoid("test command");
            
            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("ExecuteVoid", capturedRequest.Method);
            Assert.Single(capturedRequest.Parameters);
            Assert.Equal("test command", capturedRequest.Parameters[0]);
        }
    }
}