using System;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Caller
{
    class VoidResponseHandler : TypedResponseHandler<object>
    {
        public override void HandleResponse(object taskCompletionSource, RpcResponse response)
        {
            var tcs = taskCompletionSource as TaskCompletionSource<Object>;

            if (response.Error != null)
            {
                tcs.SetException(new RpcServerException("Server side error.", response.Error));
            }
            else
            {
                tcs.SetResult(null);
            }
        }
    }
}