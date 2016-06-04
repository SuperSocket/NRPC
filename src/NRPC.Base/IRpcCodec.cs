using System;

namespace NRPC.Base
{
    public interface IRpcCodec
    {
        T DecodeResult<T>(object result);
        
        InvokeResult DecodeInvokeResult(ArraySegment<byte> data);
        
        InvokeRequest DecodeRequest(ArraySegment<byte> data);
        
        ArraySegment<byte> Encode(object target);
    }
}