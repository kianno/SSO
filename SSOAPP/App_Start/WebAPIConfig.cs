using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace SSOAPP
{
    public class WebAPIConfig
    {
        public static string WebApiUrl = System.Configuration.ConfigurationManager.AppSettings["webAPIUrl"];
        

        public static void Register(HttpConfiguration config)
        {        

            // Web API routes           
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{action}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

        }

    }
}