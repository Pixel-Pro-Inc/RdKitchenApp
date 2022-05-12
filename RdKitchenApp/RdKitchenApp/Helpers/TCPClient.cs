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
        // This is a bool that will essentially check if the awaitresponse variable has NOT been changed to the new one recieved.
        static bool processingRequest;

        // REFACTOR: Consider just replacing these four lines with logic that reuses proccessingRequest
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
                    // Checks if the awaitresponse has been changed. If it has been changed it should look for the new one
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
                // Checks if the awaitresponse has been changed. If it has been changed but it also newer data has now come in it means its trying to finish processing the request
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

        #region SendRequest

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
                    // @Yewo: I changed it from being an infinite loop until blocked
                    await Checkawaitresponse();
                }
                catch (FailedtoRetrieveResponse)
                {
                    // If it failed to get a response it should try to send once again. If it isn't connected there will be expections that handle that
                    // If it gets caught in an overflow, I can't really help there. It will crash the system naturally. I could have all the send request be overrides of
                    //a single method and then wrap that in a try block and then have that wrapped in a single SendRequestPrime() method. But that might be overkill
                    // UPDATE: Instead of calling the static method again, I want to just recall the functionality that we are trying to use
                    client.Send(requestString);
                    await Checkawaitresponse();
                }


                return (List<List<OrderItem>>)awaitresponse;
            }

            Reconnect();
            // Since its not connected to the server 
            throw new NotConnectedToServerException("You aren't connected to the server");
            // UPDATE: I replaced it with the exception instead of just throwing null. There is no use of it being just an ambigous null
            //return null;
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
                    // @Yewo: I changed it from being an infinite loop until blocked
                    await Checkawaitresponse();
                }
                catch (FailedtoRetrieveResponse)
                {
                    // If it failed to get a response it should try to send once again. If it isn't connected there will be expections that handle that
                    // If it gets caught in an overflow, it gets caught in the checkawaitresponse method (hopefully). I could have all the send request be overrides of
                    //a single method and then wrap that in a try block and then have that wrapped in a single SendRequestPrime() method. But that might be overkill

                    // UPDATE: Instead of calling the static method again, I want to just recall the functionality that we are trying to use
                    client.Send(requestString);
                    await Checkawaitresponse();
                    // UPDATE: We remove all the throws of the exception variables cause we don't actually need to do that
                }

                return (List<AppUser>)awaitresponse;
            }

            Reconnect();

            return null;
        }
        // REFACTOR: Here it is possible that we get into a stack overflow cause it will take too long to actually get those awaitresponse changes
        // UPDATE: @Yewo, I set out to remove the overflow. But the draw back is will only check for 5 seconds but aleast it will try 20 times
        // This method was extracted from Sendrequest()
        private static async Task Checkawaitresponse()
        {
            int delay = 25;
            int awaitNumberofRetries = numRetries * 20 * delay; // 25*20 is 500, 500 times 10 retries to get five seconds and we want to attempt it 20 times
            // So that we know how long it was waiting for and it will be mentioned in the exception
            int millisecondsWaited = awaitNumberofRetries;
            try
            {
                // REFACTOR: This is the most faulty line. What if the awaitresponse already came through
                // UPDATE: I made it check if it is still processing request. The moment its not processing, the bool is flipped and awaitresponse is given a value
                // check DataRecieved() line 397 for more context
                // This is to ensure that it will only set awaitresponse to null BEFORE data is recieved
                // Essentially this is to check if a current value of awaitresponse has NOT been changed, it should use a null value instead
                if (processingRequest)
                {
                    // REFACTOR: Consider simply adding old and new value properties and logic to track what is going on here. 
                    // Basically compare recievedData to the new one each time, or some variation of this
                    awaitresponse = null; // Set the state as undetermined
                }


                // awaitresponse is flipped by DataRecieved event
                while (awaitresponse == null)
                {
                    
                    await Task.Delay(delay);
                    // removes the delay time from the total 5 seconds we want it to wait for and its multiplied by 10 so that it only removes 20 times 
                    // UPDATE: it shold run 200 times
                    // @Abel: remove the 250, so remove the 10
                    awaitNumberofRetries -= delay;

                    // Gets out of the while loop if the time has elapsed
                    if (awaitNumberofRetries == 0) break;
                }

                // if the awaitresponse is still null
                if (awaitresponse == null)
                    throw new FailedtoRetrieveResponse($"Failed to get the awaitresponse variable after {millisecondsWaited} milliseconds was waited, trying to get the response from " +
                        $"the request sent, and it still came up null");

            }
            // Now that we put limits in the try block I don't expect this catch block to find an overflow
            catch (StackOverflowException StackOverflowException)
            {
                throw new FailedtoRetrieveResponse("Failed to get the awaitresponse after trying to send the request for too long", StackOverflowException);
            }
        }

        #endregion


        private static void Action()
        {
            Refresh_Action();
            //Tries DataRecieved_Action in case we got the wrong type moving forward
            try
            {
                DataReceived_Action();
            }
            catch (UnexpectedDeserializableObject)
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
            // @Yewo: Why not just replaced this elasped time thingy with the same DataRecieved, except it should return a bool.
            if (startCounting_1)
                elapsedTime_1++;// it was 0

            if (elapsedTime_1 > 0)//it was 1
            {
                startCounting_1 = false;
                elapsedTime_1 = 0;

                //Checks if processing is still happening cause we use this bool when we want to set the awaitresponse in Checkawaitresponse
                // The moment it finds that it is still processing it flips the bool to state that it has finised processing and the awaitresponse is given a value
                // Essentially this is to check if a current value of awaitresponse has NOT been changed, it should set in the new one
                if (processingRequest)
                {
                    processingRequest = false;
                    if (deserializeAs is List<List<OrderItem>>)
                        awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<List<OrderItem>>>();

                    if (deserializeAs is List<AppUser>)
                        awaitresponse = await (Encoding.UTF8.GetBytes(receivedData)).FromByteArray<List<AppUser>>();
                }

                // If it is neither of the above, you should throw this exception
                if (!(deserializeAs is List<List<OrderItem>> || deserializeAs is List<AppUser>))
                    throw new UnexpectedDeserializableObject("The deserialize object did not come as a List<List<OrderItem>> || List<AppUser>");

                receivedData = "";
                processingRequest = false;

                if (awaitresponse == new List<List<OrderItem>>() || awaitresponse == new List<AppUser>())
                {
                    for (int i = 0; i < numRetries; i++)
                    {
                        // I'm assuming this is to check if a current value of awaitresponse has been found, it should check for a new one
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
