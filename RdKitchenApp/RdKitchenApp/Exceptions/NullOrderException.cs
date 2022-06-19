using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Exceptions
{
    /// <summary>
    /// This is an exception made specifically in the case that an order comes up as null. It would fire for what ever not handled exception reason 
    /// that the order does come in null or in the event a developer sets it as null. 
    /// We don't really want it as null cause Abel believes it could cause overflows in certain places.
    /// 
    /// Its also here so we can get a better understanding of what is going on when we encountered a specific bug.
    /// See TCPClient.RefreshAction() for more clarification
    /// </summary>
    [Serializable]
    public class NullOrderException : Exception
    {
        public NullOrderException() { }
        public NullOrderException(string message) : base(message) { }
        public NullOrderException(string message, Exception inner) : base(message, inner) { }
        protected NullOrderException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        // NOTE: If you have to throw this error consider using this handling:
        //_orders = await GetOrderItems();

        // We know that eventually something gets passed out in GetOrderItems() so we use it to set the _orders in the event that they are null.

    }
}
