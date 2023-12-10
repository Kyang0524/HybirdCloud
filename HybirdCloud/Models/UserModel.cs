using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace HybirdCloud.Models
{
    public class EditUser
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public HttpPostedFileBase Image { get; set; }

    }
}