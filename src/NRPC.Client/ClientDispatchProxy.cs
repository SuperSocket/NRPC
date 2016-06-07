using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NRPC.Base;

namespace NRPC.Client
{
    public abstract class ClientDispatchProxy : DispatchProxy, IRpcDispatchProxy
    {
        protected IServiceProvider ServiceProvider { get; private set; }
        
        private IRpcChannel m_RpcChannel;
        
        private IRpcCodec m_RpcCodec;
        
        private IInvokeRepository m_InvokeRepository;
        
        private static readonly TypeInfo s_TaskType;
        
        private static MethodInfo s_InvokeAsyncMethod;
        private static MethodInfo s_InvokeSyncMethod;
        
        static ClientDispatchProxy()
        {
            s_TaskType = typeof(Task).GetTypeInfo();
            s_InvokeAsyncMethod = typeof(ClientDispatchProxy).GetTypeInfo().GetDeclaredMethod("InvokeAsync");
            s_InvokeSyncMethod = typeof(ClientDispatchProxy).GetTypeInfo().GetDeclaredMethod("InvokeSync");
        }
        
        public ClientDispatchProxy(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            m_RpcChannel = serviceProvider.GetRequiredService<IRpcChannel>();
            m_RpcCodec = serviceProvider.GetRequiredService<IRpcCodec>();
            m_InvokeRepository = serviceProvider.GetRequiredService<IInvokeRepository>();
            HanldeRequestResult();
        }
        
        private async void HanldeRequestResult()
        {
            while (true)
            {
                var data = await m_RpcChannel.ReceiveAsync();
                
                if (data.Count == 0)
                    break;
                    
                var result = m_RpcCodec.DecodeInvokeResult(data);
                
                var invokeState = m_InvokeRepository.TakeInvokeState(result.Id);
                
                invokeState.ResultHandle.BeginInvoke(result.Result, null, null);
            }
        }
        
        private async Task<T> InvokeAsync<T>(MethodInfo targetMethod, object[] args)
        {
            var taskCompletionSrc = CreateInvokeTaskSrc<T>(targetMethod, args);
            return await taskCompletionSrc.Task;
        }
        
        private TaskCompletionSource<T> CreateInvokeTaskSrc<T>(MethodInfo targetMethod, object[] args)
        {
            var taskCompletionSrc = new TaskCompletionSource<T>();
            
            var request = new InvokeRequest
                {
                    MethodName = targetMethod.Name,
                    Arguments = args
                };
                
            request.Id = Guid.NewGuid().GetHashCode();

            SendRequestAsync(taskCompletionSrc, request);
                
            return taskCompletionSrc;
        }

        private async void SendRequestAsync<T>(TaskCompletionSource<T> taskCompletionSrc, InvokeRequest request)
        {
            try
            {
                await m_RpcChannel.SendAsync(m_RpcCodec.Encode(request));
            }
            catch (System.Exception e)
            {
                taskCompletionSrc.SetException(e);
                return;
            }

            m_InvokeRepository.RegisterInvokeState(request.Id,
                new InvokeState
                {
                    TimeToTimeOut = DateTime.Now.AddMinutes(5),
                    ResultHandle = (r) =>
                    {
                        taskCompletionSrc.SetResult(m_RpcCodec.DecodeResult<T>(r));
                    }
                });
        }

        
        private T InvokeSync<T>(MethodInfo targetMethod, object[] args)
        {
            var taskCompletionSrc = CreateInvokeTaskSrc<T>(targetMethod, args);
            var task = taskCompletionSrc.Task;
            task.Wait();
            return task.Result;
        }
        
        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            System.Type resultType = null;
            
            MethodInfo invokeMethod;

            // return a task
            if (s_TaskType.IsAssignableFrom(targetMethod.ReturnType.GetTypeInfo()))
            {
                if (targetMethod.ReturnType.GenericTypeArguments != null)
                    resultType = targetMethod.ReturnType.GenericTypeArguments.FirstOrDefault();
                
                invokeMethod = s_InvokeAsyncMethod;
            }
            else
            {
                resultType = targetMethod.ReturnType;
                invokeMethod = s_InvokeSyncMethod;
            }
            
            return invokeMethod.MakeGenericMethod(resultType).Invoke(this, new object[] { targetMethod, args });
        }

        T IRpcDispatchProxy.CreateClient<T>()
        {
            return CreateClient<T>();
        }
        
        protected abstract T CreateClient<T>();
    }
    
    public abstract class ClientDispatchProxy<TDispatchProxy> : ClientDispatchProxy
        where TDispatchProxy : ClientDispatchProxy
    {
        public ClientDispatchProxy(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }
        protected override T CreateClient<T>()
        {
            return DispatchProxy.Create<T, TDispatchProxy>();
        }
    }
}