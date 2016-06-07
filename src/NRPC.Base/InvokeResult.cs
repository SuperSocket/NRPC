namespace NRPC.Base
{
    /// <summary>
    /// The invoke result
    /// </summary>
    public class InvokeResult
    {
        /// <summary>
        /// The id of the result which is used for matching its invoke request
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// The invoke result data
        /// </summary>
        public object Result { get; set; }
    }
}