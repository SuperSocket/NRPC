using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using NRPC.Base;

namespace NRPC.Client
{
    public static class Extensions
    {
        public static IServiceCollection AddClientPorxy<T>(this ServiceCollection services)
            where T : class
        {
            if(!typeof(T).GetTypeInfo().IsInterface)
                throw new ArgumentException("The type argument must be an interface", nameof(T));

            var proxyType = ClientDispatchProxy.GetPorxyType<T, ClientDispatchProxy>();

            return services.AddTransient(typeof(T), proxyType);
        }

        public static IServiceCollection AddCodec<T>(this ServiceCollection services)
            where T : class, IRpcCodec
        {
            return services.AddTransient<IRpcCodec, T>();
        }

        public static IServiceCollection AddChannel<T>(this ServiceCollection services)
            where T : class, IRpcChannel
        {
            return services.AddTransient<IRpcChannel, T>();
        }
    }
}