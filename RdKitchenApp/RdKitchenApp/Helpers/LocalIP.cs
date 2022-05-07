using RdKitchenApp.Entities;
using RdKitchenApp.Exceptions;
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
            // I am confident that this string will always be either something or "1.0.0.0" cause in GetMachineIPv4s() whether fail or succeed
            // the output will always be of type List<NetworkInterfaceObject> 
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
            // This can't be null, I have checked, for reference check GetBaseIP()
            string baseIP = GetBaseIP();
            // a try block is redundant here cause this method is pretty mutually exclusive
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

            #region This is to search for a new server since we failed to find/ use the server stored

            // This while basically fires indefinetly until it reaches the stop code within it. 
            // REFACTOR: Here is a good place to put in data algorithms. @Yewo you said we employ that shit but I'm sure there is code out there we can borrow
            // that might present themselves more effcient. Like for real this was really smart that you managed this, but if there is something already out there more
            // suited for this problem we should take it
            // UPDATE: I put the block in a try catch so we can catch stackOverflows
            try
            {
                while (true)
                {
                    if (await TCPClient.TestIP(baseIP + count))
                    {
                        //Store IP
                        new SerializedObjectManager().SaveData(baseIP + count, "ServerIP");
                        return baseIP + count;
                    }

                    count++;

                    if (count >= countEnd)
                    {
                        if (countStart == 0)
                            break;
                        // This basically gets it to limit the range it will 
                        countEnd = countStart;
                        count = countStart = (int)((float)countStart / 2f);
                    }
                }
            }
            // I want it to catch the overflow that could havee occured.
            catch (StackOverflowException StackOverflowException)
            {
                throw new FailedToConnectToServerException(" It failed to find the server ip and ended up in a stack overflow", StackOverflowException);
            }
            catch
            {
                throw new FailedToConnectToServerException(" It failed to find the server ip and we are not really sure what happened, so the ip might be null or incorrect");
            }

            // NOTE: Strings are already nullable so we don't have to worry about it causeing an exception in the ConnectClient method
            return "";//If no server is online

            #endregion

        }
    }
}
