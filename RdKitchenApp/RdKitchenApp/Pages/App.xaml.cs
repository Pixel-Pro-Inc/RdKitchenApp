using RdKitchenApp.Entities;
using RdKitchenApp.Helpers;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RdKitchenApp
{
    public partial class App : Application
    {
        private readonly HttpClient client = new HttpClient();
        public App()
        {
            InitializeComponent();
            CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            MainPage = new SplashScreen();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            SendRequest(e);
        }

        async void SendRequest(UnhandledExceptionEventArgs e)
        {
            using (var requestMessage =
            new HttpRequestMessage(HttpMethod.Post, "https://app.rodizioexpress.com/api/errorlog/logerror"))
            {
                //Setting the header of the request to Basic Authorization is required
                //Use of our MerchantAPIKey from Stanbic as the auth token

                //We have to set the body of the request to be the realmName
                //As well as setting the content-type of this body which is JSON value prescribed from NGenius Documentation
                var content = JsonContent.Create(new ErrorLog()
                {
                    Exception = e.ExceptionObject.ToString(),
                    TimeOfException = DateTime.Now,
                    OriginBranchId = (new SerializedObjectManager().RetrieveData("BranchId")).ToString(),
                    OriginDevice = "POS Terminal"
                },
                new MediaTypeHeaderValue("application/json")
                );

                //Here we set the content of the request message with the object we just created for the realmName
                requestMessage.Content = content;

                //We send an asynchronous POST request
                await client.SendAsync(requestMessage);
            }
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
