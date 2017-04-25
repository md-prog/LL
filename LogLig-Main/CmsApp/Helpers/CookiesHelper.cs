using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;

/// <summary>
/// Summary description for CookiesHelper
/// </summary>
public static class CookiesHelper
{
    public const string CookieName = "cmsdata";

    public static bool IsCookieExistst()
    {
        var cookie = HttpContext.Current.Request.Cookies.Get(CookieName);
        return cookie != null;
    }
    public static string GetCookieValue(string valName)
    {
        var cookie = HttpContext.Current.Request.Cookies.Get(CookieName);
        if (cookie != null)
            return HttpUtility.UrlDecode(cookie.Values[valName]);
        else
            return "";
    }
    public static void SetCookieValue(string valName, string val)
    {
        var cookie = HttpContext.Current.Request.Cookies.Get(CookieName);
        if (cookie == null)
        {
            cookie = new HttpCookie(CookieName);
        }
        cookie.Values[valName] = HttpUtility.UrlEncode(val);
        cookie.Expires = DateTime.Now.AddDays(1d);

        HttpContext.Current.Response.Cookies.Set(cookie);
    }
    public static void RemoveCookie(string cookieName)
    {
        var cookie = HttpContext.Current.Request.Cookies.Get(cookieName);
        if (cookie != null)
        {
            cookie.Expires = DateTime.Now.AddDays(-1d);
            HttpContext.Current.Response.Cookies.Add(cookie);
        }
    }
    public static void SetCookie(string cName, object cValue, DateTime expires)
    {
        var cookie = HttpContext.Current.Request.Cookies[cName];
        if (cookie == null)
        {
            cookie = new HttpCookie(cName);
        }
        cookie.Value = cValue.ToString();
        cookie.Expires = expires;

        HttpContext.Current.Response.Cookies.Set(cookie);
    }
    public static string GetCookie(string cName)
    {
        var cookie = HttpContext.Current.Request.Cookies[cName];
        if (cookie != null)
            return HttpUtility.UrlDecode(cookie.Value);
        else
            return "";
    }
}