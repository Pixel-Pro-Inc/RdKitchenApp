using RdKitchenApp.Helpers;
using System;
using System.Globalization;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RdKitchenApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            MainPage = new SplashScreen();
        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
            DataContext.Instance.StartFunction();
        }
    }
}
