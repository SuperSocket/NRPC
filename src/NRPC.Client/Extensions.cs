using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

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
    }
}