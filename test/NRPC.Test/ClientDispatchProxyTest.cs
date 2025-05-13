using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NRPC.Abstractions;
using NRPC.Client;
using Xunit;
using System.Collections.Generic;

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
        private readonly Dictionary<int, RpcRequest> _requests = new Dictionary<int, RpcRequest>();
        private readonly Action<RpcRequest> _requestAction;

        public MockRpcConnection(Action<RpcRequest> requestAction = null)
        {
            _requestAction = requestAction;
        }

        public Task SendAsync(RpcRequest request)
        {
            _requests[request.Id] = request;
            _requestAction?.Invoke(request);
            return Task.CompletedTask;
        }

        public Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            var response = new RpcResponse();
            
            // Find the last request and generate a response for it
            if (_requests.Count > 0)
            {
                var lastRequest = _requests[_requests.Keys.Max()];
                response.Id = lastRequest.Id;

                if (lastRequest.MethodName == "Add")
                {
                    int x = (int)lastRequest.Arguments[0];
                    int y = (int)lastRequest.Arguments[1];
                    response.Result = x + y;
                }
                else if (lastRequest.MethodName == "Concat")
                {
                    string x = (string)lastRequest.Arguments[0];
                    string y = (string)lastRequest.Arguments[1];
                    response.Result = x + y;
                }
                else if (lastRequest.MethodName == "ExecuteVoid")
                {
                    // No result for void
                }
                else
                {
                    response.Error = new RpcError(404, "Method not found");
                }

                _requests.Remove(lastRequest.Id);
            }
            else
            {
                // No pending requests
                return Task.FromResult<RpcResponse>(null);
            }

            return Task.FromResult(response);
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
            
            // Start a task that will process the response asynchronously
            var responseTask = Task.Run(async () =>
            {
                await Task.Delay(100); // Small delay to ensure the request is sent
                await mockRpcConnection.ReceiveAsync();
            });
            
            // Invoke the method
            var resultTask = client.Add(5, 10);
            
            await Task.WhenAll(responseTask, resultTask);
            var result = await resultTask;
            
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

            // Start a task that will process the response asynchronously
            var responseTask = Task.Run(async () =>
            {
                await Task.Delay(100); // Small delay to ensure the request is sent
                await mockRpcConnection.ReceiveAsync();
            });
            
            // Invoke the method
            var resultTask = client.Concat("Hello, ", "World!");
            
            await Task.WhenAll(responseTask, resultTask);
            var result = await resultTask;
            
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

            // Start a task that will process the response asynchronously
            var responseTask = Task.Run(async () =>
            {
                await Task.Delay(100); // Small delay to ensure the request is sent
                await mockRpcConnection.ReceiveAsync();
            });
            
            // Invoke the void method
            var resultTask = client.ExecuteVoid("test command");
            
            await Task.WhenAll(responseTask, resultTask);
            
            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("ExecuteVoid", capturedRequest.MethodName);
            Assert.Equal(1, capturedRequest.Arguments.Length);
            Assert.Equal("test command", capturedRequest.Arguments[0]);
        }
    }
}