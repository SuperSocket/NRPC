using System;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Client
{
    class VoidResponseHandler : TypedResponseHandler<object>
    {
        public override void HandleResponse(object taskCompletionSource, RpcResponse response)
        {
            var tcs = taskCompletionSource as TaskCompletionSource<Object>;

            if (response.Error != null)
            {
                tcs.SetException(new RpcException("Server side error.", response.Error));
            }
            else
            {
                tcs.SetResult(null);
            }
        }
    }
}