using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RdKitchenApp.Entities
{
    public class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Restuarant { get; set; }
        public string Price { get; set; }
        public string ImgUrl { get; set; }
        public string PublicId { get; set; }
        public string prepTime { get; set; }
        public string Category { get; set; }
        public float MinimumPrice { get; set; }
        public float Rate { get; set; }
        public bool Availability { get; set; }
    }
}