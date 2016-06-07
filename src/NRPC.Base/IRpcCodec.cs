using System;

namespace NRPC.Base
{
    public interface IRpcCodec
    {
        object DecodeResult(object result, Type objectType);

        T DecodeResult<T>(object result);
        
        InvokeResult DecodeInvokeResult(ArraySegment<byte> data);
        
        InvokeRequest DecodeRequest(ArraySegment<byte> data);
        
        ArraySegment<byte> Encode(object target);
    }
}