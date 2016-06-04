using System;
using System.Threading.Tasks;

namespace NRPC.Base
{
    public interface IRpcChannel
    {
        void Send(ArraySegment<byte> data);
        
        Task<ArraySegment<byte>> ReceiveAsync();
    }
}