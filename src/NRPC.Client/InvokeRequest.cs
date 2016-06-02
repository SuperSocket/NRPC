namespace NRPC.Client
{
    public class InvokeRequest
    {
        public string MethodName { get; set; }
        
        public object[] Arguments  { get; set; }
    }
}