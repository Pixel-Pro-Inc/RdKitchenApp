using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RdKitchenApp.Entities
{
    public class OrderItem
    {
        /// <summary>
        /// Unique Identifier for each order in DB.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// Index of order item in order list
        /// </summary>
        public int Index { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; } = "";
        public string Description { get; set; }
        public string Reference { get; set; }
        public string Price { get; set; } = "0";
        //To allow customers to use multiple payment methods 
        public bool SplitPayment { get; set; } = false;

        //NOTE: These are here because firebase can't store the properties the way we want them to. All it does is store a list of orderitems. All the other properties
        // that come along with disappear. So if there is something you want to store permanently that can't be calculated, you have to put it here.
        /// <summary>
        /// These are the paymentMethods of the Order. Please note that you have to replicate the same information in ALL of the orderitems for this to work.
        /// <para> Once again this is cause firebase can't just store extrenous information of a class that inherits from list.  So all the fundumental
        /// information has to be stored in the constituents of the aggregate</para>
        /// </summary>
        public List<string> paymentMethods { get; set; } = new List<string>();
        /// <summary>
        /// These are the payments of the Order in the correct arrangement. Please note that you have to replicate the same information in ALL of the orderitems for this to work.
        /// <para> Once again this is cause firebase can't just store extrenous information of a class that inherits from list. So all the fundumental
        /// information has to be stored in the constituents of the aggregate</para>
        /// </summary>
        public List<string> payments { get; set; }
        //This is here cause of legacy data in the database
        public string paymentMethod { get; set; }
        //End
        public string Weight { get; set; } = "0";
        public bool Fufilled { get; set; }
        public bool Purchased { get; set; } = true;//Defaulted to true because offline orders have to be paid for before they are made
        public bool Preparable { get; set; }
        public bool WaitingForPayment { get; set; }
        public int Quantity { get; set; }
        public string PhoneNumber { get; set; }
        //A string in the form of Date_4 digit number e.g 08-11-2021_4927
        public string OrderNumber { get; set; }
        public bool Collected { get; set; } = false;
        public bool MarkedForDeletion { get; set; } = false;
        public DateTime OrderDateTime { get; set; } = DateTime.Now;
        /// <summary>
        /// This is the time the Order was complete. So we can find out how long orders take to be prepared
        /// </summary>
        public DateTime OrderCompletionTime { get; set; }
        public string User { get; set; }
        /// <summary>
        /// The prepTime is now the difference between when the order was made and when it was marked as complete. When it is marked as complete is up to the POS
        /// </summary>
        public int PrepTime
        {
            get
            {
                return (OrderCompletionTime - OrderDateTime).Minutes;
            }
        }
        public List<string> Chefs { get; set; }
        //New Additions
        public string Flavour { get; set; }
        public string MeatTemperature { get; set; }
        public List<string> Sauces { get; set; }
    }
}