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

        async void ShowPage()
        {
            await PopupNavigation.PushAsync(this, true);
        }

        async void StartUp()
        {
            await ConnectToServer();
        }

        public async Task ConnectToServer()
        {
            await Task.Delay(5000);// This give the page a chance for the page to show up instead of stealing resources

            Console.WriteLine("We attempting to connect to the server");


            await TCPClient.CreateClient();

            Console.WriteLine("We hit the reconnectingpop up and connected to the server, now we are trying to see if the orderview updates");

            //I'm thinking this where the UpdateOrderView is called  because it has reconnected to the server
            KitchenApp.Instance.UpdateOrderView();
            

            /*
             So when we have this tablet open, we have to find a way to simulate it disconnecting.
            then 
            We need it to hit this break point.
            If that's succesful then it will attempt to updateOrderView
            If thats successful it has to get the new order we have so.......

            Connect to the server.
            Disconnect.
            Add an order
            reconnect
            Check if it reflects
             */

        }
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

        public void Connected()
        {
            try
            {
                TCPClient.reconnectingPopup = null;
                PopupNavigation.PopAsync(true);
            }
            catch
            {
                return;
            }
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
    }
}