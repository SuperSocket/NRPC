using System;
using System.Threading.Tasks;

namespace NRPC.Caller
{
    interface IInvokeStateManager
    {
        bool TrySaveInvokeState(string requestId, InvokeState invokeState);
    }
}