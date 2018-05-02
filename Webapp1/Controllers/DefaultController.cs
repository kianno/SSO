using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Webapp1.Models;

namespace Webapp1.Controllers
{
    [ValidateAccess]
    public class DefaultController : Controller
    {
        // GET: Default
        public ActionResult Index()
        {
           
            return View();
        }
    }
}