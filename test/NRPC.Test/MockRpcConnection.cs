using System;
using System.Threading.Channels;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Executor;

namespace NRPC.Test
{
    // A mock implementation of IRpcConnection for testing
    internal class MockRpcConnection : IRpcConnection
    {
        private readonly Channel<RpcResponse> _responses = Channel.CreateUnbounded<RpcResponse>();

        private CompiledServiceHandler<ITestService> _handler = new CompiledServiceHandler<ITestService>(new ServiceMetadata<ITestService>(DirectTypeExpressionConverter.Singleton), DefaultRpcCallingAdapter.Singleton);

        private ITestService _testService;

        public RpcRequest LastSentRequest { get; private set; }

        public bool IsConnected { get; private set; } = true;

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
            LastSentRequest = request;
            var response = await _handler.HandleRequestAsync(_testService, request);
            _responses.Writer.TryWrite(response);
        }

        // Implement ReceiveAsync to return queued responses
        public async Task<RpcResponse> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            return await _responses.Reader.ReadAsync(cancellationToken);
        }

        public void Dispose()
        {
            if (IsConnected)
            {
                IsConnected = false;
                _responses.Writer.Complete();
            }
        }
        
        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}