using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Omu.ValueInjecter;
using AppModel;
using CmsApp.Models;
using DataService;
using System.Web.Mvc.Html;
using CmsApp.Models.Mappers;
using DataService.LeagueRank;
using Resources;

namespace CmsApp.Controllers
{
    public class GamesController : AdminController
    {
        // GET: Games
        public ActionResult Edit(int idLeague, int idStage)
        {
            var frm = new GameForm();
            frm.GroupsNum = 1;
            frm.StartDate = DateTime.Now;

            frm.LeagueId = idLeague;
            Session["idLeague"] = idLeague;
            frm.DaysList = Messages.WeekDays.Split(',');
            Game game = gamesRepo.GetByLeagueStage(idLeague, idStage);
            if (game == null)
            {
                game = new Game();
                game.GamesInterval = "00:30";
                game.NumberOfSequenceRounds = 0;
                game.StartDate = DateTime.Now;
                game.PointsWin = 2;
                game.PointsTechWin = 2;
                game.PointsTechLoss = 0;
                game.PointsLoss = 1;
                game.PointsDraw = 0;
                game.SortDescriptors = "1";

            }
            game.StageId = idStage;
            frm.StageId = idStage;
            if (game != null)
            {
                frm.InjectFrom(game);
                frm.NumberOfSequenceRounds = game.NumberOfSequenceRounds.HasValue ? game.NumberOfSequenceRounds.Value : 0;

                List<SelectListItem> sortTypesList = new List<SelectListItem>();
                var sortDescIds = game.SortDescriptors.Split(',').Select(s => Convert.ToInt32(s));
                foreach (var sortId in sortDescIds)
                {
                    LeagueSortDescriptors type = (LeagueSortDescriptors)sortId;
                    string value = sortId.ToString();
                    string text = "";
                    switch (sortId)
                    {
                        case 0:
                            text = Messages.Points;
                            break;
                        case 1:
                            text = Messages.Wins;
                            break;
                        case 2:
                            text = Messages.SetDiffs;
                            break;
                    }

                    SelectListItem si = new SelectListItem
                    {
                        Text = text,
                        Value = value
                    };
                    sortTypesList.Add(si);
                }
            }

            //frm.SortTypes = sortTypesList;
            return PartialView("_Edit", frm);
        }

        public ActionResult GamesUrl(int clubId, int teamId, int? seasonId)
        {
            var team = teamRepo.GetScheduleScrapperById(teamId, clubId);
            var teamGames = teamRepo.GetTeamGamesFromScrapper(clubId,teamId).ToViewModel();
            var viewModel = new GamesUrlForm
            {
                ClubId = clubId,
                TeamId = teamId,
                GamesUrl = team?.GameUrl ?? string.Empty,
                TeamSchedule = teamGames,
                TeamName = team?.ExternalTeamName
            };


            return PartialView("~/Views/Schedules/GamesUrls/Index.cshtml", viewModel);
        }

        [HttpPost]
        public ActionResult Edit(GameForm frm, string[] daysArr)
        {
            if (!ModelState.IsValid)
            {
                return View(frm);
            }

            Game item;
            item = gamesRepo.GetByLeagueStage(frm.LeagueId, frm.StageId);
            if (item == null)
            {
                item = new Game();
                item.StageId = frm.StageId;
                item.SortDescriptors = "0,1,2";
                gamesRepo.Create(item);
            }


            UpdateModel(item);

            item.GameDays = string.Join(",", daysArr);
            item.LeagueId = (int)Session["idLeague"];
            //item.SortDescriptors = string.Join(",", frm.NewSortTypes);

            gamesRepo.Save();

            TempData["Saved"] = true;

            return RedirectToAction("Edit", new { id = (int)Session["idLeague"] });
        }
    }
}