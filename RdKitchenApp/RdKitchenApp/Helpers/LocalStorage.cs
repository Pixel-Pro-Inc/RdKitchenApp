using RdKitchenApp.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Helpers
{
    public static class LocalStorage
    {
        public static AppUser Chef { get; private set; }

        public static void SetChef(AppUser user)
        {
            Chef = user;
        } 
    }
}
