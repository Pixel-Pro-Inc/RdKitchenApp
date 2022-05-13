using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Exceptions
{
    /// <summary>
    /// I made this so that within TCPClient line 264, in the try catch block we can get more helpful information about why the system acts sus, from
    /// time to time
    /// </summary>
    [Serializable]
    public class DatabaseChangeListeningException : Exception
    {
        public DatabaseChangeListeningException() { }
        public DatabaseChangeListeningException(string message) : base(message) { }
        public DatabaseChangeListeningException(string message, Exception inner) : base(message, inner) { }
        protected DatabaseChangeListeningException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
