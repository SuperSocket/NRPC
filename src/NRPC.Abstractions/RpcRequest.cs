namespace NRPC.Abstractions
{
    /// <summary>
    /// Rpc request
    /// </summary>
    public class RpcRequest
    {
        /// <summary>
        ///  The id of the request
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The name of the method which supposed to handle the request
        /// </summary>
        public string MethodName { get; set; }
        
        /// <summary>
        /// The request arguments
        /// </summary>
        public object[] Arguments  { get; set; }
    }
}