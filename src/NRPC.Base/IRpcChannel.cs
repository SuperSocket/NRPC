using System;
using System.Threading.Tasks;

namespace NRPC.Base
{
    public interface IRpcChannel
    {
        Task SendAsync(ArraySegment<byte> data);
        
        Task<ArraySegment<byte>> ReceiveAsync();
    }
}