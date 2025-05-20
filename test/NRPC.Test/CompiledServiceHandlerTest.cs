using System;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Executor;
using Xunit;

namespace NRPC.Test
{
    public class CompiledServiceHandlerTest
    {
        [Fact]
        public async Task TestAsyncMethodInvocation()
        {
            // Arrange
            var service = new TestService();
            var handler = new CompiledServiceHandler<TestService>();
            var request = RpcRequest.Create(1, "AddAsync", 2, 3);
            
            // Act
            var response = await handler.HandleRequestAsync(service, request);
            
            // Assert
            Assert.NotNull(response);
            Assert.Null(response.Error);
            Assert.Equal(5, response.Result);
        }

        [Fact]
        public async Task TestVoidAsyncMethodInvocation()
        {
            // Arrange
            var service = new TestService();
            var handler = new CompiledServiceHandler<TestService>();
            var request = RpcRequest.Create(1, "DoWorkAsync");
            
            // Act
            var response = await handler.HandleRequestAsync(service, request);
            
            // Assert
            Assert.NotNull(response);
            Assert.Null(response.Error);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task TestSyncMethodNotFound()
        {
            // Arrange
            var service = new TestService();
            var handler = new CompiledServiceHandler<TestService>();
            var request = RpcRequest.Create(1, "Add", 2, 3);

            // Act
            var response = await handler.HandleRequestAsync(service, request);

            // Assert
            Assert.NotNull(response);
            // Current implementation filters out non-Task methods, so this should return a "not found" error
            Assert.NotNull(response.Error);
            Assert.Equal(404, response.Error.Code);
            Assert.Contains("not found", response.Error.Message);
        }

        [Fact]
        public async Task TestStringMethodNotFound()
        {
            // Arrange
            var service = new TestService();
            var handler = new CompiledServiceHandler<TestService>();
            var request = RpcRequest.Create(1, "Concatenate", "Hello, ", "World!");

            // Act
            var response = await handler.HandleRequestAsync(service, request);

            // Assert
            Assert.NotNull(response);
            // Current implementation filters out non-Task methods, so this should return a "not found" error
            Assert.NotNull(response.Error);
            Assert.Equal(404, response.Error.Code);
            Assert.Contains("not found", response.Error.Message);
        }

        [Fact]
        public async Task TestVoidMethodNotFound()
        {
            // Arrange
            var service = new TestService();
            var handler = new CompiledServiceHandler<TestService>();
            var request = RpcRequest.Create(1, "DoWork");

            // Act
            var response = await handler.HandleRequestAsync(service, request);

            // Assert
            Assert.NotNull(response);
            // Current implementation filters out non-Task methods, so this should return a "not found" error
            Assert.NotNull(response.Error);
            Assert.Equal(404, response.Error.Code);
            Assert.Contains("not found", response.Error.Message);
        }

        [Fact]
        public async Task TestMethodNotFound()
        {
            // Arrange
            var service = new TestService();
            var handler = new CompiledServiceHandler<TestService>();
            var request = RpcRequest.Create(1, "NonExistentMethod");

            // Act
            var response = await handler.HandleRequestAsync(service, request);

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.Error);
            Assert.Equal(404, response.Error.Code);
            Assert.Contains("not found", response.Error.Message);
        }
    }
}