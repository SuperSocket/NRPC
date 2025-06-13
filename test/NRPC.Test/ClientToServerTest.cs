using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Caller;
using NRPC.Caller.Connection;
using NRPC.Executor;
using Xunit;
using Xunit.Sdk;

namespace NRPC.Test
{
    public class ClientToServerTest
    {
        [Fact]
        public async Task TestBasicRpcWorkflow()
        {
            var factory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(new MockRpcConnection()));
            var client = factory.CreateCaller(TestContext.Current.CancellationToken);

            Assert.Equal(3, await client.Add(1, 2));
            Assert.Equal("123", await client.Concat("1", "23"));
            await client.ExecuteVoid("Test");
        }

        [Fact]
        public async Task TestRpcException()
        {
            var factory = new RpcCallerFactory<ITestService>(new MockRpcConnectionFactory(new MockRpcConnection(new TestServiceWithException())));
            var client = factory.CreateCaller(TestContext.Current.CancellationToken);

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
