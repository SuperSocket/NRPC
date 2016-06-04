using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace NRPC.Client
{
    public abstract class ClientFactoryBase<T> : IClientFactory<T>
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        
        private IRpcDispatchProxy m_DispatchProxy;
        
        public ClientFactoryBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            m_DispatchProxy = serviceProvider.GetRequiredService<IRpcDispatchProxy>();
        }
        
        public T CreateClient()
        {
            return m_DispatchProxy.CreateClient<T>();
        }
    }
}