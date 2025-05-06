namespace NRPC.Abstractions
{
    /// <summary>
    /// The invoke result
    /// </summary>
    public class RpcResponse
    {
        /// <summary>
        /// The id of the result which is used for matching its invoke request
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The invoke result data
        /// </summary>
        public object Result { get; set; }

        public RpcError Error { get; set; }
    }
}