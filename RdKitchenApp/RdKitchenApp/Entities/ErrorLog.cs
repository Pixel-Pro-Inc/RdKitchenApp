using System;
using System.Collections.Generic;
using System.Text;

namespace RdKitchenApp.Entities
{
    public class ErrorLog
    {
        /// <summary>
        /// Unique Identifier for each order in DB.
        /// </summary>
        public string ID { get; set; }
        public string Exception { get; set; }
        public string OriginBranchId { get; set; }
        public string OriginDevice { get; set; }
        public DateTime TimeOfException { get; set; }

        //Subsequent Props Should be marked nullable
        public string AssignedTo { get; set; }
        public bool Completed { get; set; } = false;
    }
}
