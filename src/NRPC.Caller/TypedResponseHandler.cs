using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;

namespace NRPC.Caller
{
    class TypedResponseHandler<T> : IResponseHandler, ITypedResponseHandler
    {
        private Func<object, T> _resultConverter;

        public void Initialize(IExpressionConverter expressionConverter)
        {
            var parameterExpression = Expression.Parameter(typeof(object), "result");
            var convertedExpression = expressionConverter.Convert(parameterExpression, typeof(T));

            _resultConverter = Expression.Lambda<Func<object, T>>(convertedExpression, parameterExpression).Compile();
        }

        public object CreateTaskCompletionSource()
        {
            return new TaskCompletionSource<T>();
        }

        public Task GetTask(object taskCompletionSource)
        {
            return ((TaskCompletionSource<T>)taskCompletionSource).Task;
        }

        public virtual void HandleResponse(object taskCompletionSource, RpcResponse response)
        {
            var tcs = taskCompletionSource as TaskCompletionSource<T>;

            if (response.Error != null)
            {
                tcs.SetException(new RpcException("Server side error.", response.Error));
                return;
            }

            tcs.SetResult(_resultConverter(response.Result));
        }

        public void SetConnectionError(object taskCompletionSource, Exception exception)
        {
            var tcs = taskCompletionSource as TaskCompletionSource<T>;

            tcs?.SetException(exception);
        }
    }
}