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
    public partial class ProcessingPopUp : PopupPage
    {
        public ProcessingPopUp()
        {
            InitializeComponent();
            this.CloseWhenBackgroundIsClicked = false;
        }

        public void Close()
        {
            try
            {
                PopupNavigation.PopAsync(true);
            }
            catch
            {
                return;
            }
        }
    }
}