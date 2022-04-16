using RdKitchenApp.Helpers;
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
    public partial class ServerConnect : ContentPage
    {
        public ServerConnect()
        {
            InitializeComponent();
            StartUp();
        }

        //This prolly exists in case he wants to add more stuff to the start up protocol
        async void StartUp()
        {
            await ConnectToServer();
        }

        public async Task ConnectToServer()
        {
            await Task.Delay(5000);
            TCPClient.serverConnectPage = this;

            await TCPClient.CreateClient();
        }
        public async void DisplayMessageAlert(string msg)
        {
            await DisplayAlert("Alert", msg, "Ok");
        }

        public void Connected()
        {
            Application.Current.MainPage = new NavigationPage(new Login());
        }
    }
}