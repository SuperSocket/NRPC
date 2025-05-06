namespace NRPC.Abstractions
{
    public class RpcError
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object? Data { get; set; }

        public RpcError(int code, string message, object? data = null)
        {
            Code = code;
            Message = message;
            Data = data;
        }

        public override string ToString()
        {
            return $"Code: {Code}, Message: {Message}, Data: {Data}";
        }
    }
}