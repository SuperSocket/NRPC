using System;
using System.IO;
using NRPC.Client;
using Xunit;

namespace NRPC.Test
{
    public class MainTest
    {
        
        [Fact]
        public void TestPhotoBufEncode()
        {
            var request = new InvokeRequest();
            request.MethodName = "Test";
            request.Arguments = new object[] { 1, "3", 5.5 };

            
            Console.WriteLine(Convert.ToBase64String(memorySteam.ToArray()));
        }
    }
}