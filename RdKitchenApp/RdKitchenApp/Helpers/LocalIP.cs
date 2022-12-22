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
using Xamarin.Forms;

namespace RdKitchenApp.Helpers
{
    public static class LocalIP
    {
        private static int numRetries = 6;
        private static int delayInSeconds = 2;
        public async static Task<string> GetServerIP(ContentPage serverConnectPage)
        {
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

            //UI TO ENTER IP ADDRESS
            while (true)
            {
                string promptResult = (await serverConnectPage
                    .DisplayPromptAsync("Prompt", 
                    "Please enter the IP address of the server computer.")).Trim();

                for (int i = 0; i < numRetries; i++)
                {
                    if (await TCPClient.TestIP(promptResult))
                    {
                        new SerializedObjectManager().SaveData(promptResult, "ServerIP");
                        return promptResult;
                    }

                    await Task.Delay(delayInSeconds * 1000);
                }
            }
        }
    }
}
