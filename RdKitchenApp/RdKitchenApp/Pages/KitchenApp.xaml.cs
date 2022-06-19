using RdKitchenApp.Entities;
using RdKitchenApp.Exceptions;
using RdKitchenApp.Helpers;
using RdKitchenApp.Interfaces;
using Rg.Plugins.Popup.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Xamarin.Essentials;
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

            versionText.Text = "Version: " + AppInfo.Version + "_" + AppInfo.BuildString;

            UpdateOrderView(); //Display All Eligible Orders On Start
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

        #region Download

        List<string> GetOrderNumbers(List<List<OrderItem>> orderItems)
        {
            List<string> orderNumbers = new List<string>();

            // I'm assuming that if Orders are passedd into this as a list we don't expect them to ever be null. Which is why I'm throwing this expection in that case
            // but there is also the case where there are no orders but if that's the case orders will exist, but the count will be zero
            if (orderItems == null) throw new NullOrderException("The orders given were null, they should at least be empty/ count==0", 
                new NullReferenceException("If global orders were passed in as null, we aren't supposed to have that. Never set it to null"));

            // NOTE: So I'm thinking this might be a good place to have the dictionary types. Only because I think it might be easier 
            // to do data algorithms with. Since they can work with keys and such
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

            try
            {
                // Its possible for result to be set to null, for example, when the tablet is disconnected from the server. but we really don't want it to be null
                orders = await DataContext.Instance.GetOrders();
            }
            // We have the two catch blocks here cause there isn't a way to put them in the same block but we need both explicitly mentioned cause we need to know what happened
            catch (FailedToConnectToServerException)
            {
                //This ensures the orders don't come in as null
                orders = orders == null ? new List<List<OrderItem>>() : orders;
            }
            catch (NotConnectedToServerException)
            {
                //This ensures the orders don't come in as null
                orders = orders == null ? new List<List<OrderItem>>() : orders;
            }

            // It is possible for there to be null orders. But we don't want them. There is handling to make sure, but in case they don't catch it this is here
            if (orders == null)
                throw new NullReferenceException("Orders are never expected to be null since we have exception handling to ensure it doesn't happen ");

            orders = orders.Where(o => o.Where(p => p.Fufilled).ToList().Count != o.Count).ToList();
            orders = orders.Where(o => !o[0].MarkedForDeletion).ToList();

            message.IsVisible = !(orders.Count > 0);

            return orders;
        }

        #endregion

        #region Update

        //This basically gets the orders. We need this called when we reconnect to the server. 
        public async void UpdateOrderView(bool newOrders = false)
        {
            orderViewer.Children.Clear();

            activityIndicator.IsVisible = true;

            message.IsVisible = false;

            List<List<OrderItem>> orders = await GetOrderItems();

            orders = Formatting.ChronologicalOrderList(orders);

            _orders = orders;

            bool skip = false;
            // @Yewo: I kinda need you explain what you are trying to do here specifically
            // I believe it is trying to check in every order which orderItem has been fufilled,
            // It skips the order that has all the orders fullfilled from showing and adds them when
            // they aren't 
            // REFACTOR: Consider extracting this logic cause it is important and we need a closer eye on this to maintain, maybe call it, FindFufilled, then maybe
            // inject the function into it
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

                var frames = orderViewer.Children;
                foreach (var frame in frames)
                {
                    if (((Frame)frame).BindingContext as string == orders[i][0].OrderNumber)
                        skip = true;
                }

                if (!skip)
                {
                    // Adds the order to the screen by giving the parameters of the order, and the index of the order
                    orderViewer.Children.Add(GetFrame(orders[i], i));
                }

            }

            if (orderViewer.Children.Count == 0)
            {
                message.IsVisible = true;
            }

            activityIndicator.IsVisible = false;

            if (newOrders)
                DependencyService.Get<INotification>().CreateNotification();

        }
        public async void DatabaseChangeListenerUpdate()
        {
            List<string> orderNumbers=new List<string>();//List of OrderNumbers Stored Locally

            List<List<OrderItem>> orders;//Call For Orders From Server

            // This try block is to see if it fails to get the data it needs
            try
            {
                 orderNumbers = GetOrderNumbers(_orders);//List of OrderNumbers Stored Locally
            }
            catch(NullOrderException)
            {
                // We know that eventually something gets passed out in GetOrderItems() so we use it to set the _orders in the even that they are null.
                _orders = await GetOrderItems(); 
            }
            catch // This is here cause Yewo had something similiar, and it serves to catch .NET exceptions
            { return; }

            orders = await GetOrderItems();//Call For Orders From Server
            // We use orders to continue to calculate so that's why we have these two lines
            _orders = orders;

            #region collapse
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
            #endregion

            if (processingPopUp != null)
                processingPopUp.Close();

            // UPDATE: I removed the try block here cause I don't expect it to ever fail
            UpdateOrderView(orders.Where(o => !orderNumbers.Contains(o[0].OrderNumber)).Count() > 0);//Refresh whole view with edited order items

        }
        async Task UpdateDatabase(OrderItem order)
        {
            string branchId = (new SerializedObjectManager().RetrieveData("BranchId")).ToString();
            string fullPath = "Order/" + branchId + "/" + order.OrderNumber + "/" + order.Id.ToString();

            await DataContext.Instance.Update(fullPath, order);
        }

        async Task<List<List<OrderItem>>> GetOrderItems()
        {
            List<List<OrderItem>> orders = new List<List<OrderItem>>();

            //The following block is my attempt at removing an error
            //Apparently at certain times the tablets duplicate orders on the view
            //I postulate that this is due to some unexpected behaviour with our current network technology(TCP)
            List<string> orderNumbers = new List<string>();
            var _orders = await DataContext.Instance.GetOrders();
            foreach (var item in _orders)
            {
                //if the order has not already been added to the list add it
                if (!orderNumbers.Contains(item[0].OrderNumber))
                {
                    orderNumbers.Add(item[0].OrderNumber);
                    orders.Add(item);
                }
            }
            //End

            if (orders == null)
                return null;

            orders = orders.Where(o => o.Where(p => p.Fufilled).ToList().Count != o.Count).ToList();
            orders = orders.Where(o => !o[0].MarkedForDeletion).ToList();

        #region View Logic

        Frame GetFrame(List<OrderItem> order, int index)
        {
            #region Layout

            Frame frame = new Frame()
            {
                BackgroundColor = Color.FromHex("#343434"),
                BindingContext = order[0].OrderNumber
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

            #endregion

            #region Item
            StackLayout stackLayout3 = new StackLayout()
            {
                Margin = new Thickness(10, 0, 10, 0)
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
                Margin = new Thickness(10, 0, 10, 0)
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
                Margin = new Thickness(0, 0, 10, 0)
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

            #region Flavour
            StackLayout stackLayout4_1 = new StackLayout()
            {
                Margin = new Thickness(0, 0, 10, 0)
            };

            Label label3_1 = new Label()
            {
                TextColor = Color.White,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                Text = "Flavour"
            };

            stackLayout4_1.Children.Add(label3_1);

            for (int i = 0; i < order.Count; i++)
            {

                var orderItem = order[i];

                Label label4_1 = new Label()
                {
                    HeightRequest = 30,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Color.White
                };

                bool condition = true;

                if (orderItem.SubCategory != "Chicken" && orderItem.SubCategory != "Platter")
                    condition = false;

                string name = orderItem.Name.ToLower();

                if (orderItem.SubCategory == "Platter" && !name.Contains("chicken"))
                    condition = false;

                if (condition)
                {
                    string flavour = order[i].Flavour;

                    label4_1.Text = flavour;
                }
                else
                {
                    label4_1.Text = "None";
                }


                stackLayout4_1.Children.Add(label4_1);
            }
            #endregion

            #region MeatTemp
            StackLayout stackLayout4_2 = new StackLayout()
            {
                Margin = new Thickness(0, 0, 10, 0)
            };

            Label label3_2 = new Label()
            {
                TextColor = Color.White,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                Text = "Readiness"
            };

            stackLayout4_2.Children.Add(label3_2);

            for (int i = 0; i < order.Count; i++)
            {
                Label label4_2 = new Label()
                {
                    HeightRequest = 30,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Color.White
                };

                var orderItem = order[i];

                bool condition = true;

                if (orderItem.SubCategory != "Steak")
                    condition = false;

                if (condition)
                {
                    string meatTemperature = order[i].MeatTemperature;

                    label4_2.Text = meatTemperature;
                }
                else
                {
                    label4_2.Text = "-";
                }


                stackLayout4_2.Children.Add(label4_2);
            }
            #endregion

            #region Sauce
            StackLayout stackLayout4_3 = new StackLayout()
            {
                Margin = new Thickness(0, 0, 10, 0)
            };

            Label label3_3 = new Label()
            {
                TextColor = Color.White,
                FontSize = Device.GetNamedSize(NamedSize.Medium, typeof(Label)),
                FontAttributes = FontAttributes.Bold,
                HorizontalTextAlignment = TextAlignment.Center,
                Text = "Sauces"
            };

            stackLayout4_3.Children.Add(label3_3);

            for (int i = 0; i < order.Count; i++)
            {
                Label label4_3 = new Label()
                {
                    HeightRequest = 30,
                    VerticalTextAlignment = TextAlignment.Center,
                    HorizontalTextAlignment = TextAlignment.Center,
                    TextColor = Color.White
                };

                string sauces = "";
                if (order[i].Sauces != null)
                {
                    foreach (var item in order[i].Sauces)
                    {
                        if (order[i].Sauces.IndexOf(item) == 0)
                            sauces += item;

                        if (order[i].Sauces.IndexOf(item) != 0)
                            sauces += ", " + item;
                    }
                }

                var orderItem = order[i];

                bool condition = true;

                if (orderItem.Category != "Meat")
                    condition = false;

                if (condition)
                {
                    string sauce = sauces;

                    label4_3.Text = sauce;
                }
                else
                {
                    label4_3.Text = "-";
                }

                stackLayout4_3.Children.Add(label4_3);
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
            stackLayout2.Children.Add(stackLayout4_1);
            stackLayout2.Children.Add(stackLayout4_2);
            stackLayout2.Children.Add(stackLayout4_3);
            stackLayout2.Children.Add(stackLayout5);

            stackLayout.Children.Add(stackLayout1);
            stackLayout.Children.Add(stackLayout2);

            return frame;
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

        // @Yewo: Seriously, what does block do
        int block = 0;
        private async void Logout_Button_Clicked(object sender, EventArgs e)
        {
            if (block != 0)
                return;

            block = 1;

            //Next Page
            //Application.Current.MainPage = new Login();
            LoginPage();
        }
        private void Status_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            // This is the order position
            int Index = (int)((object[])checkBox.BindingContext)[0];
            // This is the order item position. Brought in from logic within GetFrame() under the status region
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

        #endregion

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
                if (!string.IsNullOrEmpty(phoneNumber))
                    await client.PostAsync("https://rodizioexpress.com/api/sms/send/complete/" + phoneNumber + "/" + orderNumber, null);
            }
            catch
            {
                return;
            }
        }
        public async void LoginPage()=> await Navigation.PushAsync(new Login());//Resets back to login page

        // @Yewo: What was this supposed to do?
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

    }
}