using FireSharp.Config;
using FireSharp.Interfaces;
using FireSharp.Response;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RdKitchenApp.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RdKitchenApp.Helpers
{
    public class FirebaseDataContext
    {
        IFirebaseConfig config = new FirebaseConfig
        {
            AuthSecret = "KIxlMLOIsiqVrQmM0V7pppI1Ao67UPZv5jOdU0QJ",
            BasePath = "https://rodizoapp-default-rtdb.firebaseio.com/"
        };

        IFirebaseClient client;

        int count = 0;

        public static FirebaseDataContext Instance { get; set; }

        public FirebaseDataContext()
        {
            Instance = this; 

            client = new FireSharp.FirebaseClient(config);

            string branchId = "rd29502";

            Device.StartTimer(TimeSpan.FromSeconds(1), () =>
            {
                Action();

                return true;
            });

            GetDataChanging("Order/" + branchId);            
        }
        float elapsedTime = 0;
        bool startCounting = false;
        void Action()
        {
            if (startCounting)
                elapsedTime++;

            if (elapsedTime > 5)
            {
                startCounting = false;
                elapsedTime = 0;

                if(count == 1)
                {
                    KitchenApp.Instance.DatabaseChangeListenerUpdate();
                }

                count = 1;
            }
        }
        public async void GetDataChanging(string fullPath)
        {
            EventStreamResponse response = await client.OnAsync(fullPath,
                (sender, args, context) =>
                {
                    DataReceived("add");
                },
                (sender, args, context) =>
                {
                    DataReceived("add");
                });
        }

        void DataReceived(string source)
        {
            startCounting = true;
            elapsedTime = 0;
        }
        public async Task<List<List<OrderItem>>> GetOrders()
        {
            string branchId = (new SerializedObjectManager().RetrieveData("BranchId")).ToString();

            var response = await client.GetAsync("Order/" + branchId);

            List<object> objects = new List<object>();

            var result = response.ResultAs<Dictionary<string, object>>();

            if (result != null)
            {
                foreach (var item in result)
                {
                    objects.Add(item.Value);
                }

                List<List<OrderItem>> temp = new List<List<OrderItem>>();

                foreach (var item in objects)
                {
                    List<OrderItem> data = JsonConvert.DeserializeObject<List<OrderItem>>(((JArray)item).ToString());

                    temp.Add(data);
                }

                return temp;
            }

            return new List<List<OrderItem>>();
        }
        public async Task Update(string fullPath, OrderItem order)
        {
            await client.UpdateAsync(fullPath, order);
        }
    }
}
