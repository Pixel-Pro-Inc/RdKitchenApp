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

            StartUp();
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
            await Task.Delay(5000);

            await TCPClient.CreateClient();
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