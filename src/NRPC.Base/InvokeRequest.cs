namespace NRPC.Base
{
    public class InvokeRequest
    {
        public int Id { get; set; }
        
        public string MethodName { get; set; }
        
        public object[] Arguments  { get; set; }
        
        public InvokeRequest()
        {

        }
    }
}