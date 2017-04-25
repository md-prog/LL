using System.Web.Optimization;

namespace CmsApp
{
  public class BundleConfig
  {
    public static void RegisterBundles(BundleCollection bundles)
    {
      bundles.Add(new ScriptBundle("~/bundles/js").Include(
          "~/content/js/jquery-{version}.js",
          "~/content/js/jquery-ui.min.js",
          "~/content/js/jquery.validate*",
          "~/content/js/jquery.unobtrusive-ajax.js",
          "~/content/typeahead/typeahead.bundle.js",
          "~/content/js/jquery.form.js",
          "~/content/js/messages_en.js",
          "~/content/js/bootstrap.js",
          "~/content/js/gridmvc.js",
          "~/content/js/main.js"));
       
      bundles.Add(new StyleBundle("~/content/style/css").Include(
          "~/content/css/bootstrap.css",
          "~/content/css/bootstrap-theme.css",
          "~/content/css/Gridmvc.css",
          "~/content/typeahead/typeahead-bootstrap.css",
          "~/content/css/style.css"));

      /* Localization */

      bundles.Add(new ScriptBundle("~/bundles/js/he").Include(
          "~/content/js/messages_he.js"));

      bundles.Add(new StyleBundle("~/content/style/rtl").Include(
          "~/content/css/bootstrap-rtl.css",
          "~/content/css/rtl-fix.css"));
    }
  }
}
