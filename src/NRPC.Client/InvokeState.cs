using System;
using System.Threading.Tasks;

namespace NRPC.Client
{
    class InvokeState
    {
        public DateTime TimeToTimeOut { get; set; }

        public object TaskCompletionSource { get; set; }
        
        public IResponseHandler ResponseHandler { get; set; }
    }
}