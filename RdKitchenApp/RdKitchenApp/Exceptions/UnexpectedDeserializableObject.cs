using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Exceptions
{
    /// <summary>
    /// This is the exception throw most probably in TCPClient in DataRecieved_Action when we don't get the correct deserializable object.
    /// I dont see it happening but if for what ever reason we can get it here. It will also serve well in the future when we refactor our types or include more
    /// Then it will be safer
    /// </summary>
    [Serializable]
    public class UnexpectedDeserializableObject : Exception
    {
        public UnexpectedDeserializableObject() { }
        public UnexpectedDeserializableObject(string message) : base(message) { }
        public UnexpectedDeserializableObject(string message, Exception inner) : base(message, inner) { }
        protected UnexpectedDeserializableObject(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
