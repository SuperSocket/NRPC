using System;
using System.Text;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NRPC.Base;

namespace NRPC.Codecs
{
    public class JsonCodec : IRpcCodec
    {
        public InvokeResult DecodeInvokeResult(ArraySegment<byte> data)
        {
            var result = JObject.Parse(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count));

            return new InvokeResult
            {
                Id = result.Value<int>("Id"),
                Result = result["Result"]
            };
        }

        public InvokeRequest DecodeRequest(ArraySegment<byte> data)
        {
            var request = JObject.Parse(Encoding.UTF8.GetString(data.Array, data.Offset, data.Count));

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