using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;

namespace WebApi.Models
{
    public class ExistsResponse
    {
        public bool Exists { get; set; }
    }
}

public class Settings
{
    public static bool IsTest
    {
        get { return bool.Parse(WebConfigurationManager.AppSettings["IsTestEvironment"]); }
    }
}