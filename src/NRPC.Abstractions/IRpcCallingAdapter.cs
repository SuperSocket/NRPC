namespace NRPC.Abstractions
{
    public interface IRpcCallingAdapter
    {
        RpcRequest CreateRequest() => new RpcRequest();

        RpcResponse CreateResponse() => new RpcResponse();
    }
}