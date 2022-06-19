using RdKitchenApp.Helpers;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
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

            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

            MainPage = new SplashScreen();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            SendEmail(e);
        }

        async void SendEmail(UnhandledExceptionEventArgs e)
        {
            var apiKey = "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";

            var client = new SendGridClient(apiKey);

            string userCode = "Rodizio Express Error Logger";

            var recipients = new List<string>() { "pixelprocompanyco@gmail.com",
                "yewotheu123456789@gmail.com","apexmachine2@gmail.com"};

            string _subject = "Kitchen Terminal Error " + System.DateTime.Now.ToString();

            foreach (var reciepient in recipients)
            {
                var from = new EmailAddress("corecommunications2022@gmail.com", userCode);
                var subject = _subject;
                var to = new EmailAddress(reciepient);
                var plainTextContent = _subject;
                var htmlContent = System.DateTime.Now.ToString() + "_" + e.ExceptionObject.ToString();
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                await client.SendEmailAsync(msg).ConfigureAwait(false);
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
