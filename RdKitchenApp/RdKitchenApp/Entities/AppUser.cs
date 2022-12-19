using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RdKitchenApp.Entities
{
    public class AppUser
    {
        /// <summary>
        /// Unique Identifier for each order in DB.
        /// </summary>
        public string ID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string PhoneNumber { get; set; }
        public string UserName { get; set; }
        public bool Developer { get; set; }
        public string Email { get; set; }
        public string NationalIdentityNumber { get; set; }
        public bool Admin { get; set; }
        public bool SuperUser { get; set; }
        public string ResetToken { get; set; }
        public List<string> branchId { get; set; }
        public byte[] PasswordHash { get; set; }
        public byte[] PasswordSalt { get; set; }
        public string FullName()
        {
            return FirstName + " " + LastName;
        }
    }
}