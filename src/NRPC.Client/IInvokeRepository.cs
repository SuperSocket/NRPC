using System;

namespace NRPC.Client
{
    public interface IInvokeRepository
    {
        void RegisterInvokeState(int id, InvokeState invokeState);
        
        InvokeState TakeInvokeState(int id);
    }
}