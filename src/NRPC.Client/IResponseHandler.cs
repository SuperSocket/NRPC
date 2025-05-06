using System;
using System.Threading.Tasks;
using NRPC.Abstractions;

namespace NRPC.Client
{
    /// <summary>
    /// Interface for handling the response from the server.
    /// </summary>
    interface IResponseHandler
    {
        /// <summary>
        /// Handle the response from the server
        /// </summary>
        /// <param name="taskCompletionSource">The task completion source to set the result</param>
        /// <param name="response">The response from the server</param>
        /// <returns></returns>
        void HandleResponse(object taskCompletionSource, RpcResponse response);

        /// <summary>
        /// Set the connection error
        /// This is used when the connection is closed or an error occurs
        /// </summary>
        /// <param name="taskCompletionSource">The task completion source to set the result</param>
        /// <param name="exception">The exception.</param>
        void SetConnectionError(object taskCompletionSource, Exception exception);

        /// <summary>
        /// Create a task completion source for the response
        /// </summary>
        object CreateTaskCompletionSource();

        /// <summary>
        /// Get the task from the task completion source
        /// </summary>
        /// <param name="taskCompletionSource">The task completion source.</param>
        Task GetTask(object taskCompletionSource);
    }
}