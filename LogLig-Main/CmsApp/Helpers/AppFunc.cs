using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Web.Mvc;

public static class AppFunc
{
    public static SelectList GetGridItemsCombo(object selected)
    {
        var resList = new List<SelectListItem>();

        for (int i = GlobVars.GridItems; i <= 35; i += 10)
        {
            string val = i.ToString();
            resList.Add(new SelectListItem { Value = val, Text = val });
        }

        return new SelectList(resList, "Value", "Text", selected);
    }

    public static string GetUniqName()
    {
        return DateTime.Now.ToString("ddMMyyyyHHmmssfff");
    }

    public static bool IsInAnyRole(this IPrincipal principal, params string[] roles)
    {
        return roles.Any(principal.IsInRole);
    }

    public static IEnumerable<SelectListItem> GetHebWeekDays()
    {
        var resList = new List<SelectListItem>();
        string days = "אבגדהוש";
        int i = 0;

        foreach (char ch in days)
        {
            var item = new SelectListItem();
            item.Text = ch.ToString();
            item.Value = (i++).ToString();
            resList.Add(item);
        }

        return resList;
    }
}