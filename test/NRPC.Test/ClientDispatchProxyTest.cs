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
using NRPC.Caller.Connection;

namespace NRPC.Test
{

    public class ClientDispatchProxyTest
    {
        [Fact]
        public void TestDispatchProxyCreation()
        {
            // Arrange
            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(new MockRpcConnection()));
            
            // Act
            var client = clientFactory.CreateCaller();
            
            // Assert
            Assert.NotNull(client);
            Assert.IsAssignableFrom<ITestService>(client);
        }

        [Fact]
        public async Task TestDispatchProxyInvokesMethodWithCorrectArguments()
        {
            // Arrange
            var mockRpcConnection = new MockRpcConnection();
            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client =  clientFactory.CreateCaller();
            // Invoke the method
            var result = await client.Add(5, 10);

            var capturedRequest = mockRpcConnection.LastSentRequest;

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
            var mockRpcConnection = new MockRpcConnection();
            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = clientFactory.CreateCaller();

            // Invoke the method
            var result = await client.Concat("Hello, ", "World!");
            
            var capturedRequest = mockRpcConnection.LastSentRequest;

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
            var mockRpcConnection = new MockRpcConnection();

            var clientFactory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(mockRpcConnection));

            // Act
            var client = clientFactory.CreateCaller();
            
            // Invoke the void method
            await client.ExecuteVoid("test command");

            var capturedRequest = mockRpcConnection.LastSentRequest;

            // Assert
            Assert.NotNull(capturedRequest);
            Assert.Equal("ExecuteVoid", capturedRequest.Method);
            Assert.Single(capturedRequest.Parameters);
            Assert.Equal("test command", capturedRequest.Parameters[0]);
        }
    }
}