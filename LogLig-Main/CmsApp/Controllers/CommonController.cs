using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CmsApp.Models;

namespace CmsApp.Controllers
{
    public class CommonController : AdminController
    {
        //
        // GET: /Common/

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Login");
        }

        public ActionResult Logged()
        {
            var vm = new LoggedView();
            vm.UserName = CookiesHelper.GetCookieValue("uname");
            vm.RoleType = CookiesHelper.GetCookieValue("utype");

            return PartialView("_Logged", vm);
        }

        public ActionResult Protect()
        {
            var configFile = System.Web.Configuration.WebConfigurationManager.OpenWebConfiguration("/Content");
            var configSection = configFile.Sections["connectionStrings"];
            if (!configSection.SectionInformation.IsProtected)
            {
                configSection.SectionInformation.ProtectSection("RsaProtectedConfigurationProvider");
                //configSection.SectionInformation.UnprotectSection();
                //configFile.Save();
            }
            return Content("ok");
        }

        public ActionResult SetCulture(string lang)
        {
            string culture = "he-IL"; // default

            if (lang == "en")
            {
                culture = "en-US";
            }

            HttpCookie cookie = Request.Cookies["_culture"];
            if (cookie != null)
            {
                cookie.Value = culture;
            }
            else
            {
                cookie = new HttpCookie("_culture");
                cookie.Value = culture;
                cookie.Expires = DateTime.Now.AddYears(1);
            }

            Response.Cookies.Add(cookie);

            return Redirect(Request.UrlReferrer.ToString());
        }

    }
}
