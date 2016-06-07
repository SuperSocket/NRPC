using System;
using System.Reflection;
using System.Threading.Tasks;

namespace NRPC.Proxy
{
    internal interface IRpcProxy
    {
        Task Invoke<T>(MethodInfo targetMethod, object[] args);
        
        MethodInfo GetMethodInfo(int index);
    }
    
    public abstract class RpcProxy : IRpcProxy
    {
        Task IRpcProxy.Invoke<T>(MethodInfo targetMethod, object[] args)
        {
            return Invoke<T>(targetMethod, args);
        }
        
        MethodInfo IRpcProxy.GetMethodInfo(int index)
        {
            return RpcProxyGenerator.GetMethodInfo(index);
        }
        
        protected abstract Task Invoke<T>(MethodInfo targetMethod, object[] args);
        
        public static T Create<T, TProxy>()
            where TProxy : RpcProxy
        {
            return (T)RpcProxyGenerator.CreateProxyInstance(typeof(TProxy), typeof(T));
        }

        public static Type GetPorxyType<T, TProxy>()
            where TProxy : RpcProxy
        {
            return RpcProxyGenerator.GetProxyType(typeof(TProxy), typeof(T));
        }
    }
}