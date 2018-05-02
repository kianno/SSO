using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.Net.Http;
using System.Net.Http.Headers;
using Webapp1.Models;
using Newtonsoft.Json;


namespace Webapp1.Controllers
{
    
    public class HomeController : Controller
    {
        
        // GET: Default
        public ActionResult Index()
        {
            return View();
        }


        [HttpGet]
        public ActionResult Login(string returnURL)
        {
            var userinfo = new LoginVM();

            try
            {                
                EnsureLoggedOut();

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
        public async Task<ActionResult> Login(LoginVM models)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                var postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("Username", models.Username));
                postData.Add(new KeyValuePair<string, string>("Password", models.Password));
                postData.Add(new KeyValuePair<string, string>("isRemember", models.isRemember.ToString()));
                postData.Add(new KeyValuePair<string, string>("ReturnURL", models.ReturnURL));

                HttpContent content = new FormUrlEncodedContent(postData);

                HttpResponseMessage msg = await DoApiPost("http://localhost/webservices/api/User/login", content);


                if (msg.IsSuccessStatusCode)
                {
                    var resonseContent = await msg.Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(resonseContent))
                    {
                        SignInRemember(models.Username, models.isRemember);                        
                        SetCookie(JsonConvert.DeserializeObject<string>(resonseContent));
                    }
                }
                else {
                    TempData["ErrorMSG"] = "Access Denied! Wrong Credential";
                    return View(models);
            
                }
            }
            catch (Exception)
            {
                
                throw;
            }
                       

            return RedirectToAction("Index", "Default");
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

        private async Task<HttpResponseMessage> DoApiPost(string path, HttpContent content)
        {
            var client = new HttpClient();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("EN"));
            var url = new Uri (path).ToString();
            return await client.PostAsync(url, content);
        }

        private void SignInRemember(string userName, bool isPersistent = false)
        {
            // Clear any lingering authencation data
            FormsAuthentication.SignOut();

            // Write the authentication cookie
            FormsAuthentication.SetAuthCookie(userName, isPersistent);
        }

        private void EnsureLoggedOut()
        {
            // If the request is (still) marked as authenticated we send the user to the logout action
            if (Request.IsAuthenticated)
                Logout();
        }

        [HttpGet]     
        public ActionResult Logout()
        {
            try
            {
                // First we clean the authentication ticket like always
                //required NameSpace: using System.Web.Security;
                FormsAuthentication.SignOut();

                // Second we clear the principal to ensure the user does not retain any authentication
                //required NameSpace: using System.Security.Principal;
                //HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);

                //Session.Clear();         

                if (System.Web.HttpContext.Current.Request.Cookies["SSOUserID"] != null)
                {
                    Boolean userLogoutStatus = false;
                    string userID = System.Web.HttpContext.Current.Request.Cookies["SSOUserID"].Value;

                    if (!string.IsNullOrEmpty(userID))
                    {
                        int value;
                        if (int.TryParse(userID, out value))
                        {
                            using (var client = new HttpClient())
                            {
                                var getResponse = client.GetAsync(string.Format("http://localhost/webservices/api/User/logout?id={0}", value)).Result;

                                if (getResponse.IsSuccessStatusCode)
                                    userLogoutStatus = true;
                            }
                        }

                        if (!userLogoutStatus)
                        {
                            TempData["ErrorMSG"] = "User fail to logout";
                            return RedirectToAction("Index", "Default");
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

        private ActionResult RedirectToLocal(string returnURL = "")
        {
            try
            {
                // If the return url starts with a slash "/" we assume it belongs to our site
                // so we will redirect to this "action"
                if (!string.IsNullOrWhiteSpace(returnURL) && Url.IsLocalUrl(returnURL))
                    return Redirect(returnURL);

                // If we cannot verify if the url is local to our host we redirect to a default location
                return RedirectToAction("Index", "Default");
            }
            catch
            {
                throw;
            }
        }

    }



}
