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
    public partial class ForcedToLogoutPopup : PopupPage
    {
        // GITRACK: I moved the location of all the pages, but didn't change the files namespace . If we get errors that might be a reasonable cause
        public ForcedToLogoutPopup()
        {
            InitializeComponent();

            this.CloseWhenBackgroundIsClicked = false;

            ShowPage();
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
        private void Bottom_Button_Clicked(object sender, EventArgs e)
        {
            if (block != 0)
                return;

            block = 1;
            block_1 = 0;

            message.IsVisible = false;
            activity.IsVisible = true;
            // Hopefully closes the popup
            Close();
        }
        private void Close()
        {
            try
            {
                PopupNavigation.PopAsync(true);
                // Takes us back to the login. I'm not sure what happens to the view stack, whether it gets restarted or not.
                // UPDATE: It gets replaced as this becomes the new main page
                Application.Current.MainPage = new NavigationPage(new Login());
            }
            catch(Exception)
            {
                return;
            }
        }

        #endregion

    }
}