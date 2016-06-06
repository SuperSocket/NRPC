using System.Reflection;
using System.Threading.Tasks;

namespace NRPC.Proxy
{
    public interface IRpcProxy
    {
        Task Invoke<T>(MethodInfo targetMethod, object[] args);
    }
    
    public abstract class RpcProxy : IRpcProxy
    {
        Task IRpcProxy.Invoke<T>(MethodInfo targetMethod, object[] args)
        {
            return Invoke<T>(targetMethod, args);
        }
        
        protected abstract Task Invoke<T>(MethodInfo targetMethod, object[] args);
        
        public static T Create<T, TProxy>()
            where TProxy : RpcProxy
        {
            return (T)RpcProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));
        }
    }
}