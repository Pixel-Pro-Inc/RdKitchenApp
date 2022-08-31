using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Entities
{
    public class ErrorLog
    {
        public int? Id { get; set; }
        public string Exception { get; set; }
        public string OriginBranchId { get; set; }
        public string OriginDevice { get; set; }
        public DateTime TimeOfException { get; set; }
    }
}
