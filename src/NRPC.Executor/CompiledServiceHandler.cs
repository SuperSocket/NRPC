using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;

namespace NRPC.Executor
{
    /// <summary>
    /// A high-performance service handler that pre-compiles all service methods during initialization
    /// </summary>
    /// <typeparam name="TService">The type of service to handle</typeparam>
    public class CompiledServiceHandler<TService>
    {
        private readonly ServiceMetadata _serviceMetadata;

        private readonly IRpcCallingAdapter _rpcCallingAdapter;

        /// <summary>
        /// Initializes a new instance of the CompiledServiceHandler and pre-compiles all methods
        /// </summary>
        public CompiledServiceHandler(ServiceMetadata serviceMetadata, IRpcCallingAdapter rpcCallingAdapter)
        {
            _serviceMetadata = serviceMetadata ?? throw new ArgumentNullException(nameof(serviceMetadata));
            _rpcCallingAdapter = rpcCallingAdapter ?? throw new ArgumentNullException(nameof(rpcCallingAdapter));
        }

        /// <summary>
        /// Handles an RPC request by invoking the appropriate pre-compiled method
        /// </summary>
        /// <param name="service">The service instance that will handle the request</param>
        /// <param name="request">The RPC request to handle</param>
        /// <returns>A task containing the RPC response</returns>
        public async Task<RpcResponse> HandleRequestAsync(TService service, RpcRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var response = _rpcCallingAdapter.CreateResponse();
            response.Id = request.Id;

            try
            {
                if (!_serviceMetadata.Methods.TryGetValue(request.Method, out var methodMetadata))
                {
                    response.Error = new RpcError(404, $"Method '{request.Method}' not found on service {typeof(TService).Name}");
                    return response;
                }

                var serviceMethodMetadata = methodMetadata as MethodMetadata<TService>;

                // Invoke the pre-compiled method
                object result = await serviceMethodMetadata.Caller(service, request.Parameters).ConfigureAwait(false);

                response.Id = request.Id;
                response.Result = result;

                return response;
            }
            catch (Exception ex)
            {
                // Unwrap TargetInvocationException
                if (ex is TargetInvocationException tie)
                    ex = tie.InnerException ?? tie;

                response.Error = new RpcError(500, ex.Message);

                return response;
            }
        }
    }
}