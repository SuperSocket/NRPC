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
        public object[] Arguments { get; set; }
        
        /// <summary>
        /// Creates a new RPC request with the specified parameters
        /// </summary>
        /// <param name="id">The request ID</param>
        /// <param name="methodName">The name of the method to call</param>
        /// <param name="arguments">The arguments to pass to the method</param>
        /// <returns>A new RpcRequest instance</returns>
        public static RpcRequest Create(int id, string methodName, params object[] arguments)
        {
            return new RpcRequest
            {
                Id = id,
                MethodName = methodName,
                Arguments = arguments
            };
        }
    }
}