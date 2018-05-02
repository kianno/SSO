using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Webapp1.Models;


namespace Webapp1.Controllers
{
    public class UsersController : Controller
    {
        // GET: Users
        public ActionResult Index()
        {
            return View();
        }

        // GET: Users/Details/5
        public ActionResult Details(int id)
        {
            return View();
        }

        // GET: Users/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        public async Task<ActionResult> Create(LoginVM vm)
        {
            try
            {
                
                var postData = new List<KeyValuePair<string, string>>();
                postData.Add(new KeyValuePair<string, string>("Username", vm.Username));
                postData.Add(new KeyValuePair<string, string>("Password", vm.Password));

                HttpContent content = new FormUrlEncodedContent(postData);

                HttpResponseMessage msg = await DoApiPost("http://localhost/webservices/api/User/create", content);

                if (msg.IsSuccessStatusCode)
                {
                    TempData["InfoMSG"] = "User created successfully.";

                }
                else
                {
                    var dataSerializer = new JavaScriptSerializer();
                    var retval = dataSerializer.Deserialize<Dictionary<string, dynamic>>(await msg.Content.ReadAsStringAsync());

                    var retmsg = "";
                    if (retval.ContainsKey("ModelState"))
                    {
                        Dictionary<string, object> d = retval["ModelState"];
                        if (d.Keys.Count > 0)
                        {
                            ArrayList first = (ArrayList)d.First().Value;
                            if (first.Count > 0)
                            {
                                retmsg += " " + first[0];
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(retmsg))
                        TempData["ErrorMSG"] = retmsg;

                }


                return RedirectToAction("Create", "Users");
            }
            catch(Exception ex)
            {
                return View();
            }
        }

        // GET: Users/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Users/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        // GET: Users/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Users/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        private async Task<HttpResponseMessage> DoApiPost(string path, HttpContent content)
        {
            var client = new HttpClient();
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
            client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("EN"));
            var url = new Uri(path).ToString();
            return await client.PostAsync(url, content);
        }
    }
}
