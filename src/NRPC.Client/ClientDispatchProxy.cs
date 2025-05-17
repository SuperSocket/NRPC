using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NRPC.Abstractions;
using NRPC.Proxy;

namespace NRPC.Client
{
    public class ClientDispatchProxy : RpcProxy, IDisposable
    {        
        private IRpcConnection m_RpcConnection;

        private IReadOnlyDictionary<MethodInfo, IResponseHandler> m_ResponseHandlers;

        private ConcurrentDictionary<int, InvokeState> m_InvokeStates = new ConcurrentDictionary<int, InvokeState>();

        private CancellationTokenSource m_ReadCancellationTokenSource = new CancellationTokenSource();
        
        public ClientDispatchProxy()
        {
        }

        internal void Initialize(IRpcConnection rpcConnection)
        {
            m_RpcConnection = rpcConnection;
            InitializeResponseHanders();
            StartReadResponse();
        }

        private void StartReadResponse()
        {
            _ = ReadResponseAsync(m_ReadCancellationTokenSource.Token).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    // Handle exception
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
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
                return Activator.CreateInstance(typeof(TypedResponseHandler<>).MakeGenericType(genericArgument)) as IResponseHandler;
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

        private async Task ReadResponseAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var rpcResponse = await m_RpcConnection.ReceiveAsync(cancellationToken);
                
                if (rpcResponse == null)
                    continue;

                if (!m_InvokeStates.TryRemove(rpcResponse.Id, out var invokeState))
                    return;

                invokeState.ResponseHandler.HandleResponse(invokeState.TaskCompletionSource, rpcResponse);
            }                
        }
        
        private async Task<InvokeState> CreateInvoke(MethodInfo targetMethod, object[] args)
        {
            var request = new RpcRequest
                {
                    MethodName = targetMethod.Name,
                    Arguments = args
                };
                
            request.Id = Guid.NewGuid().GetHashCode();

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
            
            if (!m_InvokeStates.TryAdd(request.Id, invokeState))
            {
                throw new Exception($"InvokeState with ID {request.Id} already exists.");
            }

            try
            {
                await m_RpcConnection.SendAsync(request);
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