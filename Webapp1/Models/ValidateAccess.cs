using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;


namespace Webapp1.Models
{
    public class ValidateAccess : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (HttpContext.Current.Request.Cookies["SSOUserID"] == null || !HttpContext.Current.Request.IsAuthenticated)
            {
                if (filterContext.HttpContext.Request.IsAjaxRequest())
                {
                    filterContext.HttpContext.Response.StatusCode = 302;
                    filterContext.HttpContext.Response.End();
                }
                else
                {
                    Boolean userStatus = false;
                    if (HttpContext.Current.Request.Cookies["SSOUserID"] != null)
                    {
                        string userID = HttpContext.Current.Request.Cookies["SSOUserID"].Value.ToString();
                        int value;
                        if (int.TryParse(userID, out value))
                        {
                            using (var client = new HttpClient())
                            {
                                var getResponse = client.GetAsync(string.Format("http://localhost/webservices/api/User/verifyuser?id={0}", value)).Result;

                                if (getResponse.IsSuccessStatusCode)
                                    userStatus = true;
                            }
                        }
                    }

                    if (!userStatus)
                    {
                        filterContext.Result = new RedirectResult(System.Web.Security.FormsAuthentication.LoginUrl + "?ReturnUrl=" +
                         filterContext.HttpContext.Server.UrlEncode(filterContext.HttpContext.Request.RawUrl));
                    }
                   
                    
                }
            }
            else
            {

                //todo

            }            
        }
        
    }
}