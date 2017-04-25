using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using DataService;
using DataService.DTO;
using CmsApp.Models;
using System;
using AppModel;

namespace CmsApp.Controllers
{
    public class SectionsController : AdminController
    {
        private const string leaguesKey = "Leagues";
        private const string auditoriumsKey = "Auditoriums";

        public ActionResult Index(int? langId)
        {
            ViewBag.LangId = new SelectList(secRepo.GetLanguages(), "LangId", "Name");
            var resList = secRepo.GetSections(langId);
            return View(resList);
        }

        [HttpPost]
        public ActionResult Update(int sectionId, string name)
        {
            var item = secRepo.GetById(sectionId);
            item.Name = name;
            secRepo.Save();
            TempData["SavedId"] = sectionId;

            return RedirectToAction("Index", new { langId = item.LangId });
        }

        public ActionResult Edit(int id)
        {
            var sect = secRepo.GetById(id);
            ViewBag.SectionName = sect.Name;

            SaveCurrentSectionAliasIntoSession(sect);
            return View();
        }

        public ActionResult List(Schedules model, int seasonId, int? idUnion = null)
        {
            if (model == null && idUnion != null)
                model = new Schedules()
                {
                    UnionId = (int)idUnion,
                };
            if (TempData.ContainsKey(leaguesKey) && model.Leagues == null)
            {
                model.Leagues = (LeagueShort[])TempData[leaguesKey];
            }
            if (TempData.ContainsKey(auditoriumsKey) && model.Auditoriums == null)
            {
                model.Auditoriums = (AuditoriumShort[])TempData[auditoriumsKey];
            }

            var res = new Schedules();
            res.UnionId = model.UnionId;
            res.SeasonId = seasonId;
            res.Games = new List<GamesInLeague>();
            var leagueShortList = leagueRepo.GetLeaguesFilterList(model.UnionId ?? 0, seasonId);
            if (model.Leagues != null)
            {
                foreach (var league in leagueShortList)
                {
                    if (model.Leagues[0].Check || model.Leagues
                            .Where(l => l.Id == league.Id).Select(l => l.Check).SingleOrDefault())
                        league.Check = true;
                }
            }
            //  Fill in auditoriums list
            var auditoriumShortList = auditoriumsRepo.GetAuditoriumsFilterList(model.UnionId ?? 0, seasonId);
            if (model.Auditoriums != null)
            {
                foreach (var aud in auditoriumShortList)
                {
                    if (model.Auditoriums[0].Check || model.Auditoriums
                            .Where(a => a.Id == aud.Id).Select(a => a.Check).SingleOrDefault())
                        aud.Check = true;
                }
            }

            //  Fill in games list
            var userIsEditor = User.IsInAnyRole(AppRole.Admins, AppRole.Editors, AppRole.Workers);
            var cond = new GamesRepo.GameFilterConditions
            {
                seasonId = seasonId,
                leagues = leagueShortList,
                auditoriums = auditoriumShortList
            };
            if (model.dateFilterType == Schedules.DateFilterPeriod.BeginningOfMonth)
            {
                cond.dateFrom = Schedules.FirstDayOfMonth;
                cond.dateTo = null;
            }
            else if (model.dateFilterType == Schedules.DateFilterPeriod.Ranged)
            {
                cond.dateFrom = model.dateFrom;
                cond.dateTo = model.dateTo;
            }
            else
            {
                cond.dateFrom = null;
                cond.dateTo = null;
            }
            var result = gamesRepo.GetCyclesByFilterConditions( cond, userIsEditor);
            var someLeaguesChecked = cond.leagues.Any(l => l.Check);
            var allLeaguesChecked = cond.leagues.All(l => l.Check);
            var someAuditoriumsChecked = cond.auditoriums.Any(a => a.Check);
            var allAuditoriumsChecked = cond.auditoriums.All(a => a.Check);
            var gamesSelected = result.Count() > 0;
            if (gamesSelected)
            {
                res.Games = result.Select(gc => new GamesInLeague(gc)
                {
                    LeagueId = gc.Stage.LeagueId,
                    LeagueName = gc.Stage.League.Name,
                    IsPublished = gc.IsPublished
                }).ToList();
                var uRepo = new UsersRepo();
                res.Referees = uRepo.GetUnionWorkers(model.UnionId ?? 0, "referee").ToArray();

                // If all games for selected leagues are published
                if (res.Games.All(gc => gc.IsPublished))
                    res.IsPublished = true;
                // If all games for selected leagues aren't published
                else if (res.Games.All(gc => !gc.IsPublished))
                    res.IsPublished = false;
                // If there are published and unpublished games for selected leagues
                else
                    res.IsPublished = null;
            }
            else
                res.IsPublished = false;

            res.Leagues = leagueShortList.ToArray();
            res.Auditoriums = auditoriumShortList.ToArray();
            res.dateFilterType = model?.dateFilterType ?? 0;
            res.dateFrom = model?.dateFrom ?? Schedules.FirstDayOfMonth;
            res.dateTo = model?.dateTo ?? Schedules.Tomorrow;
            int[] leagueIdArray;
            if (res.Leagues.Any(l => l.Check))
            {
                leagueIdArray = res.Leagues.Where(l => l.Id > 0 && l.Check).Select(l => l.Id).ToArray();
            } else
            {
                leagueIdArray = res.Games.Select(g => g.LeagueId).Distinct().ToArray();
            }
            res.teamsByGroups = teamRepo.GetGroupTeamsBySeasonAndLeagues(seasonId, leagueIdArray, 
                gamesSelected && ((allLeaguesChecked || !someLeaguesChecked) 
                               && (allAuditoriumsChecked || !someAuditoriumsChecked)));
            if (model != null)
            {
                res.Sort = model.Sort;

                if (res.Sort == 1)
                {
                    res.Games = res.Games.OrderBy(x => x.StartDate).ToList();
                }
                if (res.Sort == 2)
                {
                    res.Games = res.Games.OrderBy(x => x.Auditorium != null ? x.Auditorium.Name : "").ToList();
                }
                if (res.Sort == 0)
                {
                    res.Sort = Session["GamesSort"] == null ? 1 : (int)Session["GamesSort"];

                    if (res.Sort == 1)
                    {
                        res.Games = res.Games.OrderBy(x => x.StartDate).ToList();
                    }
                    if (res.Sort == 2)
                    {
                        res.Games = res.Games.OrderBy(x => x.Auditorium != null ? x.Auditorium.Name : "").ToList();
                    }
                }
            }
            Session["GamesSort"] = res.Sort;
            TempData[leaguesKey] = res.Leagues;
            TempData[auditoriumsKey] = res.Auditoriums;
            int? unionSection = null;
            if (idUnion.HasValue)
            {
                unionSection = idUnion.Value;
            }
            else if(res.UnionId.HasValue)
            {
                unionSection = res.UnionId.Value;
            }
            if (unionSection.HasValue)
            {
                var section = unionsRepo.GetSectionByUnionId(unionSection.Value);
                switch (section.Alias)
                {
                    case GamesAlias.WaterPolo:
                    case GamesAlias.BasketBall:
                        return PartialView("BasketBallWaterpolo/_List",res);

                    default:
                        return PartialView("_List", res);
                }
            }
            return PartialView("_List", res);
        }

        [HttpPost]
        public ActionResult Publish(int[] games, int seasonId, int unionId, bool isPublished)
        {
            if (games != null && games.Any())
            {
                var gameCycles = gamesRepo.GetGamesQuery().Where(g => games.Contains(g.CycleId)).ToList();
                gameCycles.ForEach(g => g.IsPublished = isPublished);
                gameCycles.ForEach(g => gamesRepo.Update(g));
            }

            return RedirectToAction("List", new { idUnion = unionId, seasonId });
        }

        public ActionResult Delete(int id, int seasonId)
        {
            var gc = gamesRepo.GetGameCycleById(id);
            int leagueId = gc.Stage.LeagueId;
            var league = leagueRepo.GetAll().FirstOrDefault(x => x.LeagueId == leagueId);
            gamesRepo.RemoveCycle(gc);
            gamesRepo.Save();
            var modelShedule = new Schedules()
            {
                UnionId = league.UnionId
            };
            return RedirectToAction("List", new { idUnion = modelShedule.UnionId, seasonId });
        }

        public ActionResult UpdateGame(GameCycleForm frm)
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

                if (isChanged && gc.IsPublished)
                {
                    NotesMessagesRepo notesRep = new NotesMessagesRepo();
                    if (gc.Stage != null && gc.Stage.League != null && gc.Stage.League.SeasonId != null)
                    {
                        String message = String.Format("Game details has been updated: {0} vs {1}", gc.HomeTeam != null ? gc.HomeTeam.Title : "", gc.GuestTeam != null ? gc.GuestTeam.Title : "");

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

        public ActionResult Toggle(int id, int unionId, int seasonId)
        {
            TeamShortDTO hTeam;
            TeamShortDTO gTeam;
            GamesCycle cycle;
            int homeTeamScore, guestTeamScore;
            var gameAlias = unionsRepo.GetById(unionId)?.Section?.Alias;
            try
            {
                gamesRepo.ToggleTeams(id);
                cycle = gamesRepo.GetGameCycleById(id);
                var homeTeam = teamRepo.GetById(cycle.HomeTeamId, seasonId);
                hTeam = new TeamShortDTO
                {
                    TeamId = homeTeam.TeamId,
                    Title = homeTeam.Title
                };
                var guestTeam = teamRepo.GetById(cycle.GuestTeamId, seasonId);
                gTeam = new TeamShortDTO
                {
                    TeamId = guestTeam.TeamId,
                    Title = guestTeam.Title
                };
                if (gameAlias == GamesAlias.BasketBall || gameAlias == GamesAlias.WaterPolo)
                {
                    homeTeamScore = cycle.GameSets.Sum(s => s.HomeTeamScore);
                    guestTeamScore = cycle.GameSets.Sum(s => s.GuestTeamScore);
                }
                else
                {
                    homeTeamScore = cycle.HomeTeamScore;
                    guestTeamScore = cycle.GuestTeamScore;
                }
            }
            catch (Exception e)
            {
                return Json(new { stat = "error", message = e.ToString() });
            }
            return Json(new { stat = "ok",
                id = id,
                homeTeam = hTeam,
                guestTeam = gTeam,
                homeTeamScore = homeTeamScore,
                guestTeamScore = guestTeamScore,
                arenaId = cycle.AuditoriumId
            });
        }

    }
}
