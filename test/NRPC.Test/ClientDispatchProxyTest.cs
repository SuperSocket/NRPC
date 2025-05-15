using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NRPC.Abstractions;
using NRPC.Client;
using Xunit;
using System.Collections.Generic;
using System.Threading.Channels;

namespace NRPC.Test
{
    [ServiceContractAtribute]
    public interface ITestService
    {
        Task<int> Add(int x, int y);
        
        Task<string> Concat(string x, string y);

        Task ExecuteVoid(string command);
    }

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

            if (request.MethodName == "Add")
            {
                int x = (int)request.Arguments[0];
                int y = (int)request.Arguments[1];
                response.Result = x + y;
            }
            else if (request.MethodName == "Concat")
            {
                string x = (string)request.Arguments[0];
                string y = (string)request.Arguments[1];
                response.Result = x + y;
            }
            else if (request.MethodName == "ExecuteVoid")
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

        public IRpcConnection CreateConnection()
        {
            return mockRpcConnection;
        }
    }

    public class ClientDispatchProxyTest
    {
        [Fact]
        public async Task TestDispatchProxyCreation()
        {
            // Arrange
            var clientFactory = new ProxyClientFactory<ITestService>(new MockRpcConnectionFactory(new MockRpcConnection()));
            
            // Act
            var client = clientFactory.CreateClient();
            
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
            var clientFactory = new ProxyClientFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = clientFactory.CreateClient();
            // Invoke the method
            var result = await client.Add(5, 10);
            
            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("Add", capturedRequest.MethodName);
            Assert.Equal(2, capturedRequest.Arguments.Length);
            Assert.Equal(5, capturedRequest.Arguments[0]);
            Assert.Equal(10, capturedRequest.Arguments[1]);
            Assert.Equal(15, result);
        }

        [Fact]
        public async Task TestDispatchProxyStringOperation()
        {
            // Arrange
            RpcRequest capturedRequest = null;
            
            var mockRpcConnection = new MockRpcConnection(req => capturedRequest = req);

            var clientFactory = new ProxyClientFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = clientFactory.CreateClient();

            // Invoke the method
            var result = await client.Concat("Hello, ", "World!");
            
            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("Concat", capturedRequest.MethodName);
            Assert.Equal(2, capturedRequest.Arguments.Length);
            Assert.Equal("Hello, ", capturedRequest.Arguments[0]);
            Assert.Equal("World!", capturedRequest.Arguments[1]);
            Assert.Equal("Hello, World!", result);
        }

        [Fact]
        public async Task TestDispatchProxyVoidMethod()
        {
            // Arrange
            RpcRequest capturedRequest = null;
            
            var mockRpcConnection = new MockRpcConnection(req => capturedRequest = req);

            var clientFactory = new ProxyClientFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = clientFactory.CreateClient();
            
            // Invoke the void method
            await client.ExecuteVoid("test command");
            
            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("ExecuteVoid", capturedRequest.MethodName);
            Assert.Equal(1, capturedRequest.Arguments.Length);
            Assert.Equal("test command", capturedRequest.Arguments[0]);
        }
    }
}