using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.IO;
using AppModel;
using CmsApp.Models;
using DataService;
using ClosedXML.Excel;
using System.Web;
using System.Globalization;
using System.Threading;
using System.Net;
using DataService.DTO;

namespace CmsApp.Controllers
{
    public class SchedulesController : AdminController
    {
        // GET: Schedules
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult List(int id, bool desOrder, bool isChronological = false,
            int dateFilterType = Schedules.DateFilterPeriod.BeginningOfMonth,
            DateTime? dateFrom = null, DateTime? dateTo = null, int? inpSeasonId = null)
        {
            var res = new Schedules();
            res.dateFilterType = dateFilterType;
            res.dateFrom = dateFrom ?? Schedules.FirstDayOfMonth;
            res.dateTo = dateTo ?? Schedules.Tomorrow;
            var league = leagueRepo.GetById(id);
            var cond = new GamesRepo.GameFilterConditions
            {
                seasonId = inpSeasonId ?? leagueRepo.GetById(id).SeasonId ?? seasonsRepository.GetLastSeason().Id,
                auditoriums = new List<AuditoriumShort>(),
                leagues = new List<LeagueShort>
                {
                    new LeagueShort
                    {
                        Id = id,
                        UnionId = league.UnionId,
                        Check = true,
                        Name = league.Name
                    }
                }
            };
            if (dateFilterType == Schedules.DateFilterPeriod.BeginningOfMonth && isChronological)
            {
                cond.dateFrom = Schedules.FirstDayOfMonth;
                cond.dateTo = null;
            }
            else if (dateFilterType == Schedules.DateFilterPeriod.Ranged && isChronological)
            {
                cond.dateFrom = dateFrom;
                cond.dateTo = dateTo;
            }
            else
            {
                cond.dateFrom = null;
                cond.dateTo = null;
            }
            res.Games = gamesRepo.GetCyclesByFilterConditions(cond,
                User.IsInAnyRole(AppRole.Admins, AppRole.Editors, AppRole.Workers), false)
                .Select(gc => new GamesInLeague(gc)
                {
                    LeagueId = gc.Stage.LeagueId,
                    LeagueName = gc.Stage.League.Name,
                    IsPublished = gc.IsPublished,
                }).ToList();

            if (desOrder)
                res.Games = res.Games.OrderByDescending(x => x.Stage.Number).ToList();

            string alias = string.Empty;

            Session["desOrder"] = desOrder;
            var unionId = league.UnionId;
            if (unionId.HasValue)
            {
                res.Referees = usersRepo.GetUnionAndLeageReferees(unionId.Value, id).ToArray();

                res.Auditoriums = auditoriumsRepo.GetByUnionAndSeason(unionId.Value, cond.seasonId)
                    .Select(au => new AuditoriumShort
                    {
                        Id = au.AuditoriumId,
                        Name = au.Name
                    }).ToArray();

                alias = league.Union.Section?.Alias;
            }

            res.Leagues = cond.leagues.ToArray();
            res.SeasonId = cond.seasonId;
            res.UnionId = unionId;
            res.teamsByGroups = teamRepo.GetGroupTeamsBySeasonAndLeagues(cond.seasonId, new int[] { league.LeagueId });
            Session["isChronological"] = isChronological;

            switch (alias)
            {
                case GamesAlias.WaterPolo:
                case GamesAlias.BasketBall:
                    if (isChronological)
                    {
                        return PartialView("BasketBallWaterPolo/_ChronologicalList", res);
                    }
                    else
                    {
                        return PartialView("BasketBallWaterPolo/_List", res);
                    }

                default:
                    if (isChronological)
                    {
                        return PartialView("_ChronologicalList", res);
                    }
                    else
                    {
                        return PartialView("_List", res);
                    }

            }



        }

        [HttpPost]
        public ActionResult PublishAllLeagueGamesCycles(int seasonId, int leagueId, bool isPublished)
        {
            try
            {
                IEnumerable<GamesCycle> games = gamesRepo.GetGroupsCycles(leagueId, seasonId).ToList();
                games.ForEach(g => g.IsPublished = isPublished);

                gamesRepo.Update(games);

                return new HttpStatusCodeResult(200);
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(500);
            }
        }

        [HttpPost]
        public ActionResult PublishGamesCyclesByCycleNumber(int seasonId, int leagueId, int stageId, int cycleNum, bool isPublished)
        {
            try
            {
                var games = gamesRepo.GetGroupsCycles(leagueId, seasonId)
                                      .Where(g => g.CycleNum == cycleNum && g.StageId == stageId)
                                      .ToList();
                games.ForEach(g => g.IsPublished = isPublished);

                gamesRepo.Update(games);

                return new HttpStatusCodeResult(200);
            }
            catch (Exception)
            {
                return new HttpStatusCodeResult(500);
            }
        }

        [HttpPost]
        public ActionResult PublishGamesCycle(int gameCycleId, bool isPublished)
        {
            try
            {
                GamesCycle gameCycle = gamesRepo.GetGameCycleById(gameCycleId);
                gameCycle.IsPublished = isPublished;

                gamesRepo.Update(gameCycle);

                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, e.ToString());
            }
        }

        public ActionResult TeamList(int id, int seasonId)
        {
            var model = new TeamSchedules();
            model.TeamId = id;
            model.SeasonId = id;
            model.Clubs = clubsRepo.GetByTeamAnsSeason(id, seasonId).Where(c => c.IsSectionClub.Value).ToList();
            model.LeaguesWithCycles = teamRepo.GetTeamGames(id, seasonId).ToList();
            string alias = secRepo.GetSectionByTeamId(id).Alias;

            switch (alias)
            {
                case GamesAlias.BasketBall:
                case GamesAlias.WaterPolo:
                    return PartialView("BasketBallWaterPolo/_TeamList", model);

                default:
                    return PartialView("_TeamList", model);

            }
        }

        public ActionResult ExportToExcelUnion(List<int> leaguesId, int sortType, int seasonId)
        {
            leaguesId = (List<int>)Session["LeaguesIds"];
            var xlsService = new ExcelGameService();
            bool isBasketBallOrWaterPolo = false;
            var games = new List<ExcelGameDto>();
            foreach (var leagueId in leaguesId)
            {
                games.AddRange(xlsService.GetLeagueGames(leagueId, seasonId));
            }

            if (games?.Count() > 0)
            {
                isBasketBallOrWaterPolo = gamesRepo.IsBasketBallOrWaterPoloGameCycle(games[0].GameId);
            }

            if (sortType == 1)
                games = games.OrderBy(x => x.Date).ToList();
            if (sortType == 2)
                games = games.OrderBy(x => x.Auditorium).ToList();

            return ToExcel(games, isBasketBallOrWaterPolo);
        }

        public ActionResult ExportToExcel(int? leagueId, int? teamId, int? currentLeagueId, int? seasonId,
            int dateFilterType = Schedules.DateFilterPeriod.All,
            DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var xlsService = new ExcelGameService();
            bool userIsEditor = User.IsInAnyRole(AppRole.Admins, AppRole.Editors, AppRole.Workers);
            var games = new List<ExcelGameDto>();
            bool isBasketBallOrWaterPolo = false;
            if (leagueId.HasValue)
            {
                isBasketBallOrWaterPolo = leagueRepo.IsBasketBallOrWaterPoloLeague(leagueId.Value);
                switch (dateFilterType)
                {
                    case Schedules.DateFilterPeriod.BeginningOfMonth:
                        games = xlsService.GetLeagueGames(leagueId.Value, userIsEditor, Schedules.FirstDayOfMonth, null).ToList();
                        break;
                    case Schedules.DateFilterPeriod.Ranged:
                        games = xlsService.GetLeagueGames(leagueId.Value, userIsEditor, dateFrom, dateTo).ToList();
                        break;
                    default:
                        games = xlsService.GetLeagueGames(leagueId.Value, userIsEditor, null, null).ToList();
                        break;
                }
            }
            else if (teamId.HasValue)
            {
                if (!seasonId.HasValue)
                {
                    seasonId = teamRepo.GetSeasonIdByTeamId(teamId.Value, DateTime.Now);
                }
                isBasketBallOrWaterPolo = seasonsRepository.IsBasketBallOrWaterPoloSeason(seasonId.Value);
                if (currentLeagueId.HasValue)
                {
                    games = xlsService.GetTeamGames(teamId.Value, currentLeagueId.Value, seasonId).ToList();
                }
                else
                {
                    games = xlsService.GetTeamGames(teamId.Value, seasonId).ToList();
                }
            }

            return ToExcel(games, isBasketBallOrWaterPolo);
        }

        [HttpPost]
        public ActionResult ExportToExcelUnion(List<int> leaguesId, int sortType, FormCollection form)
        {
            var xlsService = new ExcelGameService();
            var gameIds = String.IsNullOrWhiteSpace(form["gameIds1"])
             ? new int[] { }
           : form["gameIds1"].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
             .Select(s => int.Parse(s))
             .ToArray();
            bool isBacketBallOrWaterPolo = false;

            var games = xlsService.GetGameCyclesByIdSet(gameIds).ToList();
            if (games?.Count() > 0)
            {
                isBacketBallOrWaterPolo = gamesRepo.IsBasketBallOrWaterPoloGameCycle(games[0].GameId);
            }
            if (sortType == 1)
                games = games.OrderBy(x => x.Date).ToList();
            if (sortType == 2)
                games = games.OrderBy(x => x.Auditorium).ToList();

            return ToExcel(games, isBacketBallOrWaterPolo);
        }

        [HttpPost]
        public void CheckGames(FormCollection form)
        {
            var gameIds = String.IsNullOrWhiteSpace(form["gameIds"])
              ? new int[] { }
            : form["gameIds"].Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
              .Select(s => int.Parse(s))
              .ToArray();

            Session["GameIds"] = gameIds;
        }

        [HttpPost]
        public ActionResult External(int? leagueId, FormCollection form)
        {
            var gameIds = Session["GameIds"] != null ? (int[])Session["GameIds"] : new int[] { };
            var url = GlobVars.SiteUrl + "/LeagueTable/Schedules/" + leagueId + "?gameIds=" + String.Join(",", gameIds);

            return Redirect(url);
        }

        [HttpPost]
        public ActionResult ExportToExcel(int? leagueId, int? teamId, int? currentLeagueId, int? seasonId, FormCollection form)
        {
            var xlsService = new ExcelGameService();
            var gameIds = Session["GameIds"] != null ? (int[])Session["GameIds"] : new int[] { };
            var games = new List<ExcelGameDto>();
            bool isBasketBallOrWaterPolo = false;
            if (leagueId.HasValue)
            {
                isBasketBallOrWaterPolo = leagueRepo.IsBasketBallOrWaterPoloLeague(leagueId.Value);
                games = xlsService.GetLeagueGames(leagueId.Value, gameIds).ToList();
            }
            else if (teamId.HasValue)
            {
                if (!seasonId.HasValue)
                {
                    seasonId = teamRepo.GetSeasonIdByTeamId(teamId.Value, DateTime.Now);
                }
                isBasketBallOrWaterPolo = seasonsRepository.IsBasketBallOrWaterPoloSeason(seasonId.Value);
                if (currentLeagueId.HasValue)
                {
                    games = xlsService.GetTeamGames(teamId.Value, currentLeagueId.Value, gameIds, seasonId).ToList();
                }
                else
                {
                    games = xlsService.GetTeamGames(teamId.Value, gameIds, seasonId).ToList();
                }
            }
            return ToExcel(games, isBasketBallOrWaterPolo);
        }

        private ActionResult ToExcel(IList<ExcelGameDto> games, bool isBasketBallOrWaterPolo = false)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("he-IL");
            using (var workbook = new XLWorkbook(XLEventTracking.Disabled))
            {
                var ws = workbook.Worksheets.Add("Sheet1");

                #region Columns
                ws.Cell(1, 1).Value = "Game Id";

                ws.Cell(1, 2).Value = "League Id";

                ws.Cell(1, 3).Value = "League";

                ws.Cell(1, 4).Value = "Stage";

                ws.Cell(1, 5).Value = "Date";

                ws.Cell(1, 6).Value = "Time";

                ws.Cell(1, 7).Value = "Home Team Id";

                ws.Cell(1, 8).Value = "Home Team";

                ws.Cell(1, 9).Value = "Home Team Score";

                ws.Cell(1, 10).Value = "Guest Team Id";

                ws.Cell(1, 11).Value = "Guest Team";

                ws.Cell(1, 12).Value = "Guest Team Score";

                ws.Cell(1, 13).Value = "Auditorium Id";

                ws.Cell(1, 14).Value = "Auditorium";

                ws.Cell(1, 15).Value = "Referee Id";

                ws.Cell(1, 16).Value = "Referee";

                ws.Cell(1, 17).Value = "Cycle Number";

                ws.Cell(1, 18).Value = "Groupe";

                if (isBasketBallOrWaterPolo)
                {
                    ws.Cell(1, 19).Value = "Q1";
                    ws.Cell(1, 20).Value = "Q2";
                    ws.Cell(1, 21).Value = "Q3";
                    ws.Cell(1, 22).Value = "Q4";
                }
                else
                {
                    ws.Cell(1, 19).Value = "Set1";
                    ws.Cell(1, 20).Value = "Set2";
                    ws.Cell(1, 21).Value = "Set3";
                    ws.Cell(1, 22).Value = "Set4";
                }

                #endregion

                for (var i = 0; i < games.Count; i++)
                {

                    ws.Cell(i + 2, 1).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 1).SetValue(games[i].GameId);

                    ws.Cell(i + 2, 2).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 2).SetValue(games[i].LeagueId);

                    ws.Cell(i + 2, 3).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 3).SetValue(games[i].League);

                    ws.Cell(i + 2, 4).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 4).SetValue(games[i].Stage);

                    ws.Cell(i + 2, 5).DataType = XLCellValues.DateTime;
                    //ws.Cell(i + 2, 5).SetValue(games[i].Date.ToString(new CultureInfo(CultureInfo.CurrentCulture.ToString())));
                    ws.Cell(i + 2, 5).SetValue(games[i].Date.ToString("d"));
                    ws.Cell(i + 2, 6).DataType = XLCellValues.DateTime;
                    ws.Cell(i + 2, 6).SetValue(games[i].Time);

                    ws.Cell(i + 2, 7).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 7).SetValue(games[i].HomeTeamId);

                    ws.Cell(i + 2, 8).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 8).SetValue(games[i].HomeTeam);

                    ws.Cell(i + 2, 9).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 9).SetValue(games[i].HomeTeamScore);

                    ws.Cell(i + 2, 10).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 10).SetValue(games[i].GuestTeamId);

                    ws.Cell(i + 2, 11).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 11).SetValue(games[i].GuestTeam);

                    ws.Cell(i + 2, 12).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 12).SetValue(games[i].GuestTeamScore);

                    ws.Cell(i + 2, 13).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 13).SetValue(games[i].AuditoriumId);

                    ws.Cell(i + 2, 14).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 14).SetValue(games[i].Auditorium);

                    ws.Cell(i + 2, 15).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 15).SetValue(games[i].RefereeId);

                    ws.Cell(i + 2, 16).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 16).SetValue(games[i].Referee);

                    ws.Cell(i + 2, 17).DataType = XLCellValues.Number;
                    ws.Cell(i + 2, 17).SetValue(games[i].CycleNumber);

                    ws.Cell(i + 2, 18).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 18).SetValue(games[i].Groupe);

                    ws.Cell(i + 2, 19).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 19).SetValue(games[i].Set1);

                    ws.Cell(i + 2, 20).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 20).SetValue(games[i].Set2);

                    ws.Cell(i + 2, 21).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 21).SetValue(games[i].Set3);

                    ws.Cell(i + 2, 22).DataType = XLCellValues.Text;
                    ws.Cell(i + 2, 22).SetValue(games[i].Set4);

                }

                var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "ExportGamesList-" + DateTime.Now.ToLongDateString() + ".xlsx");
            }
        }

        public ActionResult Create(int id, int seasonId)
        {
            var serv = new SchedulingService();
            serv.ScheduleGames(id);
            return RedirectToAction("List", new { id = id, desOrder = true, inpSeasonId = seasonId });
        }

        public ActionResult Update(GameCycleForm frm)
        {
            try
            {
                bool isChanged = false;
                var gc = gamesRepo.GetGameCycleById(frm.CycleId);
                if (gc.AuditoriumId != frm.AuditoriumId)
                {
                    isChanged = true;
                    gc.AuditoriumId = frm.AuditoriumId;
                }
                gc.RefereeId = frm.RefereeId;
                if (!gc.StartDate.Equals(frm.StartDate))
                {
                    isChanged = true;
                    gc.StartDate = frm.StartDate;
                }
                //  Teams could be replaced only before game starts
                if (gc.GameStatus != GameStatus.Started && gc.GameStatus != GameStatus.Ended && gc.Group.TypeId == 1)
                {
                    gc.HomeTeamId = frm.HomeTeamId;
                    gc.GuestTeamId = frm.GuestTeamId;
                }
                gamesRepo.Update(gc);

                if(isChanged && gc.IsPublished)
                {
                    NotesMessagesRepo notesRep = new NotesMessagesRepo();
                    if (gc.Stage != null && gc.Stage.League != null && gc.Stage.League.SeasonId != null)
                    {
                        String message = String.Format("Game details has been updated: {0} vs {1}", gc.HomeTeam!=null?gc.HomeTeam.Title:"", gc.GuestTeam != null ? gc.GuestTeam.Title : "");

                        if (gc.HomeTeamId != null)
                        {
                            notesRep.SendToTeam((int)gc.Stage.League.SeasonId, (int)gc.HomeTeamId, message);
                        }
                        if (gc.GuestTeamId != null)
                        {
                            notesRep.SendToTeam((int)gc.Stage.League.SeasonId, (int)gc.GuestTeamId, message);
                        }
                        if (gc.RefereeId != null)
                        {
                            notesRep.SendToUsers(new List<int> { (int)gc.RefereeId }, message);
                        }
                    }

                    var notsServ = new GamesNotificationsService();
                    notsServ.SendPushToDevices(GlobVars.IsTest);
                }
            }
            catch (System.Exception e)
            {
                return Json(new { stat = "error", message = e.ToString() });
            }
            return Json(new { stat = "ok", id = frm.CycleId });
        }

        public ActionResult Groups(int id, int seasonId)
        {
            ViewBag.LeagueId = id;
            var game = gamesRepo.GetByLeague(id);
            if (game != null)
            {
                ViewBag.GameId = game.GameId;
            }
            else
            {
                ViewBag.GameId = 0;
            }
            ViewBag.SeasonId = seasonId;

            return PartialView("_Groups");
        }

        public ActionResult Toggle(int id, bool isChronological = false)
        {
            int leagueId = gamesRepo.ToggleTeams(id);

            return RedirectToAction("List", new { id = leagueId, desOrder = Session["desOrder"], isChronological = isChronological });
        }

        public ActionResult Delete(int id, bool isChronological = false)
        {
            var gc = gamesRepo.GetGameCycleById(id);
            int leagueId = gc.Stage.LeagueId;

            gamesRepo.RemoveCycle(gc);
            gamesRepo.Save();

            return RedirectToAction("List", new { id = leagueId, desOrder = Session["desOrder"], isChronological = isChronological });
        }

        public ActionResult AddNew(int stageId, int num)
        {
            var stage = stagesRepo.GetById(stageId);

            int leagueId = stage.LeagueId;
            int? unionId = stage.League.UnionId;

            var vm = new GameCycleFormFull();

            if (unionId != null)
            {
                var auditoriums = auditoriumsRepo.GetAll(unionId.Value);

                var groups = stage.Groups.Where(gr => gr.IsArchive == false);

                var referees = usersRepo.GetUnionAndLeageReferees(unionId.Value, leagueId);

                vm.StageNum = stage.Number;
                vm.LeagueId = leagueId;
                vm.StageId = stageId;
                vm.CycleNum = num;
                vm.StartDate = DateTime.Now;
                vm.Auditoriums = new SelectList(auditoriums, "AuditoriumId", "Name");
                vm.Referees = new SelectList(referees, "UserId", "FullName");
                vm.Groups = new SelectList(groups, "GroupId", "Name");
            }

            return PartialView("_AddNewForm", vm);
        }

        public ActionResult GoupTeams(int id)
        {
            var leagueTeams = groupRepo.GetById(id).GroupsTeams
                .Where(t => t.Team.IsArchive == false)
                .Select(t => t.Team)
                .ToList();

            ViewBag.TeamsList = new SelectList(leagueTeams, "TeamId", "Title");

            return PartialView("_TeamsView");
        }

        [HttpPost]
        public ActionResult AddNew(GameCycleFormFull frm)
        {
            var gc = new GamesCycle();

            UpdateModel(gc);

            var serv = new SchedulingService();
            serv.AddGame(gc);

            return RedirectToAction("List", new { id = frm.LeagueId, desOrder = Session["desOrder"] });
        }

        [HttpPost]
        public ActionResult MoveDate(int stageId, int cycleNum, DateTime startDate, bool? isAll)
        {
            var stage = stagesRepo.GetById(stageId);

            var serv = new SchedulingService();
            serv.MoveCycles(stageId, cycleNum, startDate, isAll.HasValue);

            return RedirectToAction("List", new { id = stage.LeagueId, desOrder = Session["desOrder"], inpSeasonId = stage.League.SeasonId });
        }

        public ActionResult ImportFromExcel(HttpPostedFileBase importedExcel)
        {
            try
            {

                if (importedExcel != null && importedExcel.ContentLength > 0)
                {
                    var dto = new List<ExcelGameDto>();

                    //open xml file from input
                    using (var workBook = new XLWorkbook(importedExcel.InputStream))
                    {
                        var sheet = workBook.Worksheet(1);
                        //skip column names
                        var valueRows = sheet.RowsUsed().Skip(1).ToList();

                        int i = 0;
                        //iterate over rows in xml file
                        //and getting cell from current row by [i] indexer
                        foreach (var row in valueRows)
                        {
                            int outGameId, outLeagueId, outStage,
                                outHomeTeamId, outHomeTeamScore, outGuestTeamId,
                                outGuestTeamScore, outAuditoriumId, outRefereeId,
                                outCycleNumber;

                            var strDate = sheet.Cell(i + 2, 5).Value.ToString();
                            var outDate = DateTime.UtcNow;
                            DateTime parsedTime;

                            //CurrentCulture may not give us what we expect, if the import fails we should revisit this code and for better culture detection.
                            var localCulture = System.Globalization.CultureInfo.CurrentCulture.ToString();

                            outDate = DateTime.Parse(strDate, new CultureInfo(localCulture, false));
                            var time = sheet.Cell(i + 2, 6).Value.ToString();
                            DateTime.TryParse(time, out parsedTime);
                            var date = new DateTime(outDate.Year, outDate.Month, outDate.Day);

                            int.TryParse(sheet.Cell(i + 2, 1).Value.ToString(), out outGameId);
                            int.TryParse(sheet.Cell(i + 2, 2).Value.ToString(), out outLeagueId);
                            int.TryParse(sheet.Cell(i + 2, 4).Value.ToString(), out outStage);
                            int.TryParse(sheet.Cell(i + 2, 7).Value.ToString(), out outHomeTeamId);
                            int.TryParse(sheet.Cell(i + 2, 9).Value.ToString(), out outHomeTeamScore);
                            int.TryParse(sheet.Cell(i + 2, 10).Value.ToString(), out outGuestTeamId);
                            int.TryParse(sheet.Cell(i + 2, 12).Value.ToString(), out outGuestTeamScore);
                            int.TryParse(sheet.Cell(i + 2, 13).Value.ToString(), out outAuditoriumId);
                            int.TryParse(sheet.Cell(i + 2, 15).Value.ToString(), out outRefereeId);
                            int.TryParse(sheet.Cell(i + 2, 17).Value.ToString(), out outCycleNumber);

                            var dateWithTime = date.AddHours(parsedTime.Hour).AddMinutes(parsedTime.Minute);

                            dto.Add(new ExcelGameDto
                            {
                                GameId = outGameId,
                                LeagueId = outLeagueId,
                                League = sheet.Cell(i + 2, 3).Value.ToString(),
                                Stage = outStage,
                                Date = dateWithTime,
                                HomeTeamId = outHomeTeamId,
                                HomeTeam = sheet.Cell(i + 2, 8).Value.ToString(),
                                HomeTeamScore = outHomeTeamScore,
                                GuestTeamId = outGuestTeamId,
                                GuestTeam = sheet.Cell(i + 2, 11).Value.ToString(),
                                GuestTeamScore = outGuestTeamScore,
                                AuditoriumId = outAuditoriumId,
                                Auditorium = sheet.Cell(i + 2, 14).Value.ToString(),
                                RefereeId = outRefereeId,
                                Referee = sheet.Cell(i + 2, 16).Value.ToString(),
                                CycleNumber = outCycleNumber,
                                Groupe = sheet.Cell(i + 2, 18).Value.ToString(),
                                Set1 = sheet.Cell(i + 2, 19).Value.ToString(),
                                Set2 = sheet.Cell(i + 2, 20).Value.ToString(),
                                Set3 = sheet.Cell(i + 2, 21).Value.ToString()
                            });

                            i++;
                        }
                    }

                    gamesRepo.UpdateGroupCyclesFromExcelImport(dto);

                    return Redirect(Request.UrlReferrer.ToString());

                }
                return Redirect(Request.UrlReferrer.ToString());
            }
            catch (Exception e)
            {
                return Redirect(Request.UrlReferrer.ToString());
            }
        }
    }
}
