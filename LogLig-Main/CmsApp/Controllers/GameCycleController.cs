using AppModel;
using DataService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataService.Services;

namespace CmsApp.Controllers
{
    public class GameCycleController : AdminController
    {
        GamesService _gamesService = new GamesService();

        // GET: GameCycle/Edit/5
        public ActionResult Edit(int id, bool global = false)
        {
            var gc = gamesRepo.GetGameCycleById(id);
            if (global == true)
            {
                var league = leagueRepo.GetById(gc.Stage.LeagueId);
                Session["UnionId"] = league.UnionId;
                var checks = (bool[])Session["Checks"];
            }
            Session["global"] = global;

            var gameAlias = gc.Stage?.League?.Union?.Section?.Alias;

            switch (gameAlias)
            {
                case GamesAlias.WaterPolo:
                case GamesAlias.BasketBall:
                    return View("BasketBallWaterPolo/Edit", gc);
                default:
                    return View(gc);
            }

        }

        // POST: GameCycle/Edit/5
        [HttpPost]
        public ActionResult Edit(int id, GamesCycle gc)
        {
            return RedirectToAction("Edit", new { id = id });
        }

        // GET: GameCycle/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: GameCycle/Delete/5
        [HttpPost]
        public ActionResult Delete(int id, FormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction("Index");
            }
            catch
            {
                return View();
            }
        }

        public ActionResult StartGame(int id)
        {
            GamesCycle editGc = gamesRepo.StartGame(id);
            return RedirectToAction("Edit", new { id = id });
        }

        public ActionResult EndGame(int id)
        {
            GamesCycle editGc = gamesRepo.EndGame(id);
            return RedirectToAction("Edit", new { id = id });
        }

        public ActionResult UpdateGame(GamesCycle gc)
        {
            GamesCycle editGc = gamesRepo.GetGameCycleById(gc.CycleId);
            try
            {
                bool isChanged = false;
                if (gc.AuditoriumId != editGc.AuditoriumId)
                {
                    isChanged = true;
                    editGc.AuditoriumId = gc.AuditoriumId;
                }
                editGc.RefereeId = gc.RefereeId;
                if (!editGc.StartDate.Equals(gc.StartDate))
                {
                    isChanged = true;
                    editGc.StartDate = gc.StartDate;
                }

                gamesRepo.Update(editGc);
                TempData["SavedId"] = editGc.CycleId;

                if (isChanged && editGc.IsPublished)
                {
                    NotesMessagesRepo notesRep = new NotesMessagesRepo();
                    if (editGc.Stage != null && editGc.Stage.League != null && editGc.Stage.League.SeasonId != null)
                    {
                        String message = String.Format("Game details has been updated: {0} vs {1}", editGc.HomeTeam != null ? editGc.HomeTeam.Title : "", editGc.GuestTeam != null ? editGc.GuestTeam.Title : "");

                        if (editGc.HomeTeamId != null)
                        {
                            notesRep.SendToTeam((int)editGc.Stage.League.SeasonId, (int)editGc.HomeTeamId, message);
                        }
                        if (editGc.GuestTeamId != null)
                        {
                            notesRep.SendToTeam((int)editGc.Stage.League.SeasonId, (int)editGc.GuestTeamId, message);
                        }
                        if (gc.RefereeId != null)
                        {
                            notesRep.SendToUsers(new List<int> { (int)editGc.RefereeId }, message);
                        }
                    }

                    var notsServ = new GamesNotificationsService();
                    notsServ.SendPushToDevices(GlobVars.IsTest);
                }
            } catch( Exception e) { }
            return RedirectToAction("Game", editGc);
        }

        public ActionResult Game(GamesCycle gc)
        {
            GamesCycle editGc = gamesRepo.GetGameCycleById(gc.CycleId);

            var leagueId = editGc.Stage.LeagueId;
            var unionId = editGc.Stage.League.UnionId;
            var seasonId = editGc.Stage.League.SeasonId;
            var uRepo = new UsersRepo();
            if (unionId != null)
            {
                ViewBag.Referees = uRepo.GetUnionAndLeageReferees(unionId.Value, leagueId);

                var aRepo = new AuditoriumsRepo();
                if (seasonId.HasValue)
                {
                    //if season id has value get all arena's by season id
                    ViewBag.Auditoriums = aRepo.GetByUnionAndSeason(unionId.Value, seasonId.Value);
                }
                else
                {
                    ViewBag.Auditoriums = aRepo.GetAll(unionId.Value);
                }

            }
            return PartialView("_Game", editGc);
        }

        public ActionResult AddWaterPoloBasketBallSet(GameSet set)
        {
            gamesRepo.AddGameSet(set);
            gamesRepo.UpdateBasketBallWaterPoloScore(set.GameCycleId);
            return GameSetList(set.GameCycleId);
        }

        public ActionResult AddGameSet(GameSet set)
        {
            gamesRepo.AddGameSet(set);
            return GameSetList(set.GameCycleId);
        }

        public ActionResult UpdateGameSet(GameSet set)
        {
            gamesRepo.UpdateGameSet(set);
            return RedirectToAction("GameSetList", new { id = set.GameCycleId });
        }

        public PartialViewResult DeleteLastGameSet(int id)
        {
            GamesCycle gc = gamesRepo.GetGameCycleById(id);
            var lastSet = gc.GameSets.OrderBy(c => c.SetNumber).LastOrDefault();
            gamesRepo.DeleteSet(lastSet);
            return GameSetList(id);
        }

        public ActionResult UpdateGameResults(int id, bool isWaterpoloOrBasketball)
        {
            GamesCycle gc = gamesRepo.GetGameCycleById(id);
            if (gc.GameStatus != GameStatus.Ended || gc.Group.GamesType.TypeId == 1 /* Division */)
            {
                if (isWaterpoloOrBasketball)
                {
                    gamesRepo.UpdateBasketBallWaterPoloScore(id);
                }
                else
                {
                    gamesRepo.UpdateGameScore(gc);
                }
            } else
            {   //  If game status need to be reset after game has been ended
                //  then further games in knockout or playoff group need to be
                //  rescheduled
                gamesRepo.EndGame(gc);
            }
            return RedirectToAction("Edit", new { @id = id });
        }

        public PartialViewResult GameSetList(int id)
        {
            GamesCycle gc = gamesRepo.GetGameCycleById(id);
            List<GameSet> list = gc.GameSets.ToList();

            var alias = gc.Stage?.League?.Union?.Section?.Alias;
            switch (alias)
            {
                case GamesAlias.WaterPolo:
                case GamesAlias.BasketBall:
                    return PartialView("BasketBallWaterPolo/_GameSetList", list);
                default:
                    return PartialView("_GameSetList", list);
            }
        }

        public ActionResult TechnicalWin(int id, int teamId)
        {
            _gamesService.SetTechnicalWinForGame(id, teamId);

            return RedirectToAction("Edit", new { @id = id });
        }

        public ActionResult ResetGame(int id)
        {
            gamesRepo.ResetGame(id);
            return RedirectToAction("Edit", new { @id = id });
        }

        public ActionResult PotentialTeams(int id, int index)
        {
            BracketsRepo repo = new BracketsRepo();
            List<Team> list = repo.GetAllPotintialTeams(id, index);
            return PartialView("_PotentialTeams", list);
        }

    }
}
