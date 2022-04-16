using RdKitchenApp.Entities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace RdKitchenApp.Helpers
{
    public static class Formatting
    {
        public static string FormatAmountString(float amount) // format 1,000,000.00
        {
            string result = String.Format(/*new CultureInfo("en-US"),*/ "{0:n}", amount);

            return result;
        }
        public static List<List<OrderItem>> ChronologicalOrderList(List<List<OrderItem>> orderItems)
        {
            List<List<OrderItem>> temp = new List<List<OrderItem>>();
            List<DateTime> usedTimes = new List<DateTime>();
            DateTime lastTime = new DateTime();

            for (int i = 0; i < orderItems.Count; i++)
            {
                lastTime = new DateTime();
                foreach (var item in orderItems)
                {
                    if (item[0].OrderDateTime > lastTime && !usedTimes.Contains(item[0].OrderDateTime))
                    {
                        lastTime = item[0].OrderDateTime;
                    }
                }

                usedTimes.Add(lastTime);
                temp.Add(orderItems.Where(o => o[0].OrderDateTime == lastTime).ToArray()[0]);
            }

            return temp.ToArray().Reverse().ToList();
        }
    }
}
