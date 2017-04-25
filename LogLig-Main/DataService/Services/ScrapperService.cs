using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Hosting;
using DataService.DTO;
using DataService.Utils;
using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Support.UI;

namespace DataService.Services
{
    public class ScrapperService : IDisposable
    {
        #region Fields & constructor
        private readonly IWebDriver _driver;
        private string Url { get; set; }

        public ScrapperService(string url)
        {
            if (_driver == null)
                _driver = new PhantomJSDriver(PhantomJSDriverService.CreateDefaultService(HostingEnvironment.MapPath("/")));

            _driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 30));

            Url = url;
        }

        public ScrapperService()
        {
            ProcessHelper.StartProcessIfNotStarted();
            if (_driver == null)
                _driver = new PhantomJSDriver();
            //_driver = new PhantomJSDriver(PhantomJSDriverService.CreateDefaultService(HostingEnvironment.MapPath("/")));

            _driver.Manage().Timeouts().ImplicitlyWait(new TimeSpan(0, 0, 40));
        }
        #endregion

        public List<SchedulerDTO> SchedulerScraper(string url)
        {
            var doc = GetDocumentAndClick(url);

            var model = new List<SchedulerDTO>();

            ReadShceduleScrapperFromHTML(doc, model, url);
            //var tableRows = doc.DocumentNode.SelectSingleNode("//table[@id='mbt-v2-team-schedule-and-results-tab']")?.SelectNodes(".//tbody//tr");
            //if (tableRows != null)
            //{
            //    foreach (var tableRow in tableRows)
            //    {
            //        var tableCell = tableRow.SelectNodes(".//td");

            //        try
            //        {
            //            model.Add(new SchedulerDTO
            //            {
            //                Time = tableCell.GetLinkTextFromHtmlNode(0),
            //                HomeTeam = tableCell.GetLinkTextFromHtmlNode(1),
            //                HomeTeamScore = tableCell.GetTeam1Score(2),
            //                GuestTeamScore = tableCell.GetTeam2Score(2),
            //                GuestTeam = tableCell.GetLinkTextFromHtmlNode(3),
            //                Auditorium = tableCell.GetTextFromHtmlNode(4),
            //                Url = url

            //            });
            //        }
            //        catch (Exception ex)
            //        {

            //            Trace.WriteLine(ex.Message);
            //        }
                   
            //    }
              
                
            //}

            var nextPage = NextPage();
            do
            {
                if (nextPage != null)
                {
                    ReadShceduleScrapperFromHTML(nextPage, model, url);
                    //tableRows = nextPage.DocumentNode.SelectSingleNode("//table[@id='mbt-v2-team-schedule-and-results-tab']")?.SelectNodes(".//tbody//tr");
                    //if (tableRows != null)
                    //{
                    //    foreach (var row in tableRows)
                    //    {
                    //        var tableCell = row.SelectNodes(".//td");
                    //        try
                    //        {
                    //            model.Add(new SchedulerDTO
                    //            {
                    //                Time = tableCell.GetLinkTextFromHtmlNode(0),
                    //                HomeTeam = tableCell.GetLinkTextFromHtmlNode(1),
                    //                HomeTeamScore = tableCell.GetTeam1Score(2),
                    //                GuestTeamScore = tableCell.GetTeam2Score(2),
                    //                GuestTeam = tableCell.GetLinkTextFromHtmlNode(3),
                    //                Auditorium = tableCell.GetTextFromHtmlNode(4),
                    //                Url = url

                    //            });
                    //        }
                    //        catch (Exception ex)
                    //        {
                    //            Trace.WriteLine(ex.Message);
                    //        }
                    //    }
                    //}
                    nextPage = NextPage();
                }
            } while (nextPage != null);

            return model;

        }

        public void ReadShceduleScrapperFromHTML(HtmlDocument doc, List<SchedulerDTO> model, string url)
        {
            var tableRows = doc.DocumentNode.SelectSingleNode("//table[@id='mbt-v2-team-schedule-and-results-tab']")?.SelectNodes(".//tbody//tr");
            if (tableRows != null)
            {
                foreach (var tableRow in tableRows)
                {
                    var tableCell = tableRow.SelectNodes(".//td");
                    try
                    {
                        model.Add(new SchedulerDTO
                        {
                            Time = tableCell.GetLinkTextFromHtmlNode(0),
                            HomeTeam = tableCell.GetLinkTextFromHtmlNode(1),
                            HomeTeamScore = tableCell.GetTeam1Score(2),
                            GuestTeamScore = tableCell.GetTeam2Score(2),
                            GuestTeam = tableCell.GetLinkTextFromHtmlNode(3),
                            Auditorium = tableCell.GetTextFromHtmlNode(4),
                            Url = url

                        });
                    }
                    catch (Exception ex)
                    {
                        
                       Trace.WriteLine(ex.Message);
                    }
                }
            }
        }




        public List<StandingDTO> StandingScraper(string url)
        {
            var doc = GetHtmlDocument(url);

            var model = new List<StandingDTO>();

            var tableRows = doc.DocumentNode.SelectNodes(".//table//tbody//tr");
            if (tableRows != null)
            {
                int counter = tableRows.Count;

                for (int i = 0; i < counter; i++)
                {
                    var tableCells = tableRows[i].SelectNodes(".//td");

                    try
                    {
                        model.Add(new StandingDTO
                        {
                            Rank = tableCells.GetTextFromHtmlNode(0),
                            Team = tableCells.GetLinkTextFromHtmlNode(1),
                            Games = tableCells.GetTextFromHtmlNode(2),
                            Win = tableCells.GetTextFromHtmlNode(3),
                            Lost = tableCells.GetTextFromHtmlNode(4),
                            Pts = tableCells.GetTextFromHtmlNode(5),
                            PaPf = tableCells.GetTextFromHtmlNode(6),
                            PlusMinusField = tableCells.GetTextFromHtmlNode(7),
                            Home = tableCells.GetTextFromHtmlNode(8),
                            Road = tableCells.GetTextFromHtmlNode(9),
                            ScoreHome = tableCells.GetTextFromHtmlNode(10),
                            ScoreRoad = tableCells.GetTextFromHtmlNode(11),
                            Last5 = tableCells.GetTextFromHtmlNode(12),
                            Url = url
                        });
                    }
                    catch (Exception e)
                    {

                        Trace.WriteLine(e.Message);
                    }
                }
            }

            return model;
        }

        public void Quit()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }

        private HtmlDocument GetHtmlDocument()
        {
            _driver.Navigate().GoToUrl(Url);
            var doc = new HtmlDocument();
            doc.LoadHtml(_driver.PageSource);
            return doc;
        }

        private HtmlDocument GetHtmlDocument(string url)
        {
            _driver.Navigate().GoToUrl(url);
            var doc = new HtmlDocument();
            doc.LoadHtml(_driver.PageSource);
            return doc;
        }

        private HtmlDocument GetDocumentAndClick(string url)
        {
            _driver.Manage().Window.Maximize();
            _driver.Navigate().GoToUrl(url);

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            

            var divToClick = _driver.FindElement(By.ClassName("mbt-v2-navigation")).FindElements(By.TagName("div"))[2];

            var el = wait.Until(ExpectedConditions.ElementToBeClickable(divToClick));

            el.Click();

            var divsWithSelect = _driver.FindElement(By.ClassName("mbt-v2-filters-block"));

            var select = divsWithSelect.FindElements(By.TagName("select"))[1];

            var selectElement = new SelectElement(select);
            selectElement.SelectByValue("all");

            wait.Until(x => x.FindElement(By.Id("mbt-v2-team-schedule-and-results-tab")));

            
            
            
            var doc = new HtmlDocument();

            doc.LoadHtml(_driver.PageSource);
            return doc;
        }

        private HtmlDocument NextPage()
        {
            //if no next button presented stop search
            if (_driver.FindElements(By.ClassName("mbt-v2-pagination-next")).Count == 0)
                return null;

            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(30));

            var navElement = _driver.FindElement(By.ClassName("mbt-v2-pagination"));
            if (navElement != null)
            {
                var li = navElement.FindElements(By.TagName("li")).LastOrDefault();
                if (li != null)
                {
                    li.FindElement(By.TagName("a")).Click();
                    wait.Until(x => x.FindElement(By.Id("mbt-v2-team-schedule-and-results-tab")));

                    //var nextPageTable = _driver.FindElement(By.Id("mbt-v2-team-schedule-and-results-tab"));
                    var doc = new HtmlDocument();
                    doc.LoadHtml(_driver.PageSource);
                    return doc;
                }
            }
            return null;
        }

        public void Dispose()
        {
            _driver?.Quit();
            _driver?.Dispose();
        }


    }
}
