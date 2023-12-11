using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HybirdCloud.Models
{
    public class ProductModel
    {
        public string ItemID { get; set; }
        public string ItemName {get;set;}
        public string ItemPrice { get; set; }
        public string ItemUploader { get; set; }
        public string Description { get; set; }
        public string Categories { get; set; }
        public string SalesMethod { get; set; }
        public double Expiredtime { get; set; }
        public HttpPostedFileBase ItemImage { get; set; }
        public DateTime ItemUploadTime { get; set; }
    }
    public class Shop
    {
        public string Buyer { get; set; }
        public string ItemID { get; set; }
        public string ShopID { get; set; }
        public string Ownership { get; set; }
        public string ItemPrice { get; set; }
        public string ItemName { get; set; }
    }
}