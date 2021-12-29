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
    public partial class SplashScreen : ContentPage
    {
        public SplashScreen()
        {
            InitializeComponent();

            FirebaseDataContext.Instance = new FirebaseDataContext();
            Splash();
        }
        public async void Splash()
        {
            P1.Opacity = 0;
            P2.Opacity = 0;
            await P1.FadeTo(1, 2000);
            await P2.FadeTo(1, 2000);

            if (new SerializedObjectManager().RetrieveData("BranchId") == null)
            {
                Application.Current.MainPage = new MainPage();
                return;
            }

            Application.Current.MainPage = new KitchenApp();
        }
    }
}