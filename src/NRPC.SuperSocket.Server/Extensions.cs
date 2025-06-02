using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Executor;
using SuperSocket;
using SuperSocket.Server.Abstractions;
using SuperSocket.Server.Abstractions.Host;
using SuperSocket.Server.Abstractions.Session;

namespace NRPC.SuperSocket.Server
{
    public static class Extensions
    {
        public static ISuperSocketHostBuilder<TRpcRequest> UseNRPCService<TRpcRequest, TRpcResponse, TServiceContract, TService>(this ISuperSocketHostBuilder<TRpcRequest> hostBuilder)
            where TRpcRequest : RpcRequest
            where TRpcResponse : RpcResponse
            where TServiceContract : class
            where TService : class, TServiceContract
        {
            return hostBuilder
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<TServiceContract, TService>();
                    services.TryAddSingleton<ServiceMetadata>(sp => ServiceMetadata.Create<TServiceContract>());
                    services.TryAddSingleton<IRpcCallingAdapter>(DefaultRpcCallingAdapter.Singleton); 
                    services.TryAddSingleton<CompiledServiceHandler<TServiceContract>>();
                    services.AddSingleton<IPackageHandler<TRpcRequest>, RpcPackageHandler<TServiceContract, TRpcRequest, TRpcResponse>>();
                });
        }
    }
}