using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CmsApp.Controllers
{
    public class HomeController : AdminController
    {
        //
        // GET: /Home/

        public ActionResult Index()
        {
            if (User.IsInRole(AppRole.Workers)) 
            {
                return RedirectToAction("Index", "WorkerHome");
            }

            return RedirectToAction("Index", "Sections");
        }
    }
}