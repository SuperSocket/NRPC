using System;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
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
            var handler = new CompiledServiceHandler<ITestService>(new ServiceMetadata<ITestService>(DirectTypeExpressionConverter.Singleton), DefaultRpcCallingAdapter.Singleton);
            var request = RpcRequest.Create("1", "Add", 2, 3);
            
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
            var handler = new CompiledServiceHandler<ITestService>(new ServiceMetadata<ITestService>(DirectTypeExpressionConverter.Singleton), DefaultRpcCallingAdapter.Singleton);
            var request = RpcRequest.Create("1", "ExecuteVoid", new object[] { "command" });
            
            // Act
            var response = await handler.HandleRequestAsync(service, request);
            
            // Assert
            Assert.NotNull(response);
            Assert.Null(response.Error);
            Assert.Null(response.Result);
        }

        [Fact]
        public async Task TestMethodNotFound()
        {
            // Arrange
            var service = new TestService();
            var handler = new CompiledServiceHandler<ITestService>(new ServiceMetadata<ITestService>(DirectTypeExpressionConverter.Singleton), DefaultRpcCallingAdapter.Singleton);
            var request = RpcRequest.Create("1", "Multiply", 2, 3);

            // Act
            var response = await handler.HandleRequestAsync(service, request);

            // Assert
            Assert.NotNull(response);
            // Current implementation filters out non-Task methods, so this should return a "not found" error
            Assert.NotNull(response.Error);
            Assert.Equal(404, response.Error.Code);
            Assert.Contains("not found", response.Error.Message);
        }
    }
}