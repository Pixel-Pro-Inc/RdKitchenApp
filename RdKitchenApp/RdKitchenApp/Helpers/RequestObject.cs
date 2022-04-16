using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdKitchenApp.Helpers
{
    [Serializable]
    public class RequestObject
    {
        public enum requestMethod
        {
            Get,
            Store,
            Update,
            Delete
        }

        public string fullPath { get; set; }        
        public requestMethod requestType { get; set; }
        public object data { get; set; }
    }
}
