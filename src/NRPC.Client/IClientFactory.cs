namespace NRPC.Client
{
    public interface IClientFactory
    {
    }
    
    public interface IClientFactory<T> : IClientFactory
    {
        T CreateClient();
    }
}