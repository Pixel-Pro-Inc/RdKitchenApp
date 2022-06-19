using Plugin.LatestVersion;
using RdKitchenApp.Entities;
using RdKitchenApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RdKitchenApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class Login : ContentPage
    {
        private string Username { get; set; }
        private string Password { get; set; }
        public Login()
        {
            InitializeComponent();

            NavigationPage.SetHasNavigationBar(this, false);

            //@Yewo: Please refactor this code since the plugin no longer works
            //CheckForUpdates();
        }

        private async void CheckForUpdates()
        {
            var isLatest = await CrossLatestVersion.Current.IsUsingLatestVersion();

            if (!isLatest) //If the user does not have the last version
            {
                var update = await DisplayAlert("New version available", 
                    "There is a new version of our app. You have to update as soon as possible to not miss out on crucial changes. Would you like to update now?", 
                    "Yes", "No");

                if (update)
                {
                    //Open the store
                    await CrossLatestVersion.Current.OpenAppInStore();
                }
            }
        }

        int block = 0;
        private void Signin_Button_Clicked(object sender, EventArgs e)
        {
            if (block != 0)
                return;

            block = 1;

            Signin();
        }

        private void Username_Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            Username = GetEntryText(sender);
        }

        private void Password_Entry_TextChanged(object sender, TextChangedEventArgs e)
        {
            Password = GetEntryText(sender);
        }

        private string GetEntryText(object entry)
        {
            return ((Entry)entry).Text;
        }
        private async void _DisplayAlert(string message)
        {
            activity.IsVisible = false;

            await DisplayAlert("Error", message, "Try again");
        }

        private async void Signin()
        {
            //Activity Indicator
            activity.IsVisible = true;

            if (string.IsNullOrEmpty(Username))
            {
                _DisplayAlert("You cannot leave username empty.");
                block = 0;
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                _DisplayAlert("You cannot leave password empty.");
                block = 0;
                return;
            }
            //Get Users
            List<AppUser> accounts = await DataContext.Instance.GetUsers();

            var query = accounts.Where(a => a.UserName.ToLower().Trim() == Username.ToLower().Trim()).ToList();
            if(query.Count == 0)
            {
                _DisplayAlert("Username doesn't exist.");
                block = 0;
                return;
            }

            foreach (var account in query)
            {
                if (account.branchId == null)
                {
                    _DisplayAlert("You are not a staff member of this branch!");
                    block = 0;
                    return;
                }

                string branchId = (new SerializedObjectManager().RetrieveData("BranchId")).ToString();
                if (!account.branchId.Contains(branchId))
                {
                    _DisplayAlert("You are not a staff member of this branch!");
                    block = 0;
                    return;
                }
            }            

            if (!PasswordMatches(query[0], Password))
            {
                _DisplayAlert("Password is incorrect.");
                block = 0;
                return;
            }

            LocalStorage.SetChef(query[0]);

            //Next Page
            //Application.Current.MainPage = new KitchenApp();
            //Makes a new kitchen app page
            NewKitchenPage();
        }

        public async void NewKitchenPage()
        {
            await Navigation.PushAsync(new KitchenApp());
        }

        private bool PasswordMatches(AppUser user, string password)
        {
            var hmac = new HMACSHA512(user.PasswordSalt);

            Byte[] computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != user.PasswordHash[i])
                    return false;
            }

            return true;
        }
    }
}