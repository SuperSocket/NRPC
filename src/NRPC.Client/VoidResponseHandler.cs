using System;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Client
{
    public class VoidResponseHandler : IResponseHandler
    {
        public object CreateTaskCompletionSource()
        {
            return new TaskCompletionSource();
        }

        public Task GetTask(object taskCompletionSource)
        {
            return ((TaskCompletionSource)taskCompletionSource).Task;
        }

        public void HandleResponse(object taskCompletionSource, RpcResponse response)
        {
            var tcs = taskCompletionSource as TaskCompletionSource;

            if (response.Error != null)
            {
                tcs.SetException(new RpcException("Server side error.", response.Error));
            }
            else
            {
                tcs.SetResult();
            }
        }

        public void SetConnectionError(object taskCompletionSource, Exception exception)
        {
            var tcs = taskCompletionSource as TaskCompletionSource;

            if (tcs != null)
            {
                tcs.SetException(exception);
            }
        }
    }
}