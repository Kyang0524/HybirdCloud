using HybirdCloud.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Diagnostics;

namespace HybirdCloud.Controllers
{
    public class UserController : Controller
    {
        // GET: User
        public ActionResult Info()
        {
            if (Session["Account"] == null)
            {
                TempData["msg"] = "Please Login first!";
                return RedirectToAction("Login", "Home");
            }
            var db = new HybridCloudEntities();
            UserInfo user= db.UserInfo.Find(Session["Account"]);
            ViewBag.user = user;

            Session["Account"] = user.Username;
            Session["Email"] = user.Email;
            Session["PhoneNumber"] = user.PhoneNumber;
            Session["AccountType"] = user.AccountType;
            Session["Gender"] = user.Gender;
            Session["Wallet"] = user.Wallet;

            Session["Image"] = Convert.ToBase64String(user.Image);
            return View();
        }
        public ActionResult EditInfo()
        {
            if (Session["Account"] == null)
            {
                TempData["msg"] = "Please Login first!";
                return RedirectToAction("Login","Home");
            }
            return View();
        }
        [HttpPost]
        public ActionResult EditInfo(EditUser userModel)
        {
            var db = new HybridCloudEntities();
            UserInfo user = db.UserInfo.Find(Session["Account"]);
            user.Email = userModel.Email;
            user.PhoneNumber = userModel.PhoneNumber;

            if (userModel.Image != null)
            {
                byte[] fileBytes;
                using (var stream = userModel.Image.InputStream)
                {
                    fileBytes = new byte[stream.Length];
                    stream.Read(fileBytes, 0, (int)stream.Length);
                }
                user.Image = fileBytes;
            }

            if (ModelState.IsValid)
            {
                db.Entry(user).State = EntityState.Modified;
                db.SaveChanges();
                TempData["msg"] = "Change Info Successful";
                UserInfo users = db.UserInfo.Find(Session["Account"]);
                Session["Account"] = users.Username;
                Session["Email"] = users.Email;
                Session["PhoneNumber"] = users.PhoneNumber;
                Session["AccountType"] = users.AccountType;
                Session["Gender"] = users.Gender;
                Session["Wallet"] = users.Wallet;
                Session["Image"] = Convert.ToBase64String(users.Image);
                return View("Info");
            }
            return View(userModel);
            
        }

        public ActionResult Cart()
        {
            if (Session["Account"] != null)
            {
                Session["TotalPrice"] = 0;
                var db = new HybridCloudEntities();
                string username = Session["Account"].ToString();
                List<Cart> carts = db.Cart.Where(x => x.Username.Equals(username)).ToList();
                List<ItemInfo> items = new List<ItemInfo>();
                foreach(Cart cart in carts)
                {
                    items.Add(db.ItemInfo.Find(cart.ItemID));  
                }
                foreach(ItemInfo item in items)
                {
                    Session["TotalPrice"] = Convert.ToInt64(Session["TotalPrice"]) + Convert.ToInt64(item.ItemPrice) ;
                }
                ViewBag.cart = items;
                return View();
            }
            TempData["msg"] = "please login!";
            return RedirectToAction("Login","Home");
        }
        [HttpPost]
        public ActionResult DeleteItem(Cart cart)
        {
            var db = new HybridCloudEntities();
            string user = Session["Account"].ToString();
            List<Cart> item = db.Cart.Where(x => x.ItemID.Equals(cart.ItemID) && x.Username.Equals(user)).ToList();
            foreach (Cart cartitem in item)
            {
                db.Cart.Remove(cartitem);
            }
            db.SaveChanges();
            return RedirectToAction("Cart","User");
        }
        [HttpPost]
        public ActionResult BuyOnCart()
        {
            if (Session["Account"] != null)
            {
                var db = new HybridCloudEntities();
                string username = Session["Account"].ToString();
                UserInfo userInfo = db.UserInfo.Find(username);

                List<Cart> carts = db.Cart.Where(x => x.Username.Equals(username)).ToList();
                List<ItemInfo> items = new List<ItemInfo>();
                if (Convert.ToInt64(userInfo.Wallet) < Convert.ToInt64(Session["TotalPrice"])) {
                    TempData["msg"] = "not enough money!";
                    return RedirectToAction("Index", "Home");
                }
                foreach (Cart cart in carts)
                {
                    items.Add(db.ItemInfo.Find(cart.ItemID));
                }


                foreach (ItemInfo item in items)
                {
                    string sellername = item.ItemUploader;
                    UserInfo sellerinfo = db.UserInfo.Find(sellername);
                    sellerinfo.Wallet = (Convert.ToInt64(sellerinfo.Wallet) + Convert.ToInt64(item.ItemPrice)).ToString();
                    db.Entry(sellerinfo).State = EntityState.Modified;
                    db.SaveChanges();
                    Shopping shopping = new Shopping();
                    Random generator = new Random();
                    string d1 = generator.Next(0, 1000000).ToString("D6");

                    shopping.ID = d1 + generator.Next(0, 1000000).ToString("D6");
                    shopping.Buyer = Session["Account"].ToString();
                    shopping.ItemID = item.ItemID;
                    db.Shopping.Add(shopping);

                    string user = Session["Account"].ToString();
                    List<Cart> item_ = db.Cart.Where(x => x.ItemID.Equals(item.ItemID) && x.Username.Equals(username)).ToList();
                    foreach (Cart cartitem in item_)
                    {
                        db.Cart.Remove(cartitem);
                    }

                }

                userInfo.Wallet = (Convert.ToInt64(userInfo.Wallet) - Convert.ToInt64(Session["TotalPrice"])).ToString();
                Session["TotalPrice"] = 0;
                db.Entry(userInfo).State = EntityState.Modified;
                
                db.SaveChanges();
                Session["Wallet"] = userInfo.Wallet;

                List<Cart> carts_ = db.Cart.Where(x => x.Username.Equals(username)).ToList();
                List<ItemInfo> items_ = new List<ItemInfo>();
                foreach (Cart cart in carts_)
                {
                    items.Add(db.ItemInfo.Find(cart.ItemID));
                }
                ViewBag.cart = items_;

                return View("Cart");
            }
            TempData["msg"] = "please login!";
            return RedirectToAction("Login", "Home");
            
        }


        public ActionResult Upload()
        {
            if (Session["Account"] == null)
            {
                TempData["msg"] = "Please Login first!";
                return RedirectToAction("Login", "Home");
            }
            if (Session["AccountType"].Equals("Buyer"))
            {
                TempData["msg"] = "You are not Seller account";
                return RedirectToAction("Index", "Home");
            }
            return View();
        }
        [HttpPost]
        public ActionResult Upload(ProductModel productModel)
        {
            var db = new HybridCloudEntities();
            var itemInfo = new ItemInfo();

            Random generator = new Random();

            itemInfo.ItemID = generator.Next(0, 1000000).ToString("D6");
            itemInfo.ItemName = productModel.ItemName;
            itemInfo.ItemPrice = productModel.ItemPrice;
            itemInfo.ItemUploader = Session["Account"].ToString();
            itemInfo.Description = productModel.Description;
            itemInfo.Categories = productModel.Categories;
            itemInfo.SalesMethod = productModel.SalesMethod;
            itemInfo.ItemUploadTime = DateTime.UtcNow;
            itemInfo.Expiredtime = productModel.Expiredtime.ToString();

            if (productModel.ItemImage != null) {
                byte[] fileBytes;

                using (var stream = productModel.ItemImage.InputStream)
                {
                    fileBytes = new byte[stream.Length];
                    stream.Read(fileBytes, 0, (int)stream.Length);
                }

                itemInfo.ItemImage = fileBytes;
            }

            db.ItemInfo.Add(itemInfo);
            db.SaveChanges();
            TempData["msg"] = "Upload product successful.";
            return View();
        }

        public ActionResult Myproduct()
        {
            if (Session["Account"] != null)
            {
                var db = new HybridCloudEntities();
                string username = Session["Account"].ToString();
                if (Session["AccountType"].Equals("Buyer"))
                {

                    List<Shopping> shops = db.Shopping.Where(S => S.Buyer.Equals(username)).ToList();
                    List<ItemInfo> items = new List<ItemInfo>();
                    foreach (Shopping shop in shops)
                    {
                        ItemInfo item = db.ItemInfo.Find(shop.ItemID);
                        items.Add(item);
                    }
                    ViewBag.Myproduct = items;
                    return View();
                }
                else
                {
                    List<ItemInfo> items = db.ItemInfo.Where(I => I.ItemUploader.Equals(username)).ToList();
                    ViewBag.Myproduct = items;
                    return View();
                }

            }

            TempData["msg"] = "Please Login first!";

            return RedirectToAction("Login", "Home");
        }
    }
}