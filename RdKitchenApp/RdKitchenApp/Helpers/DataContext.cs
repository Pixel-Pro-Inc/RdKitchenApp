using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RdKitchenApp.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace RdKitchenApp.Helpers
{
    public class DataContext
    {
        int count = 0;

        public static DataContext Instance { get; set; }

        public DataContext()
        {
            Instance = this;

            StartFunction();
        }
        public void StartFunction()
        {
            string branchId = (new SerializedObjectManager().RetrieveData("BranchId")).ToString();
        }
        public async Task<List<AppUser>> GetUsers()
        {
            var resultData = await LANDataContext.GetUserData(Enums.Directories.Account);

            return (List<AppUser>)resultData;
        }
        public async Task<List<List<OrderItem>>> GetOrders()
        {
            var resultData = await LANDataContext.GetData(Enums.Directories.Order);

            return (List<List<OrderItem>>)resultData;
        }
        public async Task Update(string fullPath, OrderItem order)
        {
            await LANDataContext.StoreDataOverwrite(Enums.Directories.Order, order);
        }
    }
}
