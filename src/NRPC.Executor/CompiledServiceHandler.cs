using System;
using System.Reflection;
using System.Threading.Tasks;
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

        public CompiledServiceHandler()
            : this(ServiceMetadata.Create<TService>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the CompiledServiceHandler and pre-compiles all methods
        /// </summary>
        public CompiledServiceHandler(ServiceMetadata serviceMetadata)
        {
            _serviceMetadata = serviceMetadata ?? throw new ArgumentNullException(nameof(serviceMetadata));
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

            try
            {
                if (!_serviceMetadata.Methods.TryGetValue(request.Method, out var methodMetadata))
                {
                    return new RpcResponse
                    {
                        Id = request.Id,
                        Error = new RpcError(404, $"Method '{request.Method}' not found on service {typeof(TService).Name}")
                    };
                }

                var serviceMethodMetadata = methodMetadata as MethodMetadata<TService>;

                // Invoke the pre-compiled method
                object result = await serviceMethodMetadata.Caller(service, request.Parameters).ConfigureAwait(false);

                return new RpcResponse
                {
                    Id = request.Id,
                    Result = result
                };
            }
            catch (Exception ex)
            {
                // Unwrap TargetInvocationException
                if (ex is TargetInvocationException tie)
                    ex = tie.InnerException ?? tie;

                return new RpcResponse
                {
                    Id = request.Id,
                    Error = new RpcError(500, ex.Message)
                };
            }
        }
    }
}