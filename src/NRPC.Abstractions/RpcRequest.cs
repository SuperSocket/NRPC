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
        public string Id { get; set; }
        
        /// <summary>
        /// The name of the method which supposed to handle the request
        /// </summary>
        public string Method { get; set; }
        
        /// <summary>
        /// The request parameters.
        /// </summary>
        public object[] Parameters { get; set; }

        /// <summary>
        /// Creates a new RPC request with the specified parameters
        /// </summary>
        /// <param name="id">The request ID</param>
        /// <param name="method">The name of the method to call</param>
        /// <param name="parameters">The parameters to pass to the method</param>
        /// <returns>A new RpcRequest instance</returns>
        public static RpcRequest Create(string id, string method, params object[] parameters)
        {
            return new RpcRequest
            {
                Id = id,
                Method = method,
                Parameters = parameters
            };
        }
    }
}