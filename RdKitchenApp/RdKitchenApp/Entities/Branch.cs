using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RdKitchenApp.Entities
{
    public class Branch
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string ImgUrl { get; set; }
        public string PublicId { get; set; }
        public DateTime LastActive { get; set; }
        public int PhoneNumber { get; set; }
    }
}
