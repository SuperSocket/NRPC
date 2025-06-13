using System;
using System.Threading.Tasks;

namespace NRPC.Caller
{
    internal interface IInvokeStateManager
    {
        bool TrySaveInvokeState(string requestId, InvokeState invokeState);
    }
}