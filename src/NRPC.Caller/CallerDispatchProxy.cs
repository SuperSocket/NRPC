using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NRPC.Abstractions;
using NRPC.Abstractions.Metadata;
using NRPC.Caller.Connection;
using NRPC.Proxy;

namespace NRPC.Caller
{
    public class CallerDispatchProxy : RpcProxy, IDisposable
    {
        private IInvokeStateManager m_InvokeStateManager;
        private IConnectionManager<IRpcConnection> m_ConnectionManager;

        private IRpcCallingAdapter m_RpcCallingAdapter;

        private IExpressionConverter m_ResultExpressionConverter;
        
        private IReadOnlyDictionary<MethodInfo, IResponseHandler> m_ResponseHandlers;

        private CancellationTokenSource m_ReadCancellationTokenSource = new CancellationTokenSource();
        
        public CallerDispatchProxy()
        {
        }

        internal void Initialize(IConnectionManager<IRpcConnection> connectionManager, IInvokeStateManager invokeStateManager, IRpcCallingAdapter rpcCallingAdapter, IExpressionConverter resultExpressionConverter)
        {
            m_ConnectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            m_InvokeStateManager = invokeStateManager ?? throw new ArgumentNullException(nameof(invokeStateManager));
            m_RpcCallingAdapter = rpcCallingAdapter ?? throw new ArgumentNullException(nameof(rpcCallingAdapter));
            m_ResultExpressionConverter = resultExpressionConverter ?? throw new ArgumentNullException(nameof(resultExpressionConverter));

            InitializeResponseHanders();
        }

        private IResponseHandler CreateResponseHandler(MethodInfo methodInfo)
        {
            if (methodInfo.ReturnType == typeof(Task))
            {
                return new VoidResponseHandler();
            }
            else if (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var genericArgument = methodInfo.ReturnType.GenericTypeArguments[0];
                var responseHandler = Activator.CreateInstance(typeof(TypedResponseHandler<>).MakeGenericType(genericArgument)) as ITypedResponseHandler;
                responseHandler.Initialize(m_ResultExpressionConverter);
                return responseHandler;
            }
            else
            {
                throw new NotSupportedException($"Method {methodInfo.Name} return type is not supported.");
            }
        }

        private void InitializeResponseHanders()
        {
            var serviceContractAtribute = typeof(ServiceContractAtribute);
            var serviceContract = this.GetType().GetInterfaces().FirstOrDefault(
                t => t.GetCustomAttribute(serviceContractAtribute) != null);

            var taskType = typeof(Task);

            m_ResponseHandlers = serviceContract.GetMethods()
                .ToDictionary(
                    m => m,
                    m => CreateResponseHandler(m));
        }
        
        private RpcRequest CreateRequest(MethodInfo targetMethod, object[] args)
        {
            var request = m_RpcCallingAdapter.CreateRequest();

            request.Method = targetMethod.Name;
            request.Parameters = args;
            request.Id = Guid.NewGuid().ToString();
            
            return request;
        }
        
        private async Task<InvokeState> CreateInvoke(MethodInfo targetMethod, object[] args)
        {
            var request = CreateRequest(targetMethod, args);

            var responseHandler = m_ResponseHandlers[targetMethod];

            return await SendRequestAsync(request, responseHandler);
        }

        private async Task<InvokeState> SendRequestAsync(RpcRequest request, IResponseHandler responseHandler)
        {
            var taskCompletionSrc = responseHandler.CreateTaskCompletionSource();

            var invokeState = new InvokeState
            {
                TaskCompletionSource = taskCompletionSrc,
                TimeToTimeOut = DateTime.UtcNow.AddSeconds(30),
                ResponseHandler = responseHandler
            };
            
            if (!m_InvokeStateManager.TrySaveInvokeState(request.Id, invokeState))
            {
                throw new Exception($"InvokeState with ID {request.Id} already exists.");
            }

            try
            {
                using var connectionCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                var connection = await m_ConnectionManager.GetConnectionAsync(connectionCts.Token).ConfigureAwait(false);
                await connection.SendAsync(request).ConfigureAwait(false);
                m_ConnectionManager.ReturnConnection(connection);
            }
            catch (Exception e)
            {
                responseHandler.SetConnectionError(taskCompletionSrc, e);
            }

            return invokeState;
        }

        protected override async Task<T> Invoke<T>(MethodInfo targetMethod, object[] args)
        {
            var invokeState = await CreateInvoke(targetMethod, args);
            var result = await (invokeState.ResponseHandler.GetTask(invokeState.TaskCompletionSource) as Task<T>);
            return result;
        }

        public void Dispose()
        {
            m_ReadCancellationTokenSource.Cancel();
            m_ReadCancellationTokenSource.Dispose();
        }
    }
}