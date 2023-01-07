using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Entities
{
    public class Payments
    {
        public float Cash { get; set; }
        public float Card { get; set; }
        public float Online { get; set; }
        public float EFT { get; set; }
        public float Cheque { get; set; }
        public float MobileMoneyWallet { get; set; }
        public float EWallet { get; set; }
    }
}
