using System;
using System.Threading.Tasks;

namespace NRPC.Client
{
    public class InvokeState
    {
        public DateTime TimeToTimeOut { get; set; }
        
        public Action<object> ResultHandle { get; set; }
    }
}