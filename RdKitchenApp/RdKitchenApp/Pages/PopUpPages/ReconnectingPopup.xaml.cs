using RdKitchenApp.Exceptions;
using RdKitchenApp.Helpers;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RdKitchenApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ReconnectingPopup : PopupPage
    {
        public ReconnectingPopup()
        {
            InitializeComponent();            

            this.CloseWhenBackgroundIsClicked = false;

            ShowPage();

            StartUp();// Basically tries to connect to the server
        }

        async void StartUp()=> await ConnectToServer();
        public async Task ConnectToServer()
        {
            try
            {
                await Task.Delay(5000);// This give the page a chance for the page to show up instead of stealing resources

                await TCPClient.CreateClient();
            }
            catch(FailedToConnectToServerException FailedToConnectToServerException)
            {
                // We don't need to do any handling here cause it already prints the message to the user about network connection. But its necessary to have the exception
                // caught here in the event that that unknown reason for the error pops up ( it results in an empty ip). Also I want to log all the exceptions thrown
                // at some point. And I just haven't figured out how yet.
                // NOTE: Remember, always throw the exception you just caught
                Console.WriteLine($"If it gets to this point that means we don't know why its hasn't connected, the error is {0}" +
                   "But this was thrown when reconnecting", FailedToConnectToServerException.Message);
                throw FailedToConnectToServerException;
            }
        }
        //This is the only place where we know it will be connected. Everywhere else it hasn't.
        public void Connected()
        {
            try
            {
                TCPClient.reconnectingPopup = null;
                PopupNavigation.PopAsync(true);

                //This basically makes sure that everytime you reconnect to the server you have to login. 
                //This is a good solution cause everytime you login it clears and retakes all the orders from the Computer Client
                KitchenApp.Instance.LoginPage();

            }
            catch (FailedToConnectToServerException FailedToConnectToServerException)
            {
                Console.WriteLine($"If it gets to this point that means we don't know why its hasn't connected, the error is {0}" +
                    "But this was thrown when reconnecting", FailedToConnectToServerException.Message);
                throw FailedToConnectToServerException;
            }
        }
        #region View

        async void ShowPage() => await PopupNavigation.PushAsync(this, true);
        int block_1 = 0;
        public void DisplayMessageAlert()
        {
            if (block_1 != 0)
                return;

            block_1 = 1;

            message.IsVisible = true;
            activity.IsVisible = false;

            block = 0;
        }
        int block = 0;
        private void Retry_Button_Clicked(object sender, EventArgs e)
        {
            if (block != 0)
                return;

            block = 1;
            block_1 = 0;

            message.IsVisible = false;
            activity.IsVisible = true;

            StartUp();
        }

        #endregion

    }
}