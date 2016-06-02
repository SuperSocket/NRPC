using System;
using System.Reflection;

namespace NRPC.Client
{
    public class ClientDispatchProxy : DispatchProxy
    {
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            var request = new InvokeRequest
                {
                    MethodName = targetMethod.Name,
                    Arguments = args
                };
                
            throw new NotImplementedException();
        }
    }
}