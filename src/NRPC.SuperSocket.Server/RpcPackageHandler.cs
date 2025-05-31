using SuperSocket.ProtoBase;
using NRPC.Abstractions;
using SuperSocket.Server.Abstractions;
using System.Threading.Tasks;
using SuperSocket.Server.Abstractions.Session;
using System.Threading;
using NRPC.Executor;

namespace NRPC.SuperSocket.Server
{
    public class RpcPackageHandler<TServiceContract, TRpcRequest, TRpcResponse> : IPackageHandler<TRpcRequest>
        where TServiceContract : class
        where TRpcRequest : RpcRequest
        where TRpcResponse : RpcResponse
    {
        private readonly TServiceContract _service;

        private readonly CompiledServiceHandler<TServiceContract> _compiledServiceHandler;

        private readonly IPackageEncoder<TRpcResponse> _responseEncoder;

        public RpcPackageHandler(TServiceContract service, CompiledServiceHandler<TServiceContract> compiledServiceHandler, IPackageEncoder<TRpcResponse> responseEncoder)
        {
            _service = service;
            _compiledServiceHandler = compiledServiceHandler;
            _responseEncoder = responseEncoder;
        }

        public async ValueTask Handle(IAppSession session, TRpcRequest package, CancellationToken cancellationToken)
        {
            var response = await _compiledServiceHandler.HandleRequestAsync(_service, package);
            await session.SendAsync(_responseEncoder, (TRpcResponse)response, cancellationToken);
        }
    }
}