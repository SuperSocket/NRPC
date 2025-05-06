using System;
using System.IO;
using NRPC.Abstractions;
using NRPC.Client;
using Xunit;

namespace NRPC.Test
{
    public class MainTest
    {
        
        [Fact]
        public void TestRpcRequest()
        {
            var request = new RpcRequest();
            request.MethodName = "Test";
            request.Arguments = new object[] { 1, "3", 5.5 };
        }
    }
}