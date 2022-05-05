using RdKitchenApp.Entities;
using RdKitchenApp.Exceptions;
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

        private static float elapsedTime_2 = 0;
        private static bool startCounting_2 = false;

        // these are used mostly in Events_DataRecieved
        static int numRetries = 10;
        static int delaySeconds = 2;
        static bool processingRequest;

        // For refresh
        private static float elapsedTime = 0;
        private static bool startCounting = false;

        // For Data_Recieved
        private static float elapsedTime_1 = 0;
        private static bool startCounting_1 = false;

        static object awaitresponse = null;

        #region Events

        private static void Events_Disconnected(object sender, ConnectionEventArgs e)
        {
            startCounting_2 = true;
            elapsedTime_2 = 0;
        }
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

                    await Task.Delay(delaySeconds * 1000);
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

                await Task.Delay(delaySeconds * 1000);
            }
        }

        #endregion

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
        public static bool Client_IsConnected()=> client.IsConnected;

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
        // @Yewo: We don't use block here, so can we remove it?
        static int block = 0;

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

                client.Send(requestObject.ToByteArray<RequestObject>());

                // @Yewo: Shouldn't this be a good place to throw an exception or something cause simply throwing the expected this isn't helpful
                if (requestMethod != RequestObject.requestMethod.Get)
                    return new List<List<OrderItem>>();                

                //await response
                awaitresponse = null; // Set the state as undetermined

                // awaitresponse is flipped by DataRecieved event
                while (awaitresponse == null)
                {
                    await Task.Delay(2500);
                }

                return (List<List<OrderItem>>)awaitresponse;
            }

            Reconnect();

            return null;
        }
        // REFACTOR: This method to similar to the previous one. Consider using a base method or a generic type defined for both types eg. where T is List<OrderItem>, Appuser
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

                client.Send(requestObject.ToByteArray<RequestObject>());

                if (requestMethod != RequestObject.requestMethod.Get)
                    return new List<AppUser>();

                //await response
                awaitresponse = null; // Set the state as undetermined

                while (awaitresponse == null)
                {
                    await Task.Delay(2500);
                }

                return (List<AppUser>)awaitresponse;
            }

            Reconnect();

            return null;
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

            if (elapsedTime_2 > 1)
            {
                startCounting_2 = false;
                elapsedTime_2 = 0;

                Reconnect();
            }
        }
       
        private async static void DataReceived_Action()
        {
            if (startCounting_1)
                elapsedTime_1++;

            if (elapsedTime_1 > 1)
            {
                startCounting_1 = false;
                elapsedTime_1 = 0;

                // NOTE: This try block is to test if the deserializeAs is the expected type it is supposed to be.
                // Cause maybe we are expecting an OrderItem, but we are still getting it as an AppUser
                // @Yewo: What do you think, let's uncomment this baby. or you know, we can just go into development and do it there
                /*
                  try
                {
                    if (deserializeAs is List<List<OrderItem>>)
                        awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<List<OrderItem>>>();

                    if (deserializeAs is List<AppUser>)
                        awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<AppUser>>();
                }
                catch
                {
                    throw new UnexpectedDeserializableObject("The deserialize object did not come as a List<List<OrderItem>> || List<AppUser>");
                }
                 */
                if (deserializeAs is List<List<OrderItem>>)
                    awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<List<OrderItem>>>();

                if (deserializeAs is List<AppUser>)
                    awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<AppUser>>();

                receivedData = "";
                processingRequest = false;
            }
        }

        // REFACTOR: Consider making a delegate or finding a timer delegate and have DatabaseChangeListenerUpdate() become an event that will subscribe to it.
        private static void Refresh_Action()
        {
            if (startCounting)
                elapsedTime++;

            if (elapsedTime > 1)
            {
                startCounting = false;
                elapsedTime = 0;

                try
                {
                    KitchenApp.Instance.DatabaseChangeListenerUpdate();
                }

                // @Yewo: The following blocks are so we can find out why the tablet acts out when it is trying to update menu orders as mentioned in my experience of the bug
                // For a reminder this is what happens:
                //// Encountered that same bug where the tablet doesn't update, then I log out and log back in and it takes forever to log back in
                //   Logged in and it finnally worked but there are now double orders
                //   I think it some how remebered in local but didn't show, and when it finnally synced up, it pulled from the POS itself
                //   It resolved itself when you try to update one of the duplicates
                //   But now when I hit confirm collection its taking spans to load
                //   The loading stopped immeditely I made a new order, but they come up as double orders as well
                //   Tried to update new order, takes forever to process request

                // NOTE: its necessary to have the KitchenApp called cause we want the Viewer to be updated everytime we refresh anyways
                
                catch (UnexpectedDeserializableObject unexpectedDeserializableObject)
                {
                    // Here would be were some error handling would go but I haven't really thought of how to remedy it.
                    // like say they get the wrong type. I only really want the user to know that this happened and log it
                    // Go to the exceptions definition for an understanding of what this should do
                    throw unexpectedDeserializableObject;
                }
                catch (DatabaseChangeListeningException dbChangedListeningException)
                {
                    // I only really expect this to log that this happened. Not that there is any solution I can think of here
                    throw dbChangedListeningException;
                }
                // UPDATE: This is where your initial catch statement was. I don't expect the previous two expections to be caught cause I commented out where they were
                // thrown. Which means the only block that will fire is this catch block. But if your catch blocks are empty then the try keyword as a whole is reduntant
                catch { return; }
                 
            }
        }
       
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
