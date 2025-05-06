using System;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Client
{
    class TypedResponseHandler<T> : IResponseHandler
    {
        public object CreateTaskCompletionSource()
        {
            return new TaskCompletionSource<T>();
        }

        public Task GetTask(object taskCompletionSource)
        {
            return ((TaskCompletionSource<T>)taskCompletionSource).Task;
        }

        public void HandleResponse(object taskCompletionSource, RpcResponse response)
        {
            var tcs = taskCompletionSource as TaskCompletionSource<T>;

            if (response.Error != null)
            {
                tcs.SetException(new RpcException("Server side error.", response.Error));
                return;
            }

            tcs.SetResult((T)response.Result);
        }

        public void SetConnectionError(object taskCompletionSource, Exception exception)
        {
            var tcs = taskCompletionSource as TaskCompletionSource<T>;

            tcs?.SetException(exception);
        }
    }
}