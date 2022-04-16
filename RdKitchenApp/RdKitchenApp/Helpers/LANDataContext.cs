using RdKitchenApp.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using static RdKitchenApp.Entities.Enums;

namespace RdKitchenApp.Helpers
{
    public static class LANDataContext
    {
        public async static void StoreData(Directories path, object data)
        {
            await TCPClient.SendRequest(data, path.ToString(), RequestObject.requestMethod.Store);
        }
        public async static Task StoreDataOverwrite(Directories path, object data)
        {
            await TCPClient.SendRequest((OrderItem)data, path.ToString(), RequestObject.requestMethod.Store);
        }
        public async static Task<object> GetData(Directories path)
        {
            return await TCPClient.SendRequest(null, path.ToString(), RequestObject.requestMethod.Get);
        }

        public async static Task<object> GetUserData(Directories path)
        {
            return await TCPClient.SendRequest(path.ToString(), RequestObject.requestMethod.Get);
        }
        public async static void EditOrderData(Directories path, OrderItem data)
        {
            await TCPClient.SendRequest(data, path.ToString(), RequestObject.requestMethod.Update);
        }
    }
}