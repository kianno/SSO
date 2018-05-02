using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using System.Text;
using System.Threading.Tasks;
using SSOAPP.Models;

namespace SSOAPP.Areas.WebAPI.Controllers
{
    public class UserController : ApiController
    {
        private SSODBEntities db = new SSODBEntities();
        private static int saltLengthLimit = 32;

        
        [HttpGet]
        public IHttpActionResult VerifyUser(long ID)
        {
            if (ID > 0)
            {
                var userInfo = db.Users.Where(s => s.UserID == ID && s.SessionTimeout >= DateTime.Now).FirstOrDefault();

                if (userInfo != null)
                return Ok();
            }

            return NotFound();
        }

        [HttpGet]
        public IHttpActionResult Logout(long ID)
        {
            try
            {
                if (ID > 0)
                {
                    var userInfo = db.Users.Where(s => s.UserID == ID).FirstOrDefault();

                    if (userInfo != null)
                    {
                        userInfo.SessionTimeout = DateTime.Now.AddSeconds(-1);

                        db.SaveChanges();

                        return Ok();
                    }

                    return NotFound();
                }
                throw new Exception ("Invalid user ID");
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
            
        }

        [HttpPost]
        public IHttpActionResult Create(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {

                var userInfo = db.Users.Where(s => s.Username == model.Username.Trim()).FirstOrDefault();

                if (userInfo == null)
                {
                    using (db = new SSODBEntities())
                    {
                        byte[] usrSalt = Get_SALT();

                        User usr = new User();
                        usr.Username = model.Username;
                        usr.SALT = usrSalt;
                        usr.HASH = Get_HASH_SHA512(model.Password, model.Username, usrSalt);
                        usr.SessionTimeout = DateTime.Now;

                        db.Users.Add(usr);

                        db.SaveChanges();
                    }
                }
                else
                {
                    ModelState.AddModelError("ErrorMessage", "User already exists.");
                    return BadRequest(ModelState);

                }

                return Ok(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ErrorMessage", ex.Message);
                return BadRequest(ModelState);
            }
        }

        [HttpPost]        
        public IHttpActionResult Login(LoginVM model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            string OldHASHValue = string.Empty;
            byte[] SALT = new byte[saltLengthLimit];
            string userID = string.Empty;

            try
            {
                using (db = new SSODBEntities())
                {
                    
                    //Retrive Stored HASH Value From Database According To Username (one unique field)
                    var userInfo = db.Users.Where(s => s.Username == model.Username.Trim()).FirstOrDefault();

                    //Assign HASH Value
                    if (userInfo != null)
                    {
                        OldHASHValue = userInfo.HASH;
                        SALT = userInfo.SALT;
                    }

                    bool isLogin = CompareHashValue(model.Password, model.Username, OldHASHValue, SALT);

                    if (isLogin)
                    {                        
                        SetCookie(userInfo.UserID.ToString());

                        userInfo.SessionTimeout = DateTime.Now.AddDays(1);

                        db.SaveChanges();
                    }
                    else
                    {
                        //Login Fail
                        ModelState.AddModelError("ErrorMessage", "Access Denied! Wrong Credential");
                        return BadRequest(ModelState);
                        
                    }
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("ErrorMessage", ex.Message);
                return BadRequest(ModelState);
            }


            return Ok(userID);
        }

        private void SetCookie(string uID)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies["SSOUserID"];

            if (cookie == null)
            {
                cookie = new HttpCookie("SSOUserID");
                cookie.Value = uID.ToString();
                cookie.Expires = DateTime.Now.AddDays(1);
                HttpContext.Current.Response.Cookies.Add(cookie);
            }
            else
            {
                cookie.Value = uID.ToString();
                cookie.Expires = DateTime.Now.AddDays(1);
            }

        }

        private static string Get_HASH_SHA512(string password, string username, byte[] salt)
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

        private static bool CompareHashValue(string password, string username, string OldHASHValue, byte[] SALT)
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
    }
}
