﻿using RdKitchenApp.Entities;
using RdKitchenApp.Helpers;
using RdKitchenApp.Interfaces;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace RdKitchenApp
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class KitchenApp : ContentPage
    {
        List<List<OrderItem>> _orders = new List<List<OrderItem>>();

        private static readonly HttpClient client = new HttpClient();
        ProcessingPopUp processingPopUp = null;

        public static KitchenApp Instance { get; set; }
        public KitchenApp()
        {            
            InitializeComponent();
            Instance = this;

            NavigationPage.SetHasNavigationBar(this, false);

            UpdateOrderView(); //Display All Eligible Orders On Start
        }
        
        public async void UpdateOrderView(bool newOrders = false)
        {
            orderViewer.Children.Clear();

            activityIndicator.IsVisible = true;

            message.IsVisible = false;

            var result = await GetOrderItems();

            List<List<OrderItem>> orders = result == null? new List<List<OrderItem>>() : result;

            orders = Formatting.ChronologicalOrderList(orders);

            _orders = orders;

            bool skip = false;

            for (int i = 0; i < orders.Count; i++)
            {
                int count = 0;
                foreach (var item in orders[i])
                {
                    if (item.Fufilled == true)
                        count++;

                    skip = false;                    

                    if (count == orders[i].Count)
                    {
                        count = 0;
                        skip = true;
                    }
                }

                if (!skip)
                    orderViewer.Children.Add(GetFrame(orders[i], i));
            }

            if(orderViewer.Children.Count == 0)
            {
                message.IsVisible = true;
            }

            activityIndicator.IsVisible = false;

            if (newOrders)
                DependencyService.Get<INotification>().CreateNotification();
        }        

        public async void DatabaseChangeListenerUpdate()
        {
            List<string> orderNumbers = GetOrderNumbers(_orders);//List of OrderNumbers Stored Locally
            
            List<List<OrderItem>> orders = await GetOrderItems();//Call For Orders From Server

            _orders = orders;

            /*if (orders.Where(o => !orderNumbers.Contains(o[0].OrderNumber)).Count() > 0)//If new items exist add to bottom of list
            {
                //Add new items to bottom of list
                foreach (var order in orders)
                {
                    if (!orderNumbers.Contains(order[0].OrderNumber))
                    {
                        orderNumbers.Add(order[0].OrderNumber);

                        _orders.Add(order);

                        orderViewer.Children.Add(GetFrame(order, orderViewer.Children.Count));
                    }
                }

                if (processingPopUp != null)
                    processingPopUp.Close();

                return;
            }

            //Refresh whole view with edited order items
            
            //Fully Fufilled
            List<string> newOrderNumbers = GetOrderNumbers(orders);//List of OrderNumbers From Server

            //Bindings need to be updated after
            foreach (var orderNumber in orderNumbers)
            {
                if (!newOrderNumbers.Contains(orderNumber))
                {
                    //Has been fully fufilled thus absent from list
                    int count_1 = 0;
                    foreach (var item in _orders)
                    {
                        if(item[0].OrderNumber == orderNumber)
                        {
                            //Remove from viewer
                            orderViewer.Children.RemoveAt(count_1);
                            ResetBindings();
                        }

                        count_1++;
                    }
                }
            }

            _orders = orders;//Set Local to Server Orders

            /*int count = 0;
            foreach (var order in orderViewer.Children)
            {
                //Partially Fufilled
                Frame frame = ((Frame)order);
                StackLayout stackLayout = (StackLayout)frame.Content;

                //Get Label with Order Number String
                StackLayout labelHouser = (StackLayout)stackLayout.Children[0];
                Label label = (Label)labelHouser.Children[0];
                string _OrderNumber = label.Text;

                //Extract Last 4 indexes to get Order Number (e.g 4999)
                _OrderNumber = _OrderNumber.Substring(_OrderNumber.Length - 4, 4);

                StackLayout stackLayout_1 = (StackLayout)stackLayout.Children[1];

                StackLayout stackLayout_2 = (StackLayout)stackLayout_1.Children[3];//Status Holder

                int count_1 = 1;
                foreach (var item in _orders)
                {
                    if (item[0].OrderNumber.Substring(item[0].OrderNumber.IndexOf('_') + 1, 4) == _OrderNumber)
                    {
                        var checkBox = (CheckBox)(stackLayout_2.Children[count_1]);
                        checkBox.IsChecked = item[count_1 - 1].Fufilled;

                        count_1++;
                    }
                }

                count++;
            }*/

            if (processingPopUp != null)
                processingPopUp.Close();

            UpdateOrderView(orders.Where(o => !orderNumbers.Contains(o[0].OrderNumber)).Count() > 0);//Refresh whole view with edited order items
        }
        private void ResetBindings()
        {
            int count = 0;
            foreach (var order in orderViewer.Children)
            {
                //Partially Fufilled
                Frame frame = ((Frame)order);
                StackLayout stackLayout = (StackLayout)frame.Content;

                StackLayout stackLayout_1 = (StackLayout)stackLayout.Children[1];

                StackLayout stackLayout_2 = (StackLayout)stackLayout_1.Children[3];//Status Holder

                for (int i = 1; i < stackLayout_2.Children.Count; i++)
                {
                    var checkBox = (CheckBox)(stackLayout_2.Children[i]);

                    object[] orderIndexPair = { count, (i - 1) };

                    checkBox.BindingContext = orderIndexPair;
                }
                count++;
            }
        }

        List<string> GetOrderNumbers(List<List<OrderItem>> orderItems)
        {
            List<string> orderNumbers = new List<string>();

            foreach (var item in orderItems)
            {
                foreach (var itm in item)
                {
                    if (!orderNumbers.Contains(itm.OrderNumber))
                    {
                        orderNumbers.Add(itm.OrderNumber);
                    }
                }
            }

            return orderNumbers;
        }

        async Task<List<List<OrderItem>>> GetOrderItems()
        {
            List<List<OrderItem>> orders = new List<List<OrderItem>>();

            orders = await DataContext.Instance.GetOrders();

            if (orders == null)
                return null;

            orders = orders.Where(o => o.Where(p => p.Fufilled).ToList().Count != o.Count).ToList();
            orders = orders.Where(o => !o[0].MarkedForDeletion).ToList();

            message.IsVisible = !(orders.Count > 0);

            return orders;
        }

        Frame GetFrame(List<OrderItem> order, int index)
        {
            Frame frame = new Frame()
            {
                BackgroundColor = Color.FromHex("#343434")            
            };

            StackLayout stackLayout = new StackLayout();

            frame.Content = stackLayout;

            StackLayout stackLayout1 = new StackLayout()
            {
                Orientation = StackOrientation.Horizontal
            };

            Label label = new Label()
            {
                FontSize = Device.GetNamedSize(NamedSize.Title, typeof(Label)),
                TextColor = Color.White,
                VerticalTextAlignment = TextAlignment.Center,
                Text = "Order Number - " + order[0].OrderNumber.Substring(order[0].OrderNumber.IndexOf('_') + 1, 4)
            };

            Button button = new Button()
            {
                HorizontalOptions = LayoutOptions.EndAndExpand,
                BackgroundColor = Color.OrangeRed,
                TextColor = Color.White,
                Text = "View",
            };

            button.Clicked += View_Clicked;

            stackLayout1.Children.Add(label);
            stackLayout1.Children.Add(button);

            StackLayout stackLayout2 = new StackLayout()
            {
                Orientation = StackOrientation.Horizontal,
                IsVisible = false
            };

            button.BindingContext = stackLayout2;

            #region Item
            StackLayout stackLayout3 = new StackLayout()
            {
                Margin = new Thickness(10,0,60,0)
            };

            Label label1 = new Label()
            {
                TextColor = Color.White,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                Text = "Name"
            };

            stackLayout3.Children.Add(label1);

            for (int i = 0; i < order.Count; i++)
            {
                Label label2 = new Label()
                {
                    HeightRequest = 30,
                    VerticalTextAlignment = TextAlignment.Center,
                    TextColor = Color.White,
                    Text = order[i].Name
                };

                stackLayout3.Children.Add(label2);
            }
            #endregion

            #region Quantity
            StackLayout stackLayout3_1 = new StackLayout()
            {
                Margin = new Thickness(10, 0, 60, 0)
            };

            Label label1_1 = new Label()
            {
                TextColor = Color.White,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                Text = "Quantity"
            };

            stackLayout3_1.Children.Add(label1_1);

            for (int i = 0; i < order.Count; i++)
            {
                Label label2 = new Label()
                {
                    HeightRequest = 30,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Color.White,
                    Text = order[i].Quantity.ToString()
                };

                stackLayout3_1.Children.Add(label2);
            }
            #endregion

            #region Weight
            StackLayout stackLayout4 = new StackLayout()
            {
                Margin = new Thickness(0, 0, 60, 0)
            };

            Label label3 = new Label()
            {
                TextColor = Color.White,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                Text = "Weight"
            };

            stackLayout4.Children.Add(label3);

            for (int i = 0; i < order.Count; i++)
            {
                Label label4 = new Label()
                {
                    HeightRequest = 30,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Color.White
                };

                if (!string.IsNullOrEmpty(order[i].Weight))
                {
                    string weight = order[i].Weight;

                    if (order[i].Weight.Contains("grams"))
                        weight = order[i].Weight.Replace("grams", "");

                    float weightFloat = 0;

                    weightFloat = float.Parse(weight);//, CultureInfo.InvariantCulture.NumberFormat);

                    label4.Text = order[i].Category == "Meat" ? Formatting.FormatAmountString(weightFloat) + " grams" : "-";
                }
                else
                {
                    label4.Text = "-";
                }
                

                stackLayout4.Children.Add(label4);
            }
            #endregion

            #region Status
            StackLayout stackLayout5 = new StackLayout();

            Label label5 = new Label()
            {
                HorizontalTextAlignment = TextAlignment.Center,
                TextColor = Color.White,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                FontAttributes = FontAttributes.Bold
            };
            stackLayout5.Children.Add(label5);

            for (int i = 0; i < order.Count; i++)
            {
                object[] orderIndexPair = { index, i };

                CheckBox checkBox = new CheckBox()
                {
                    HorizontalOptions = LayoutOptions.Center,
                    BindingContext = orderIndexPair,
                    IsChecked = order[i].Fufilled
                };

                checkBox.CheckedChanged += Status_CheckedChanged;

                stackLayout5.Children.Add(checkBox);
            }
            #endregion

            stackLayout2.Children.Add(stackLayout3);
            stackLayout2.Children.Add(stackLayout3_1);
            stackLayout2.Children.Add(stackLayout4);
            stackLayout2.Children.Add(stackLayout5);

            stackLayout.Children.Add(stackLayout1);
            stackLayout.Children.Add(stackLayout2);

            return frame;
        }

        async Task UpdateDatabase(OrderItem order)
        {
            string branchId = (new SerializedObjectManager().RetrieveData("BranchId")).ToString();
            string fullPath = "Order/" + branchId + "/" + order.OrderNumber + "/" + order.Id.ToString();

            await DataContext.Instance.Update(fullPath, order);
        }

        private void Status_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            int Index = (int)((object[])checkBox.BindingContext)[0];
            int Index1 = (int)((object[])checkBox.BindingContext)[1];

            if (TCPClient.Client_IsConnected())
                _orders[Index][Index1].Fufilled = checkBox.IsChecked;

            _orders[Index][Index1].Chefs = _orders[Index][Index1].Chefs == null ? new List<string>() : _orders[Index][Index1].Chefs;

            if (!_orders[Index][Index1].Chefs.Contains(LocalStorage.Chef.FullName()))
            {
                _orders[Index][Index1].Chefs.Add(LocalStorage.Chef.FullName());
            }

            Logic(Index, Index1);
        }

        async void Logic(int Index, int Index1)
        {
            await UpdateDatabase(_orders[Index][Index1]);

            //Show Popup Message            
            processingPopUp = new ProcessingPopUp();
            await PopupNavigation.PushAsync(processingPopUp, true);

            if (!TCPClient.Client_IsConnected())
                return;

            //Check Order Completed
            int count = _orders[Index].Where(o => o.Fufilled).Count();            

            if (count != _orders[Index].Count)
                return;

            //SendSMS
            string orderNumber = _orders[Index][Index1].OrderNumber.Substring(_orders[Index][Index1].OrderNumber.IndexOf('_') + 1, 4);
            SendSMS(_orders[Index][Index1].PhoneNumber, orderNumber);            
        }

        List<string> sentOutSMS = new List<string>();

        async void SendSMS(string phoneNumber, string orderNumber)
        {
            if (!sentOutSMS.Contains(orderNumber))
            {
                sentOutSMS.Add(orderNumber);
            }
            else
            {
                return;
            }

            try
            {
                await client.PostAsync("https://rodizioexpress.com/api/sms/send/complete/" + phoneNumber + "/" + orderNumber, null);
            }
            catch
            {
                return;
            }        
        }

        private void View_Clicked(object sender, EventArgs e)
        {
            Button b = (Button)sender;

            StackLayout stack = (StackLayout)b.BindingContext;

            stack.IsVisible = !stack.IsVisible;

            if (b.Text == "View")
            {
                b.Text = "Hide";
                return;
            }

            b.Text = "View";
        }

        int block = 0;
        private async void Logout_Button_Clicked(object sender, EventArgs e)
        {
            if (block != 0)
                return;

            block = 1;

            //Next Page
            //Application.Current.MainPage = new Login();
            await Navigation.PushAsync(new Login());//Resets back to login page
        }
    }
}