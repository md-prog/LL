using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using CmsApp.Models;
using CmsApp.Helpers;
using DataService;
using System.Threading.Tasks;
using System.Net;
using CmsApp.Services;
using System.Configuration;

namespace CmsApp.Controllers
{
    public class PlanController : Controller
    {
        [HttpGet]
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Index(PlanSendViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index");
            }

            EmailService emailService = new EmailService();
            string body = string.Empty;
            using (StreamReader reader = new StreamReader(Server.MapPath("~/Views/Plan/EmailTeamplate.cshtml")))
            {
                body = reader.ReadToEnd();
            }
            body = body.Replace("{Field1}", model.Field1);
            body = body.Replace("{Field2}", model.Field2);
            body = body.Replace("{Field3}", model.Field3);
            body = body.Replace("{Field4}", model.Field4);
            body = body.Replace("{Field5}", model.Field5);
            body = body.Replace("{Field7}", model.Field7);
            body = body.Replace("{Field8}", model.Field8);

            try
            {
                emailService.SendAsync(ConfigurationManager.AppSettings["MailAdminEmailAddress"], body);
            }
            catch (Exception ex)
            {
                TempData["EmailError"] = ex.Message;
                return RedirectToAction("Index");
            }

            return RedirectToAction("Index", "Thanks");
        }
        

        public class PlanSendViewModel
        {
            [Required]
            public string Field1 { get; set; }

            [Required]
            public string Field2 { get; set; }

            [Required]
            public string Field3 { get; set; }

            [Required]
            public string Field4 { get; set; }

            [Required]
            public string Field5 { get; set; }
            
            [Required]
            public string Field7 { get; set; }

            
            public string Field8 { get; set; }
        }
    }

    public class ThanksController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

    }

    public class LoginengController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

    }

    public class TermsController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

    }

    public class LoginController : Controller
    {
        //
        // GET: /Login/

        public ActionResult Index()
        {
            var vm = new LoginForm();
            vm.IsSecure = IsCaptchaCookie();

            return View(vm);
        }

        [NoCache]
        public ActionResult Captcha()
        {
            return new Helpers.ImageResult();
        }

        [HttpPost]
        public ActionResult Index(LoginForm frm, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("LgnErr", "נא מלא את פרטי ההתחברות");
                return View(frm);
            }

            bool isCaptcha = IsCaptchaCookie();
            if (isCaptcha && !IsValidCaptcha(frm.Captcha))
            {
                ModelState.AddModelError("LgnErr", "קוד אבטחה שגוי");
                ModelState.AddModelError("Captcha", "Err");
                frm.IsSecure = true;

                return View(frm);
            }

            int tries = AnonymousTries();
            if (tries == 1)
            {
                SetCaptchaCookie(true);
            }

            var uRep = new UsersRepo();
            var usr = uRep.GetByUsername(frm.UserName.Trim());

            if (usr == null)
            {
                usr = uRep.GetByIdentityNumber(frm.UserName.Trim());
            }

            if (usr == null)
            {
                AnonymousTriesAdd();

                ModelState.AddModelError("LgnErr", "שם משתמש או סיסמה שגויים");
                ModelState.AddModelError("Password", "Err");
                return View(frm);
            }

            if (usr.IsBlocked)
            {
                ModelState.AddModelError("LgnErr", "משתמש זה נחסם, יש לפנות להנהלת האתר");
                return View(frm);
            }

            string usrPass = Protector.Decrypt(usr.Password);
            if (frm.Password != usrPass)
            {
                ModelState.AddModelError("LgnErr", "שם משתמש או סיסמה שגויים");
                usr.TriesNum += 1;

                if (usr.TriesNum > 2)
                {
                    frm.IsSecure = true;
                    SetCaptchaCookie(true);
                }

                if (usr.TriesNum >= 10)
                {
                    usr.TriesNum = 0;
                    usr.IsBlocked = true;
                }

                uRep.Save();
                return View(frm);
            }

            SetCaptchaCookie(false);

            string currSession = usr.SessionId;

            usr.TriesNum = 0;
            usr.SessionId = Session.SessionID;
            uRep.Save();

            string userData = usr.UsersType.TypeRole + "^" + usr.SessionId;
            string userId = usr.UserId.ToString();

            LoginService.UpdateSessions(usr.SessionId, currSession);

            var ticket = new FormsAuthenticationTicket(1, userId,
                DateTime.Now,
                DateTime.Now.AddHours(3),
                frm.IsRemember,
                userData,
                FormsAuthentication.FormsCookiePath);

            string encTicket = FormsAuthentication.Encrypt(ticket);
            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encTicket);
            cookie.Expires = ticket.Expiration;
            Response.Cookies.Add(cookie);

            SetAdminCookie(usr.FullName, usr.UsersType.TypeRole);

            if (!string.IsNullOrEmpty(returnUrl))
            {
                Redirect(returnUrl);
            }

            //For Testing
            //var test = uRep.GetById(54);
            //System.Diagnostics.Debug.WriteLine("*" + test.UserName + "*");
            //System.Diagnostics.Debug.WriteLine("*" + Protector.Decrypt(test.Password) + "*");

            //test = uRep.GetById(13);
            //System.Diagnostics.Debug.WriteLine("*" + test.UserName + "*");
            //System.Diagnostics.Debug.WriteLine("*" + Protector.Decrypt(test.Password) + "*");

            return Redirect(FormsAuthentication.DefaultUrl);
        }

        private void SetAdminCookie(string name, string role)
        {
            var c = new HttpCookie("cmsdata");
            c.Values.Add("uname", HttpUtility.UrlEncode(name));
            c.Values.Add("utype", HttpUtility.UrlEncode(role));
            c.Expires = DateTime.Now.AddDays(1);

            Response.Cookies.Add(c);
        }

        private void SetCaptchaCookie(bool isAdd)
        {
            int day = isAdd ? 1 : -1;
            var c = new HttpCookie("capterr", "true");
            c.Expires = DateTime.Now.AddDays(day);

            Response.Cookies.Add(c);
        }

        public bool IsCaptchaCookie()
        {
            return Request.Cookies["capterr"] != null;
        }

        private bool IsValidCaptcha(string captcha)
        {
            if (string.IsNullOrWhiteSpace(captcha))
                return false;

            if (TempData["Captcha"] == null)
                return false;

            string captTxt = TempData["Captcha"].ToString();

            return captcha.ToUpper() == captTxt;
        }

        private int AnonymousTries()
        {
            if (Session["lgntries"] == null)
                return 0;

            return (int)Session["lgntries"];
        }

        private void AnonymousTriesAdd()
        {
            int tries = AnonymousTries();

            Session["lgntries"] = ++tries;
        }
    }
}
