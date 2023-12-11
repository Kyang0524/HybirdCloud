using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using HybirdCloud.Models;

namespace HybirdCloud.Controllers
{
    public class HomeController : Controller
    {
        public FileContentResult ValidateCode()
        {
            string code = "";
            MemoryStream ms = new Captcha().Create(out code);
            Session["gif"] = code;//验证码存储在Session中，供验证。
            Response.ClearContent();//清空输出流
            return File(ms.ToArray(), @"image/png");
        }
        public ActionResult Index()
        {
            var db = new HybridCloudEntities();

            List<ItemInfo> itemInfos = db.ItemInfo.Where(I => I.SalesMethod.Equals("Normal")).ToList();
            ViewBag.AllItemList = itemInfos;
            return View();
        }
        [HttpGet]
        public ActionResult Product(ProductModel model)
        {
            var db = new HybridCloudEntities();
            List<ItemInfo> itemInfos = db.ItemInfo.Where(I => I.Categories.Equals(model.Categories) && I.SalesMethod.Equals("Normal")).ToList();
            ViewBag.Item = itemInfos;
            return View();
        }
        [HttpGet]
        public ActionResult ProductDetail(ProductModel model)
        {
            if (Session["Account"] != null)
            {
                var db = new HybridCloudEntities();
                List<ItemInfo> itemInfos = db.ItemInfo.Where(I => (I.ItemName.Equals(model.ItemName) && I.ItemUploader.Equals(model.ItemUploader))).ToList();
                if (itemInfos.Count == 0)
                {
                    return RedirectToAction("Index", "Home");
                }
                ViewBag.ItemDetail = itemInfos;
                return View();
            }
            TempData["msg"] = "Please Login first!";
            return RedirectToAction("Login","Home");
        }
        [HttpPost]
        public ActionResult Shop(Shop shop)
        {
            if (Session["Account"] != null)
            {
                if (shop.Ownership.Equals(Session["Account"].ToString())) {
                    TempData["msg"] = "Can not buy self item";
                    return RedirectToAction("Index","Home");
                }
                var db = new HybridCloudEntities();

                string userName = Session["Account"].ToString();
                UserInfo user = db.UserInfo.Find(userName);
                UserInfo Seller = db.UserInfo.Find(shop.Ownership);

                if (Convert.ToInt64(user.Wallet) < Convert.ToInt64(shop.ItemPrice))
                {
                    TempData["msg"] = "not enough money!";
                    return RedirectToAction("ProductDetail", "Home", new { ItemName = shop.ItemName, ItemUploader = shop.Ownership });
                }
                else
                {
                    Session["Wallet"] = user.Wallet = (Convert.ToInt64(user.Wallet) - Convert.ToInt64(shop.ItemPrice)).ToString();
                    Seller.Wallet = (Convert.ToInt64(Seller.Wallet)+ Convert.ToInt64(shop.ItemPrice)).ToString();
                    Shopping shopping = new Shopping();
                    Random generator = new Random();

                    shopping.ID = generator.Next(0, 1000000).ToString("D6");
                    shopping.Buyer = Session["Account"].ToString();
                    shopping.ItemID = shop.ItemID;

                    db.Entry(Seller).State = EntityState.Modified;
                    db.Entry(user).State = EntityState.Modified;
                    db.Shopping.Add(shopping);
                    db.SaveChanges();
                    TempData["msg"] = "Purchase done,check [my product].";
                    return RedirectToAction("Index", "Home");
                }
            }

            TempData["msg"] = "Please Login first!";

            return RedirectToAction("Login","Home");

        }
        public ActionResult AboutUs()
        {
            return View();
        }

        public ActionResult Login()
        {
            if (Session["Account"] != null)
            {
                return RedirectToAction("Index","Home");
            }
            return View();
        }
        [HttpPost]
        public ActionResult Login(LoginViewModel userViewModel)
        {

            var db = new HybridCloudEntities();
            UserInfo user = db.UserInfo.Find(userViewModel.Username);
            if (userViewModel.Password.Equals(user.Password) && userViewModel.Password != null && userViewModel.ValidateCode.Equals(Session["gif"].ToString()))
            {

                Session["Account"] = user.Username;
                Session["Email"] = user.Email;
                Session["PhoneNumber"] = user.PhoneNumber;
                Session["AccountType"] = user.AccountType;
                Session["Gender"] = user.Gender;
                Session["Wallet"] = user.Wallet;

                Session["Image"] = Convert.ToBase64String(user.Image);

                HttpContext httpContext = System.Web.HttpContext.Current;
                var userOnline = (Dictionary<string, User>)httpContext.Application["Online"];
                User userInfo = new User();
                userInfo.Username = user.Username;
                if (userOnline != null && userOnline.Count > 0)
                {
                    IDictionaryEnumerator enumerator = userOnline.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var enValue = userOnline[enumerator.Key.ToString()];
                        if (enValue != null && enValue.Username == user.Username)
                        {
                            userOnline[enumerator.Key.ToString()].SessionValue = "_offline_";
                            break;
                        }
                    }
                }
                else
                {
                    userOnline = new Dictionary<string, User>();
                }
                userOnline[Session.SessionID] = userInfo;//保存用戶信息在session
                httpContext.Application.Lock();
                httpContext.Application["Online"] = userOnline;
                httpContext.Application.UnLock();



                return RedirectToAction("Index","Home");
            }
            TempData["msg"] = "Login fail,pleses check validation code or password and username correct!";
            return View();
        }
        public ActionResult Logout()
        {
            Session.Clear();
            TempData["msg"] = "Successful logout";
            return View("Login");
        }

        public ActionResult Register()
        {

            return View();
        }
        [HttpPost]

        public ActionResult Register(RegisterViewModel registerViewModel)
        {
            var db = new HybridCloudEntities();
            UserInfo user_chekc =db.UserInfo.Find(registerViewModel.Username);
            if (user_chekc != null)
            {
                TempData["msg"] = "Username already exists.";
                return View("Register");
            }
            var users = new UserInfo();

            users.Username = registerViewModel.Username; 
            users.Email = registerViewModel.Email;
            users.Password = registerViewModel.Password;
            users.PhoneNumber = registerViewModel.PhoneNumber;
            users.AccountType = registerViewModel.AccountType;
            users.Gender = registerViewModel.Gender;
            users.Wallet = "99999";

            byte[] fileBytes;
            using (var stream = registerViewModel.Image.InputStream)
            {
                fileBytes = new byte[stream.Length];
                stream.Read(fileBytes, 0, (int)stream.Length);
            }
            users.Image = fileBytes;
            
            db.UserInfo.Add(users);
            db.SaveChanges();
            TempData["msg"] = "Register Successful";
            return View();
        }

        public ActionResult Auction()
        {
            if (Session["Account"] != null)
            {
                var db = new HybridCloudEntities();
                List<ItemInfo> itemInfosList = db.ItemInfo.Where(I => I.SalesMethod.Equals("Promotion")).OrderBy(x => x.ItemUploadTime).ToList();
                List<ItemInfo> items = new List<ItemInfo>();

                //&& I.ItemUploadTime.AddMinutes(5.0) <= DateTime.UtcNow
                foreach (ItemInfo item in itemInfosList) {
                    if (item.ItemUploadTime.AddMinutes(Double.Parse(item.Expiredtime)) > DateTime.UtcNow)
                    {
                        item.ItemUploadTime = item.ItemUploadTime.AddMinutes(Double.Parse(item.Expiredtime)).AddHours(8.0);
                        items.Add(item);
                    }
                }

                ViewBag.Auction = items;
                return View();
            }
            TempData["msg"] = "Please Login first!";
            return RedirectToAction("Login", "Home");
        }

        [HttpPost]
        public ActionResult AddToCart(CartModel cartModel)
        {
            
            if (Session["Account"] != null)
            {
                var db = new HybridCloudEntities();
                string user = Session["Account"].ToString();
                List<Cart> cart1 = db.Cart.Where(x => x.ItemID.Equals(cartModel.ItemID) && x.Username.Equals(user)).ToList();
                if (cart1.Count() == 0 )
                {
                    var cart = new Cart();
                    cart.Username = Session["Account"].ToString();
                    cart.ItemID = cartModel.ItemID;

                    Random generator = new Random();

                    string id1 = generator.Next(0, 1000000).ToString("D6");
                    string id2 = id1 + generator.Next(0, 1000000).ToString("D6");

                    cart.CartID = id2;
                    db.Cart.Add(cart);
                    db.SaveChanges();

                    TempData["msg"] = "Add to cart successful";

                    
                    return RedirectToAction("Cart", "User");
                }
                
                TempData["msg"] = "already add to cart";
                return RedirectToAction("Cart","User");
            }
            return View("Login");
        }


        [HttpPost]
        public JsonResult CheckIsForcedLogout()
        {
            try
            {
                HttpContext httpContext = System.Web.HttpContext.Current;
                var userOnline = (Dictionary<string, User>)httpContext.Application["Online"];
                if (userOnline != null)
                {
                    if (userOnline.ContainsKey(httpContext.Session.SessionID))
                    {
                        var AuctionInfo = userOnline[httpContext.Session.SessionID];
                        if (AuctionInfo.SessionValue != null && "_offline_".Equals(AuctionInfo.SessionValue))
                        {
                            userOnline.Remove(httpContext.Session.SessionID);
                            httpContext.Application.Lock();
                            httpContext.Application["online"] = userOnline;
                            httpContext.Application.UnLock();
                            string msg = "下線通知：當前賬號在另一地點登入，您被迫下線。若非本人操作，您的登入密碼很可能已經洩露，請及時改密。";
                            Session.Clear();
                            return Json(new { OperateResult = "Success", OperateData = msg }, JsonRequestBehavior.AllowGet);
                        }
                    }
                }
                return Json(new { OperateResult = "Failed" }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { OperateResult = "Failed" }, JsonRequestBehavior.AllowGet);
            }
        }

    }
}