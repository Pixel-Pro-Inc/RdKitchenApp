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
        public static ForcedToLogoutPopup forcedToLogout;

        private static object deserializeAs = new List<List<OrderItem>>();

        // This is so we store the ip of the server if found and user it ONLY if its constantly failing to connect to the server even if it is there.
        // There is already logic to accomodate that 'exceptional' situation
        private static string _ip { get; set; }

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
                // NOTE: I'm assuming if you don't specify the exception you will only catch .NET exceptions
                return false;
            }

            return true;
        }
        public static bool Client_IsConnected()=> client.IsConnected;
        public async static Task CreateClient()
        {
            // NOTE: We can't have this property not be a nullable type cause it will cause an exception. So if we switch this type, please set it to be nullable
            string ip;
            #region Tries to get the server IP

            // Here we are trying to check if it falls into a problem when searching for the server, most likely the overflow
            try
            {
                // We don't need to worry about this string being null. All strings are already nullable.
                ip = await LocalIP.GetServerIP();
            }
            //This is in the event that the overflow happened. Else it would skip this and continued along the block
            catch (FailedToConnectToServerException FailedToConnectToServerException) when(FailedToConnectToServerException.InnerException is StackOverflowException)
            {
                // This is so if it continues to fail, a Hail Mary will be to used, an ip stored
                ip = _ip;
                throw FailedToConnectToServerException;
            }

            #endregion

            #region Retries if the ip still empty due to no connection or some other bug not an overflow

            // REFACTOR: This is a recursive that could easily end up as a stack overflow. We might need to add a count or something to limit the amout of retrys
            // I'm thinking this is where the error is, cause say it doesn't connect the first time, it will fire the same method but this method wasn't finished,
            // basically 'deadlock'
            if (ip == "" && reconnectingPopup == null)
            {
                serverConnectPage.DisplayMessageAlert("Make sure you are connected to a Local Area Connection. You do not need to have internet access but you need a network connection. You can also try restarting the tablet.");

                await Task.Delay(5000);//Retry in 5 seconds

                // NOTE: We don't want to have the retry method show up here cause it handles it's own errors differently. So we will handle here differently in turn
                await CreateClient();
                // UPDATE: @Yewo I added a return so that we don't have the same method being used/ called simutaneously. Hopefully this solves the bug
                return;
            }

            #endregion

            // In the event that we are reconnecting and we still don't have an ip so the unknow bug sends the ip as null still
            // We aren't using a try block here cause if this boolean expression is true then we know something is wrong
            if (ip == "" && reconnectingPopup != null)
            {
                reconnectingPopup.DisplayMessageAlert();
                // This is so we know if there is no ip adress given cause in the event that we eventually get the reconnectpop to be instanciated,
                // there might still be no ip given
                // NOTE: When you throw an exception the method stops executing right after the throw
                // UPDATE: There was a return keyword here. And it has been replaced with the throw keyword in terms of functionality.
                throw new FailedToConnectToServerException("There is no server ip address given, please check your network connection");
            }

            client = new SimpleTcpClient(ip + ":2000");
            client.Events.DataReceived += Events_DataReceived;
            //Im not sure what this does
            client.Events.Disconnected += Events_Disconnected;

            #region Tries to connect the client
            // tries to connect can catches if the ip is still null despite all the checks in place
            try
            {
                client.Connect();
                //NOTE: If the return keyword is done in the try, it will exit the method. if it is done with the method in the try, it follows along the block (obviously)
            }
            // This is in the even that it was connecting for the first time, but we don't really know why it failed to connect
            catch (FailedToConnectToServerException FailedToConnectToServerException)
            {
                Console.WriteLine($"If it gets to this point that means we don't know why its hasn't connected, the error is {0}" +
                    "But this was thrown when connecting to the server for the first time", FailedToConnectToServerException.Message);
                throw FailedToConnectToServerException;
            }

            #endregion

            //Basically checking if we are connecting for the first time or not
            //If first time
            if (reconnectingPopup == null)
                serverConnectPage.Connected();
            //If reconnecting
            if (reconnectingPopup != null)
                reconnectingPopup.Connected();

            // This is a just in case line as a contingency if we experience an overflow when looking for the server ip. It will then try this stored one
            _ip = ip;

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

                byte[] request = requestObject.ToByteArray<RequestObject>();

                string requestString = "[" + Convert.ToBase64String(request);

                client.Send(requestString);

                //NOTE: This is to check if it is a get request. If not, it shouldn't try to get//update the awaitresponse
                // @Yewo: Shouldn't this be a good place to throw an exception or something cause simply throwing the expected this isn't helpful
                if (requestMethod != RequestObject.requestMethod.Get)
                    // UPDATE: @Yewo I made it return null instead
                    return null;
                //return new List<List<OrderItem>>();                

                try
                {
                    await Checkawaitresponse();
                }
                catch (FailedtoRetrieveResponse FailedtoRetrieveResponse)
                {
                    // TODO: Add handling here
                    throw FailedtoRetrieveResponse;
                }
               

                return (List<List<OrderItem>>)awaitresponse;
            }

            Reconnect();
            // Since its not connected to the server 
            throw new NotConnectedToServerException("You aren't connected to the server");
            // UPDATE: I replaced it with the exception instead of just throwing null. There is no use of it being just an ambigous null
            //return null;
        }

        // REFACTOR: Here it is possible that we get into a stack overflow cause it will take too long to actually get those awaitresponse changes
        // so we have to have a try block or an if statement to check if it is even possible to get the awaitresponse or if we have tried too many times to
        //await a response
        // This method was extracted from Sendrequest()
        private static async Task Checkawaitresponse()
        {
            try
            {
                awaitresponse = null; // Set the state as undetermined

                // awaitresponse is flipped by DataRecieved event
                while (awaitresponse == null)
                {
                    await Task.Delay(25);
                }

                // We will have to use this for the logic we create when trying to limit the amount of awaitresponse waiting period or exceptions that could happen
                // This is for when the await response is still null 

                // if( awaitresponse is still null)
                //throw new FailedtoRetrieveResponse("Failed to get the awaitresponse after trying to send the request and so it came up null");
            }
            catch (StackOverflowException StackOverflowException)
            {
                throw new FailedtoRetrieveResponse("Failed to get the awaitresponse after trying to send the request for too long", StackOverflowException);
            }
        }

        // REFACTOR: This method to similar to the Other SendRequest. Consider using a base method or a generic type defined for both types eg. where T is List<OrderItem>, Appuser
        // We are even encountering a possible error here cause we have logic that has to compensate of the situation that the request is not a get request. When we don't need to
        // return anything whatsoever in everyother case
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

                // NOTE: Logic was extracted from here
                try
                {
                    await Checkawaitresponse();
                }
                catch (FailedtoRetrieveResponse FailedtoRetrieveResponse)
                {
                    // TODO: Add handling here same as line 239
                    throw FailedtoRetrieveResponse;
                }

                return (List<AppUser>)awaitresponse;
            }

            Reconnect();

            return null;
        }
        private static void Action()
        {
            Refresh_Action();
            //Tries DataRecieved_Action in case we got the wrong type moving forward
            try
            {
                DataReceived_Action();
            }
            catch (UnexpectedDeserializableObject unexpectedDeserializableObject)
            {
                //I'm thinking it should try to find out if there is someone logged in. If there is someone logged in, it should assume that the data being recieved is an orderItem
                //Then just have it follow through with that
                if (LocalStorage.Chef == null)
                {
                    forcedToLogout = forcedToLogout == null ? new ForcedToLogoutPopup() : forcedToLogout;
                    forcedToLogout.DisplayMessageAlert();
                    // I'm assuming there is alot that needs to be done as you login for things not to break so I want to stop whatever is happening here and start afresh
                    // hence the use of the return keyword
                    return;
                }
                else
                {
                    deserializeAs = new List<List<OrderItem>>();
                }
                DataReceived_Action();
                throw unexpectedDeserializableObject;
            }
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
        private async static void DataReceived_Action()
        {
            if (startCounting_1)
                elapsedTime_1++;

            if (elapsedTime_1 > 0)//Was 1
            {
                startCounting_1 = false;
                elapsedTime_1 = 0;

                // NOTE: This try block is to test if the deserializeAs is the expected type it is supposed to be.
                // Cause maybe we are expecting an OrderItem, but we are still getting it as an AppUser
                // @Yewo: What do you think, let's uncomment this baby. or you know, we can just go into development and do it there

                if (deserializeAs is List<List<OrderItem>>)
                    awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<List<OrderItem>>>();

                if (deserializeAs is List<AppUser>)
                    awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<AppUser>>();

                // If it is neither of the above, you should throw this exception
                if (!(deserializeAs is List<List<OrderItem>> || deserializeAs is List<AppUser>))
                    throw new UnexpectedDeserializableObject("The deserialize object did not come as a List<List<OrderItem>> || List<AppUser>");

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

                        // @Yewo: Why are we waiting 500 seconds here
                        await Task.Delay(delaySeconds * 500);//Was 1000
                    }
                }
            }
        }

        // REFACTOR: Consider making a delegate or finding a timer delegate and have DatabaseChangeListenerUpdate() become an event that will subscribe to it.
        private static void Refresh_Action()
        {
            if (startCounting)
                elapsedTime++;

            if (elapsedTime > 0)//Was 1
            {
                startCounting = false;
                elapsedTime = 0;

                // NOTE: its necessary to have the KitchenApp called cause we want the Viewer to be updated everytime we refresh anyways
                KitchenApp.Instance.DatabaseChangeListenerUpdate();

                // UPDATE: This is where your initial catch statement was.
                // The exception handling is now done within the method. Also if your catch blocks are empty then all they do is catch .NET exceptions
                // And there is no problem with that.
                 
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

           // This shows Popup
            reconnectingPopup = reconnectingPopup == null ? new ReconnectingPopup() : reconnectingPopup;          
        }
    }
}
