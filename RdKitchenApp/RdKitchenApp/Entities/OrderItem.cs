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
        /// <summary>
        /// Name of the item that was ordered
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The description of the item that was ordered
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// This property stores info on the channel the order was placed through.
        /// For example if an ordered was placed online, called in or placed in person.
        /// </summary>
        public string Reference { get; set; }
        /// <summary>
        /// An 8 digit number stored as a string
        /// </summary>
        public string PhoneNumber { get; set; }
        /// <summary>
        /// A string in the form of Date_4 digit number e.g 08-11-2021_4927.
        /// Offline orders are prefixed with a zero e.g 08-11-2021_0927.
        /// </summary>
        public string OrderNumber { get; set; }
        /// <summary>
        /// The name of the cashier who manipulated the order
        /// </summary>
        public string User { get; set; }
        /// <summary>
        /// A list of chefs who worked on the order
        /// </summary>
        public List<string> Chefs { get; set; }

        /// <summary>
        /// This is the index of the location under the customers locations. So if you want to know the location of the delivery you are making, you will use the 
        /// phone number to access the customer and then using this index you can get the specific location the customer wants to be delivered to.
        /// </summary>
        public int LocationIndex { get; set; }
        /// <summary>
        /// This property shows whether an order was placed for delivery or not
        /// </summary>
        public bool DeliveryOrder { get; set; } = false;
        #endregion

        #region Monetary
        /// <summary>
        /// The unit price multiplied by the quantity of the orderItem
        /// </summary>
        public string Price { get; set; } = "0";
        /// <summary>
        /// Stores information on how an order was paid
        /// </summary>
        public Payments OrderPayments { get; set; }
        #endregion

        #region Timings
        /// <summary>
        /// This is when the order is made, as in a customer makes the order, from what ever source it may be.
        /// Opted to not make it readonly because OrderItems need to have the exact same time and each one is created at
        /// Slightly different times.
        /// </summary>
        public DateTime OrderDateTime { get; set; }
        /// <summary>
        /// This is the time the Order was complete. So we can find out how long orders take to be prepared
        /// </summary>
        public DateTime OrderCompletionTime { get; set; }
        /// <summary>
        /// The prepTime is an estimate in how long an order will take based on how long it takes to prepare the menu items.
        /// </summary>
        public int PrepTime { get; set; }
        /// <summary>
        /// This time is marked when the delivery man ticks start on any particular order route.
        /// </summary>
        public DateTime DeliveryJourneyStartTime { get; set; }
        /// <summary>
        /// This is the point when the order has been marked by the delivery man that it has been delivered. We expected them to make the tick before they move
        /// on to the next order. Cause the longer the delivery time you have the worse your review will be.
        /// </summary>
        public DateTime DeliveryJourneyCompletionTime { get; set; }
        #endregion

        #region Qualities and conditions
        /// <summary>
        /// The weight of the order item.
        /// </summary>
        public string Weight { get; set; } = "0";
        /// <summary>
        /// The number of order items which are ordered.
        /// </summary>
        public int Quantity { get; set; }
        /// <summary>
        /// This is marked true when an orderItem is prepared in the kitchen.
        /// </summary>
        public bool Fufilled { get; set; }
        /// <summary>
        /// This is marked true when an order has been paid for
        /// </summary>
        public bool Purchased { get; set; }
        /// <summary>
        /// Lets the kitchen know whether to prepare the food or not.
        /// </summary>
        public bool Preparable { get; set; }
        /// <summary>
        /// Marked true when either the customer has collected the food or when the
        /// Customer receives the food from the delivery man.
        /// </summary>
        public bool Collected { get; set; } = false;
        /// <summary>
        /// This sets an order to cancelled
        /// </summary>
        public bool MarkedForDeletion { get; set; } = false;
        ///This could be achieved better by having sub orderitems 
        ///With have a price of zero and are used as different options
        public string Flavour { get; set; }
        public string MeatTemperature { get; set; }
        public List<string> Sauces { get; set; }
        /// <summary>
        /// Category of the order Item
        /// </summary>
        public string Category { get; set; }
        /// <summary>
        /// Subcategory of the orderItem
        /// </summary>
        public string SubCategory { get; set; } = "";
        #endregion
    }
}