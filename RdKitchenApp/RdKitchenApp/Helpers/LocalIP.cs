using RdKitchenApp.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace RdKitchenApp.Helpers
{
    public static class LocalIP
    {
        private static int numRetries = 6;
        private static int delayInSeconds = 2;
        public static List<NetworkInterfaceObject> GetMachineIPv4s()
        {
            List<NetworkInterfaceObject> output = new List<NetworkInterfaceObject>();

            foreach (NetworkInterface item in NetworkInterface.GetAllNetworkInterfaces())
            {
                // Read the IP configuration for each network 
                IPInterfaceProperties properties = item.GetIPProperties();

                if (item.OperationalStatus != OperationalStatus.Up)
                    continue;

                // Each network interface may have multiple IP addresses 
                foreach (IPAddressInformation ip in properties.UnicastAddresses)
                {
                    // We're only interested in IPv4 addresses for now 
                    if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    // Ignore loopback addresses (e.g., 127.0.0.1) 
                    if (IPAddress.IsLoopback(ip.Address))
                        continue;

                    output.Add(new NetworkInterfaceObject()
                    {
                        IPAddress = ip.Address.ToString(),
                        _type = item.NetworkInterfaceType
                    });
                }
            }

            return output;
        }
        public static string GetLocalIPv4()
        {
            List<NetworkInterfaceObject> networkInterfaceObjects = GetMachineIPv4s();
            return networkInterfaceObjects.Count > 0 ? networkInterfaceObjects[0].IPAddress : "1.0.0.0";
        }

        public static string GetBaseIP()
        {
            string ip = GetLocalIPv4();
            string baseIP = "";

            int count = 0;

            for (int i = 0; i < ip.Length; i++)
            {
                if (ip[i] == '.')
                    count++;

                baseIP += ip[i];

                if (count == 3)
                    break;
            }

            return baseIP;
        }

        public async static Task<string> GetServerIP()
        {
            string baseIP = GetBaseIP();
            var result = new SerializedObjectManager().RetrieveData("ServerIP");

            string storedIP = result != null? (string)result: null;

            if (!string.IsNullOrEmpty(storedIP))
            {
                for (int i = 0; i < numRetries; i++)
                {
                    if (await TCPClient.TestIP(storedIP))
                        return storedIP;

                    await Task.Delay(delayInSeconds * 1000);
                }
            }            

            int countEnd = 256, countStart = 128, count = countStart;

            while (true)
            {
                if (await TCPClient.TestIP(baseIP + count))
                {
                    //Store IP
                    new SerializedObjectManager().SaveData(baseIP + count, "ServerIP");
                    return baseIP + count;
                }

                count++;

                if(count >= countEnd)
                {
                    if (countStart == 0)
                        break;

                    countEnd = countStart;
                    count = countStart = (int)((float)countStart / 2f);
                }                
            }

            return "";//If no server is online
        }
    }
}
