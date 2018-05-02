using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using SSOAPP.Models;

namespace SSOAPP.Controllers
{
    public class HomeController : Controller
    {

        private SSODBEntities db = new SSODBEntities();
        private static int saltLengthLimit = 32;

        [HttpGet]
        public ActionResult Login(string returnURL)
        {
            var userinfo = new LoginVM();

            try
            {
               
                // We do not want to use any existing identity information
                EnsureLoggedOut();

                // Store the originating URL so we can attach it to a form field
                userinfo.ReturnURL = returnURL;

                return View(userinfo);
            }
            catch
            {
                throw;
            }
        }        
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            try
            {
                // First we clean the authentication ticket like always
                //required NameSpace: using System.Web.Security;
                FormsAuthentication.SignOut();

                // Second we clear the principal to ensure the user does not retain any authentication
                //required NameSpace: using System.Security.Principal;
                HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);

                //Session.Clear();
                if (System.Web.HttpContext.Current.Request.Cookies["SSOUserID"] != null)
                {
                    string userID = System.Web.HttpContext.Current.Request.Cookies["SSOUserID"].Value;

                    int value;
                    if (int.TryParse(userID, out value))
                    {
                        var userInfo = db.Users.Where(s => s.UserID == value).FirstOrDefault();

                        if (userInfo != null)
                        {
                            userInfo.SessionTimeout = DateTime.Now.AddSeconds(-1);
                        }                    
                    }   

                    HttpCookie currentUserCookie = System.Web.HttpContext.Current.Request.Cookies["SSOUserID"];
                    System.Web.HttpContext.Current.Response.Cookies.Remove("SSOUserID");
                    currentUserCookie.Expires = DateTime.Now.AddDays(-10);
                    currentUserCookie.Value = null;
                    System.Web.HttpContext.Current.Response.SetCookie(currentUserCookie);
                }
                
                

                // Last we redirect to a controller/action that requires authentication to ensure a redirect takes place
                // this clears the Request.IsAuthenticated flag since this triggers a new request
                return RedirectToLocal();
            }
            catch
            {
                throw;
            }
        }

        private void EnsureLoggedOut()
        {
            // If the request is (still) marked as authenticated we send the user to the logout action
            if (Request.IsAuthenticated)
                Logout();
        }

        private ActionResult RedirectToLocal(string returnURL = "")
        {
            try
            {
                // If the return url starts with a slash "/" we assume it belongs to our site
                // so we will redirect to this "action"
                if (!string.IsNullOrWhiteSpace(returnURL) && Url.IsLocalUrl(returnURL))
                    return Redirect(returnURL);

                // If we cannot verify if the url is local to our host we redirect to a default location
                return RedirectToAction("Index", "Dashboard");
            }
            catch
            {
                throw;
            }
        }

        private void SignInRemember(string userName, bool isPersistent = false)
        {
            // Clear any lingering authencation data
            FormsAuthentication.SignOut();

            // Write the authentication cookie
            FormsAuthentication.SetAuthCookie(userName, isPersistent);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginVM entity)
        {
            string OldHASHValue = string.Empty;
            byte[] SALT = new byte[saltLengthLimit];

            try
            {
                using (db = new SSODBEntities())
                {
                    // Ensure we have a valid viewModel to work with
                    if (!ModelState.IsValid)
                        return View(entity);

                    //Retrive Stored HASH Value From Database According To Username (one unique field)
                    var userInfo = db.Users.Where(s => s.Username == entity.Username.Trim()).FirstOrDefault();

                    //Assign HASH Value
                    if (userInfo != null)
                    {
                        OldHASHValue = userInfo.HASH;
                        SALT = userInfo.SALT;
                    }

                    bool isLogin = CompareHashValue(entity.Password, entity.Username, OldHASHValue, SALT);

                    if (isLogin)
                    {
                        //Login Success
                        //For Set Authentication in Cookie (Remeber ME Option)
                        SignInRemember(entity.Username, entity.isRemember);

                        //Set A Unique ID in session
                        //Session["SSOUserID"] = userInfo.UserID;


                        SetCookie(userInfo.UserID.ToString());


                        userInfo.SessionTimeout = DateTime.Now.AddDays(1);

                        db.SaveChanges();

                        // If we got this far, something failed, redisplay form
                        // return RedirectToAction("Index", "Dashboard");
                        return RedirectToLocal(entity.ReturnURL);
                    }
                    else
                    {
                        //Login Fail
                        TempData["ErrorMSG"] = "Access Denied! Wrong Credential";
                        return View(entity);
                    }
                }
            }
            catch
            {
                throw;
            }

        }

        private void SetCookie(string uID)
        {
            HttpCookie cookie = Request.Cookies["SSOUserID"];

            if (cookie == null)
            {
                cookie = new HttpCookie("SSOUserID");
                cookie.Value = uID.ToString();
                cookie.Expires = DateTime.Now.AddDays(1);
                Response.Cookies.Add(cookie);
            }
            else
            {
                cookie.Value = uID.ToString();
                cookie.Expires = DateTime.Now.AddDays(1);
            }

        }


        #region --> Generate SALT Key

        private static byte[] Get_SALT()
        {
            return Get_SALT(saltLengthLimit);
        }

        private static byte[] Get_SALT(int maximumSaltLength)
        {
            var salt = new byte[maximumSaltLength];

            //Require NameSpace: using System.Security.Cryptography;
            using (var random = new RNGCryptoServiceProvider())
            {
                random.GetNonZeroBytes(salt);
            }

            return salt;
        }

        #endregion

        #region --> Generate HASH Using SHA512
        public static string Get_HASH_SHA512(string password, string username, byte[] salt)
        {
            try
            {
                //required NameSpace: using System.Text;
                //Plain Text in Byte
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(password + username);

                //Plain Text + SALT Key in Byte
                byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + salt.Length];

                for (int i = 0; i < plainTextBytes.Length; i++)
                {
                    plainTextWithSaltBytes[i] = plainTextBytes[i];
                }

                for (int i = 0; i < salt.Length; i++)
                {
                    plainTextWithSaltBytes[plainTextBytes.Length + i] = salt[i];
                }

                HashAlgorithm hash = new SHA512Managed();
                byte[] hashBytes = hash.ComputeHash(plainTextWithSaltBytes);
                byte[] hashWithSaltBytes = new byte[hashBytes.Length + salt.Length];

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    hashWithSaltBytes[i] = hashBytes[i];
                }

                for (int i = 0; i < salt.Length; i++)
                {
                    hashWithSaltBytes[hashBytes.Length + i] = salt[i];
                }

                return Convert.ToBase64String(hashWithSaltBytes);
            }
            catch
            {
                return string.Empty;
            }
        }
        #endregion

        #region --> Comapare HASH Value
        public static bool CompareHashValue(string password, string username, string OldHASHValue, byte[] SALT)
        {
            try
            {
                string expectedHashString = Get_HASH_SHA512(password, username, SALT);

                return (OldHASHValue == expectedHashString);
            }
            catch
            {
                return false;
            }
        }
        #endregion
    }
}