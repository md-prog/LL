using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace LogLigFront.Models
{
  public static class UIHelper
  {
    public static string DefaultImage = "~/content/img/default.png";

    public static string GetTeamLogo(string imgName)
    {
      if (!string.IsNullOrEmpty(imgName))
      {
        return String.Concat(ConfigurationManager.AppSettings["SiteUrl"], "/Assets/teams/" + imgName);
      }
      else
      {
        return VirtualPathUtility.ToAbsolute(DefaultImage);
      }
    }

    public static string GetLeagueLogo(string imgName)
    {
      if (!string.IsNullOrEmpty(imgName))
      {
        return String.Concat(ConfigurationManager.AppSettings["SiteUrl"], "/Assets/league/" + imgName);
      }
      else
      {
        return string.Empty;
      }
    }
  }
}