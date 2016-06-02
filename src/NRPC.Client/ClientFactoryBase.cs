using System.Reflection;

namespace NRPC.Client
{
    public abstract class ClientFactoryBase<T> : IClientFactory<T>
    {
        public T CreateClient()
        {
            return DispatchProxy.Create<T, ClientDispatchProxy>();
        }
    }
}