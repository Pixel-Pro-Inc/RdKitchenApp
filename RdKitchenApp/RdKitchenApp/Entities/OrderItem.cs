using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RdKitchenApp.Entities
{
    public class OrderItem
    {
        #region References
        /// <summary>
        /// Unique Identifier for each order in DB.
        /// </summary>
        public string ID { get; set; }
        /// <summary>
        /// Index of order item in order list
        /// </summary>
        public int Index { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        /// <summary>
        /// This property stores info on the channel the order was placed through
        /// </summary>
        public string Reference { get; set; }
        public string PhoneNumber { get; set; }
        //A string in the form of Date_4 digit number e.g 08-11-2021_4927
        public string OrderNumber { get; set; }
        public string User { get; set; }
        public List<string> Chefs { get; set; }

        /// <summary>
        /// This is the index of the location under the customers locations. So if you want to know the location of the delivery you are making, you will use the 
        /// phone number to access the customer and then using this index you can get the specific location the customer wants to be delivered to.
        /// </summary>
        public int LocationIndex { get; set; }
        /// <summary>
        /// This property shows whether an order was placed for delivery or not
        /// </summary>
        public bool DeliveryOrder { get; set; }
        #endregion

        #region Payments
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
        public bool WaitingForPayment { get; set; }
        #endregion

        #region Timings
        /// <summary>
        /// This is when the order is made, as in a customer makes the order, from what ever source it may be. It is set as readonly because it already defaults
        /// to the point when the orderitem was made, which coincides with when the customer makes the order.
        /// </summary>
        public DateTime OrderDateTime { get; } = DateTime.Now;
        /// <summary>
        /// This is the time the Order was complete. So we can find out how long orders take to be prepared
        /// </summary>
        public DateTime OrderCompletionTime { get; set; }
        /// <summary>
        /// The prepTime is now the difference between when the order was made and when it was marked as complete. When it is marked as complete is up to the POS
        /// </summary>
        public int? PrepTime
        {
            get
            {
                return Math.Abs((OrderCompletionTime - OrderDateTime).Minutes);
            }
        }

        /// <summary>
        /// This is the point when the order has been marked by the delivery man that it has been delivered. We expected them to make the tick before they move
        /// on to the next order. Cause the longer the delivery time you have the worse you review will be.
        /// </summary>
        public DateTime OrderDeliveryTime { get; set; }
        /// <summary>
        /// The DeliveryJourneyTime is now the difference between when the order was made (<see cref="OrderCompletionTime"/>) and when it was given to the customer
        /// (<see cref="OrderDeliveryTime"/>).
        /// When it is marked as as delivered is up to the delivery man
        /// </summary>
        public int? DeliveryJourneyTime
        {
            get
            {
                return (OrderCompletionTime - OrderDeliveryTime).Minutes;
            }
        }

        #endregion

        #region Qualities and conditions

        public string Weight { get; set; } = "0";
        public int Quantity { get; set; }
        public bool Fufilled { get; set; }
        public bool Purchased { get; set; } = true;//Defaulted to true because offline orders have to be paid for before they are made
        public bool Preparable { get; set; }
        public bool Collected { get; set; } = false;
        public bool MarkedForDeletion { get; set; } = false;
        public string Flavour { get; set; }
        public string MeatTemperature { get; set; }
        public List<string> Sauces { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; } = "";

        #endregion
    }
}