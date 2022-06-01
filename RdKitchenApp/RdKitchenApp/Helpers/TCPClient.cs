using RdKitchenApp.Entities;
using RdKitchenApp.Extensions;
using Rg.Plugins.Popup.Services;
using SuperSimpleTcp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RdKitchenApp.Helpers
{
    public static class TCPClient
    {
        public static SimpleTcpClient client = null;
        public static ServerConnect serverConnectPage;
        public static ReconnectingPopup reconnectingPopup;
        private static object deserializeAs = new List<List<OrderItem>>();

        public async static Task<bool> TestIP(string ip)
        {
            client = new SimpleTcpClient(ip + ":2000");
            client.Events.DataReceived += Events_DataReceived;
            client.Events.Disconnected += Events_Disconnected;

            try
            {
                client.ConnectWithRetries(400);
            }
            catch
            {
                return false;
            }

            return true;
        }
        public static bool Client_IsConnected()
        {
            return client.IsConnected;
        }

        public async static Task CreateClient()
        {
            string ip = await LocalIP.GetServerIP();

            if(ip == "" && reconnectingPopup == null)
            {
                serverConnectPage.DisplayMessageAlert("Make sure you are connected to a Local Area Connection. You do not need to have internet access but you need a network connection. You can also try restarting the tablet.");

                await Task.Delay(5000);//Retry in 5 seconds
                await CreateClient();
            }

            if (ip == "" && reconnectingPopup != null)
            {
                reconnectingPopup.DisplayMessageAlert();
                return;
            }

            client = new SimpleTcpClient(ip + ":2000");
            client.Events.DataReceived += Events_DataReceived;
            //Im not sure what this does
            client.Events.Disconnected += Events_Disconnected;

            client.Connect();

            //Basically checking if we are connecting for the first time or not
            //If first time
            if (reconnectingPopup == null)
                serverConnectPage.Connected();
            //If reconnecting
            if (reconnectingPopup != null)
                reconnectingPopup.Connected();

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                Action();

                return true;
            });
        }
        static int block = 0;
        private static void Events_Disconnected(object sender, ConnectionEventArgs e)
        {
            startCounting_2 = true;
            elapsedTime_2 = 0;
        }

        static object awaitresponse = null;
        public static float retryInterval = 5000f;
        public async static Task<List<List<OrderItem>>> SendRequest(object data, string fPath, RequestObject.requestMethod requestMethod)
        {
            if (client.IsConnected)
            {
                RequestObject requestObject = new RequestObject()
                {
                    data = data,
                    fullPath = fPath,
                    requestType = requestMethod,
                };

                deserializeAs = new List<List<OrderItem>>();

                byte[] request = requestObject.ToByteArray<RequestObject>();

                string requestString = "[" + Convert.ToBase64String(request);

                client.Send(requestString);

                if (requestMethod != RequestObject.requestMethod.Get)
                    return new List<List<OrderItem>>();                

                //await response
                awaitresponse = null; // Set the state as undetermined
                float timeElapsed = 0;

                while (awaitresponse == null)
                {
                    await Task.Delay(25);
                    timeElapsed += 25;

                    if(timeElapsed > retryInterval)
                    {
                        timeElapsed = 0;
                        client.Send(requestString);
                    }
                }

                return (List<List<OrderItem>>)awaitresponse;
            }

            Reconnect();

            return null;
        }

        public async static Task<List<AppUser>> SendRequest(string fPath, RequestObject.requestMethod requestMethod)
        {
            if (client.IsConnected)
            {
                RequestObject requestObject = new RequestObject()
                {
                    data = null,
                    fullPath = fPath,
                    requestType = requestMethod,
                };

                deserializeAs = new List<AppUser>();

                byte[] request = requestObject.ToByteArray<RequestObject>();

                string requestString = "[" + Convert.ToBase64String(request);

                client.Send(requestString);

                if (requestMethod != RequestObject.requestMethod.Get)
                    return new List<AppUser>();

                //await response
                awaitresponse = null; // Set the state as undetermined

                float timeElapsed = 0;

                while (awaitresponse == null)
                {
                    await Task.Delay(25);
                    timeElapsed += 25;

                    if (timeElapsed > retryInterval)
                    {
                        timeElapsed = 0;
                        client.Send(requestString);
                    }
                }

                return (List<AppUser>)awaitresponse;
            }

            Reconnect();

            return null;
        }
        static int numRetries = 10;
        static int delaySeconds = 1;//Was 2

        static bool processingRequest;
        private static async void Events_DataReceived(object sender, DataReceivedEventArgs e)
        {
            var response = Encoding.UTF8.GetString(e.Data);

            //Update UI after network change
            if (response.Contains("REFRESH"))
            {
                for (int i = 0; i < numRetries; i++)
                {
                    if (!processingRequest)
                    {
                        Refresh_UI();
                        return;
                    }

                    await Task.Delay(delaySeconds * 500);//Was 1000
                }                
            }

            //Introduced retries to reduce crashes

            Byte[] bytes = Convert.FromBase64String(response);
            string receivedData = Encoding.UTF8.GetString(bytes);

            for (int i = 0; i < numRetries; i++)
            {
                if (!processingRequest || receivedData[0] != '[')
                {
                    DataReceived(receivedData);//There is a data limit for every packet once exceeded is sent in another packet
                    processingRequest = true;
                    break;
                }

                await Task.Delay(delaySeconds * 500);//Was 1000
            }            
        }
        private static void Action()
        {
            Refresh_Action();
            DataReceived_Action();
            Disconnect_Action();
        }
        private static void Disconnect_Action()
        {
            if (startCounting_2)
                elapsedTime_2++;

            if (elapsedTime_2 > 0)//Was 1
            {
                startCounting_2 = false;
                elapsedTime_2 = 0;

                Reconnect();
            }
        }
        private static float elapsedTime_2 = 0;
        private static bool startCounting_2 = false;
        private async static void DataReceived_Action()
        {
            if (startCounting_1)
                elapsedTime_1++;

            if (elapsedTime_1 > 0)//Was 1
            {
                startCounting_1 = false;
                elapsedTime_1 = 0;

                if (deserializeAs is List<List<OrderItem>>)
                    awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<List<OrderItem>>>();

                if (deserializeAs is List<AppUser>)
                    awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<AppUser>>();

                receivedData = "";
                processingRequest = false;

                if (awaitresponse == new List<List<OrderItem>>() || awaitresponse == new List<AppUser>())
                {
                    for (int i = 0; i < numRetries; i++)
                    {
                        if (!processingRequest)
                        {
                            Refresh_UI();
                            return;
                        }

                        await Task.Delay(delaySeconds * 500);//Was 1000
                    }
                }
            }
        }
        private static float elapsedTime_1 = 0;
        private static bool startCounting_1 = false;

        private static void Refresh_Action()
        {
            if (startCounting)
                elapsedTime++;

            if (elapsedTime > 0)//Was 1
            {
                startCounting = false;
                elapsedTime = 0;

                try
                {
                    KitchenApp.Instance.DatabaseChangeListenerUpdate();
                }
                catch
                {
                    return;
                }                
            }
        }
        private static float elapsedTime = 0;
        private static bool startCounting = false;
        private static void Refresh_UI()
        {
            startCounting = true;
            elapsedTime = 0;
        }
        private static string receivedData = "";
        private static void DataReceived(string data)
        {
            startCounting_1 = true;
            elapsedTime_1 = 0;

            receivedData += data;
        }
        private static void Reconnect()
        {
            if (reconnectingPopup != null)
                return;

            //Show Popup
            reconnectingPopup = reconnectingPopup == null ? new ReconnectingPopup() : reconnectingPopup;          
        }
    }
}
