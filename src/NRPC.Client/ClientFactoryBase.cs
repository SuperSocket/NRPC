using System;
using Microsoft.Extensions.DependencyInjection;

namespace NRPC.Client
{
    public abstract class ClientFactoryBase<T> : IClientFactory<T>
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        
        public ClientFactoryBase(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            ServiceProvider = serviceProvider;
        }
        
        public T CreateClient()
        {
            return ServiceProvider.GetService<T>();
        }
    }
}