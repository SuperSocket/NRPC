using System;
using System.IO;
using System.Threading.Tasks;
using System.Reflection;
using NRPC.Client;
using NRPC.Proxy;
using Xunit;

namespace NRPC.Test
{
    public interface ICaculator
    {
        Task<int> Add(int x, int y);
    }
    
    public class ProxyTest : RpcProxy
    {
        protected override Task Invoke<T>(MethodInfo targetMethod, object[] args)
        {
            throw new NotImplementedException();
        }
        
        [Fact]
        public void TestProxyCreation()
        {
            var caculator = RpcProxy.Create<ICaculator, ProxyTest>();
            
            Assert.Throws(typeof(NotImplementedException), () =>
            {
                caculator.Add(1, 2);
            });
        }
    }
}