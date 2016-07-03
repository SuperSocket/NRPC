using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using NRPC.Base;
using System.IO;

namespace NRPC.Codecs
{
    public class JsonCodec : IRpcCodec
    {
        public InvokeResult DecodeInvokeResult(IList<ArraySegment<byte>> data)
        {
            var length = data.Sum(s => s.Count);
            var result = JObject.Parse(Encoding.UTF8.GetString(data, 0, length));

            return new InvokeResult
            {
                Id = result.Value<int>("Id"),
                Result = result["Result"]
            };
        }

        public InvokeRequest DecodeRequest(IList<ArraySegment<byte>> data)
        {
            var length = data.Sum(s => s.Count);
            var request = JObject.Parse(Encoding.UTF8.GetString(data, 0, length));

            return new InvokeRequest
            {
                Id = request.Value<int>("Id"),
                MethodName = request.Value<string>("MethodName"),
                Arguments = request.Values<JObject>("Arguments").OfType<object>().ToArray()
            };
        }

        public object DecodeResult(object result, Type objectType)
        {
            var resultObj = (JObject)result;
            return resultObj.ToObject(objectType);
        }

        public T DecodeResult<T>(object result)
        {
            var resultObj = (JObject)result;
            return resultObj.ToObject<T>();
        }

        public ArraySegment<byte> Encode(object target)
        {
            // this place could be improved by reuse buffer
            var data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(target));
            return new ArraySegment<byte>(data);
        }
    }
}