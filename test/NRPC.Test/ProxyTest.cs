using System;
using System.Threading.Tasks;
using System.Reflection;
using System.Linq;
using NRPC.Proxy;
using Xunit;

namespace NRPC.Test
{
    public interface ICaculator
    {
        Task<int> Add(int x, int y);
        
        Task<string> Concact(string x, string y);
    }
    
    public class ProxyTest : RpcProxy
    {
        protected override Task Invoke<T>(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.Name == "Add")
                return Task.FromResult(args.Select(a => (int)a).Sum());
            else
                return Task.FromResult(string.Join("", args.Select(a => (string)a).ToArray()));
        }
        
        [Fact]
        public async void TestProxyCreation()
        {
            var caculator = RpcProxy.Create<ICaculator, ProxyTest>();            
            Assert.Equal(3, await caculator.Add(1, 2));
            Assert.Equal("Hello World", await caculator.Concact("Hello ", "World"));
        }
    }
}