
namespace NRPC.Client
{
    public interface IRpcDispatchProxy
    {
        T CreateClient<T>();
    }
}