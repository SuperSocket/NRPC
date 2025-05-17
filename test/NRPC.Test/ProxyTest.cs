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

        Task Execute(string command);
    }
    
    public class ProxyTest : RpcProxy
    {
        public ProxyTest()
        {
            
        }

        protected override Task<T> Invoke<T>(MethodInfo targetMethod, object[] args)
        {
            if (targetMethod.Name == "Add")
                return Task.FromResult(args.Select(a => (int)a).Sum()) as Task<T>;
            else if (targetMethod.Name == "Concact")
                return Task.FromResult(string.Join("", args.Select(a => (string)a).ToArray())) as Task<T>;
            else
                return Task.Delay(1000).ContinueWith(t => default(T));
        }
        
        [Fact]
        public async Task TestProxyCreation()
        {
            var caculator = RpcProxy.Create<ICaculator, ProxyTest>();            
            Assert.Equal(3, await caculator.Add(1, 2));
            Assert.Equal("Hello World", await caculator.Concact("Hello ", "World"));
            await caculator.Execute("Do something");
        }
    }
}