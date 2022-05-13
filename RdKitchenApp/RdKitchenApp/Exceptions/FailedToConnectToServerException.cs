using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Exceptions
{
    /// <summary>
    /// This exception is expected to be thrown within the TCPClient when trying to create a new Client in CreateClient() or in LocalIP.GetServerIP().
    /// This can happen either from the first time you try to connect to the server or a retry. The exception will most likely be throwm
    /// when retrying. There has also been some logic that works in the event that we don't really know why it hasn't worked ie. the ip will still
    /// be empty or null.
    /// </summary>
    [Serializable]
    public class FailedToConnectToServerException : Exception
    {
        public FailedToConnectToServerException() { }
        public FailedToConnectToServerException(string message) : base(message) { }
        public FailedToConnectToServerException(string message, Exception inner) : base(message, inner) { }
        protected FailedToConnectToServerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
