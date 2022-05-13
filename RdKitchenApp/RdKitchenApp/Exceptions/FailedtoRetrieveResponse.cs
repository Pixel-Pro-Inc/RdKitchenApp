using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Exceptions
{
    /// <summary>
    /// I expect to throw this when a Request was sent to the SendRequest Method in the TCPClient but we weren't getting the response.
    /// For example, we took a long time and the awaitresponse property still null or other reasons as to have it fail to get the response.
    /// Stay tuned for more clarification. This could also be used in other places
    /// </summary>
    [Serializable]
    public class FailedtoRetrieveResponse : Exception
    {
        public FailedtoRetrieveResponse() { }
        public FailedtoRetrieveResponse(string message) : base(message) { }
        public FailedtoRetrieveResponse(string message, Exception inner) : base(message, inner) { }
        protected FailedtoRetrieveResponse(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
