using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HybirdCloud.Models
{
    public class User
    {
        public string Username { get; set; }
        public string SessionValue { get; set; }
    }
    public class Auction
    { 
        public string Bidder { get; set; }
        public string FinalPrice { get; set; }
        public string SessionValue { get; set; }
        public string ItemID { get; set; }
    }
}