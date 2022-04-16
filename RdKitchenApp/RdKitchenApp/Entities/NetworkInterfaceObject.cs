using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Text;

namespace RdKitchenApp.Entities
{
    public class NetworkInterfaceObject
    {
        public string IPAddress { get; set; }
        public NetworkInterfaceType _type { get; set; }
    }
}
