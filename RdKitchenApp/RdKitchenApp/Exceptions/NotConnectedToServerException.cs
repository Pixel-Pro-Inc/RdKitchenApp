using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Exceptions
{
    /// <summary>
    /// This will be thrown in TCPClient when an if statements finds that it is not connected to the server. The first expected place found was in
    /// TCPClient when creating the client
    /// </summary>
    [Serializable]
    public class NotConnectedToServerException : Exception
    {
        public NotConnectedToServerException() { }
        public NotConnectedToServerException(string message) : base(message) { }
        public NotConnectedToServerException(string message, Exception inner) : base(message, inner) { }
        protected NotConnectedToServerException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
