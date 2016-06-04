using System.Reflection;
using System.Threading.Tasks;

namespace NRPC.Proxy
{
    public abstract class RpcProxy
    {
        protected abstract Task Invoke<T>(MethodInfo targetMethod, object[] args);
        
        public static T Create<T, TProxy>()
            where TProxy : RpcProxy
        {
            return (T)RpcProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));
        }
    }
}