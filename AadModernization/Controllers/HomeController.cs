using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;

namespace AadModernization.Controllers
{
    [AllowAnonymous]
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            var xon = ConfigurationManager.ConnectionStrings["BrutusConnectionString"];
            if (xon == null) //missing xon string
            {
                UpdateStatus(false, "hmmm...");
                return View(new List<NYCCallData>());
            }
            
            try
            {
                var bdb = new BrutusDataContext();
                var model = bdb.NYCCallDatas.OrderByDescending(x => x.Created_Date).Take(20).ToList();
                UpdateStatus(true, "Online", xon);
                return View(model);
            }
            catch (Exception ex) //valid xon string, but can't connect
            {
                UpdateStatus(false, ex.Message, xon);
                return View(new List<NYCCallData>());
            }
        }

        private void UpdateStatus(bool success, string message, ConnectionStringSettings xon = null)
        {
            ViewBag.StatusMessage = message;

            if (success && xon != null)
            {
                ViewBag.ConnectionString = xon.ConnectionString.Replace("p@ssw0rd123", "LOL");
                ViewBag.StatusClass = "bg-Green";
                return;
            }

            ViewBag.ConnectionString = "Not connected!";
            ViewBag.StatusClass = "bg-Red";
            
        }

        [Authorize]
        public ActionResult Claims()
        {
            
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}