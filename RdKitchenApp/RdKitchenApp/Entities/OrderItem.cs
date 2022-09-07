using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RdKitchenApp.Entities
{
    public class OrderItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        public string Reference { get; set; }
        public string Price { get; set; }
        public string Weight { get; set; }
        public bool Fufilled { get; set; }
        public bool Purchased { get; set; }
        public string PaymentMethod { get; set; }
        public bool Preparable { get; set; }
        public bool WaitingForPayment { get; set; }
        public int Quantity { get; set; }
        public string OrderNumber { get; set; }
        public bool Collected { get; set; }
        public bool MarkedForDeletion { get; set; } = false;
        public string PhoneNumber { get; set; }
        public DateTime OrderDateTime { get; set; }
        public string User { get; set; }
        public List<string> Chefs { get; set; }
        public int PrepTime { get; set; }
        //New Additions
        public string Flavour { get; set; }
        public string MeatTemperature { get; set; }
        public List<string> Sauces { get; set; } = new List<string>();
        public string SubCategory { get; set; }
        //To allow customers to use multiple payment methods 
        public bool SplitPayment { get; set; } = false;
        public List<string> paymentMethods { get; set; } = new List<string>();
        public List<string> payments { get; set; } = new List<string>();
    }
}