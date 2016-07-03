using System;
using System.Collections.Generic;

namespace NRPC.Base
{
    /// <summary>
    /// The interface for codecs of invoke request and invoke result
    /// </summary>
    public interface IRpcCodec
    {
        /// <summary>
        /// Deocde the raw result to the result in the exact type
        /// </summary>
        /// <param name="result">the raw result</param>
        /// <param name="objectType">the type of the object which should be returned</param>
        /// <returns>the object in the exact type after decoded</returns>
        object DecodeResult(object result, Type objectType);

        /// <summary>
        /// Deocde the raw result to the result in the exact type
        /// </summary>
        /// <param name="result">the raw result</param>
        /// <returns>the object in the exact type after decoding</returns>
        T DecodeResult<T>(object result);
        
        /// <summary>
        /// Decode the binary data to InvokeResult
        /// </summary>
        /// <param name="data">the raw binary data</param>
        /// <returns>the InvokeResult instance after decoding</returns>
        InvokeResult DecodeInvokeResult(IList<ArraySegment<byte>> data);
        
        /// <summary>
        /// Decode the binary data to InvokeRequest
        /// </summary>
        /// <param name="data">the raw binary data</param>
        /// <returns>the InvokeRequest instance after decoding</returns>
        InvokeRequest DecodeRequest(IList<ArraySegment<byte>> data);
        

        /// <summary>
        /// Encode object to binary data
        /// </summary>
        /// <param name="target">the object to be encoded</param>
        /// <returns>the binary data come from encoding</returns>
        ArraySegment<byte> Encode(object target);
    }
}