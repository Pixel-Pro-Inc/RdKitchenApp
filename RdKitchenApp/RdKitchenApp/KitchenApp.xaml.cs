using RdKitchenApp.Entities;
using RdKitchenApp.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
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

        public static KitchenApp Instance { get; set; }
        public KitchenApp()
        {
            InitializeComponent();
            Instance = this;

            UpdateOrderView(); //Display All Eligible Orders On Start
        }
        
        public async void UpdateOrderView()
        {
            orderViewer.Children.Clear();

            activityIndicator.IsVisible = true;

            message.IsVisible = false;

            List<List<OrderItem>> orders = await GetOrderItems();

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
        }

        public async void DatabaseChangeListenerUpdate()
        {
            List<string> orderNumbers = new List<string>();

            foreach (var item in _orders)
            {
                foreach (var itm in item)
                {
                    if (!orderNumbers.Contains(itm.OrderNumber))
                    {
                        orderNumbers.Add(itm.OrderNumber);
                    }
                }
            }
            
            List<List<OrderItem>> orders = await GetOrderItems();
            if (orders.Where(o => !orderNumbers.Contains(o[0].OrderNumber)).Count() > 0)//If no new items edit
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

                return;
            }

            UpdateOrderView();//Refresh whole view with edited order items
        }

        async Task<List<List<OrderItem>>> GetOrderItems()
        {
            List<List<OrderItem>> orders = new List<List<OrderItem>>();

            orders = await FirebaseDataContext.Instance.GetOrders();

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
                    TextColor = Color.White,
                    Text = order[i].Weight
                };

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

            await FirebaseDataContext.Instance.Update(fullPath, order);
        }

        private void Status_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;

            int Index = (int)((object[])checkBox.BindingContext)[0];
            int Index1 = (int)((object[])checkBox.BindingContext)[1];

            _orders[Index][Index1].Fufilled = checkBox.IsChecked;            

            Logic(Index, Index1);
        }

        async void Logic(int Index, int Index1)
        {
            for (int i = 0; i < _orders[Index].Count; i++)
            {
                await UpdateDatabase(_orders[Index][i]);
            }

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
                
            await client.PostAsync("https://rodizioexpress.azurewebsites.net/api/sms/send/" + phoneNumber + "/" + orderNumber, null);//Switch to final domain name            
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
    }
}