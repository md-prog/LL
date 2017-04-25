using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

using CmsApp.Helpers;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Threading;
using DataService.Jobs.Registry;
using FluentScheduler;

namespace CmsApp
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            ModelBinders.Binders.Add(typeof(DateTime?), new DateModelBinder());

            ClientDataTypeModelValidatorProvider.ResourceClassKey = "Messages";
            DefaultModelBinder.ResourceClassKey = "Messages";

            DataAnnotationsModelValidatorProvider.RegisterAdapter(
                    typeof(RequiredAttribute),
                    typeof(CustRequiredAttributeAdapter)
                );

            DataAnnotationsModelValidatorProvider.RegisterAdapter(
                    typeof(RangeAttribute),
                    typeof(CustRangeAttributeAdapter)
                );

            GlobalConfiguration.Configuration.EnsureInitialized(); 

            //JobManager.Initialize(new GamesRegistry());
            

        }

        protected void Application_BeginRequest()
        {
            HttpCookie cookie = Request.Cookies["_culture"];
            if(cookie != null)
            {
                var ci = CultureInfo.GetCultureInfo(cookie.Value);
                
                Thread.CurrentThread.CurrentCulture = new CultureInfo("he-IL");
                Thread.CurrentThread.CurrentUICulture = ci;
            }
        }
    }
}
