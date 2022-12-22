using RdKitchenApp.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RdKitchenApp
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
        private void Button_Clicked(object sender, EventArgs e)
        {
            SaveBranchId(branchId.Text, ipAddress.Text);
        }
        void SaveBranchId(string id, string _ipAddress)
        {
            if (string.IsNullOrEmpty(_ipAddress))
            {
                DisplayAlert("Error", "You need to enter the ip address.", "Close");
                //TODO: Check if formatted correctly
                return;
            }

            if (id != null && id.Length == 5)
            {
                new SerializedObjectManager().SaveData("rd" + id, "BranchId");
                new SerializedObjectManager().SaveData(_ipAddress.Trim(), "ServerIP");

                DataContext.Instance = new DataContext();
                Application.Current.MainPage = new ServerConnect();

                return;
            }

            DisplayAlert("Error", "The id you entered is incorrect", "Close");
        }
    }
}
