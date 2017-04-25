using System;
using System.IO;
using System.Configuration;

public static class GlobVars
{
    public static readonly int GridItems = 15;

    private static string GetValue(string name)
    {
        return ConfigurationManager.AppSettings[name];
    }

    public static string ContentPath
    {
        get { return GetValue("ContentPath"); }
    }

    public static string SiteUrl
    {
        get { return GetValue("SiteUrl"); }
    }

    public static string[] ValidImages
    {
        get { return GetValue("ValidImages").Split('|'); }
    }

    public static int MaxFileSize
    {
        get { return int.Parse(GetValue("MaxFileSize")); }
    }

    public static bool IsTest
    {
        get { return bool.Parse(GetValue("IsTestEvironment")); }
    }

    public static string PdfRoute
    {
        get { return GetValue("PdfRoute"); }
    }

    public static string PdfUrl
    {
        get { return GetValue("PdfUrl"); }
    }

    public static string ClubContentPath
    {
        get { return Path.Combine(ContentPath, "Clubs/"); }
    }
}