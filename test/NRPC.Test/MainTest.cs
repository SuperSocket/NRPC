using System;
using NRPC.Abstractions;
using Xunit;

namespace NRPC.Test
{
    public class MainTest
    {
        
        [Fact]
        public void TestRpcRequest()
        {
            var request = new RpcRequest();
            request.Method = "Test";
            request.Parameters = new object[] { 1, "3", 5.5 };
        }
    }
}