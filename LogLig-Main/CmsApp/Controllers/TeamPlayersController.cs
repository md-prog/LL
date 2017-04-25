using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using Resources;

using CmsApp.Models;
using AppModel;
using Omu.ValueInjecter;
using CmsApp.Helpers;
using DataService.DTO;
using System.Net;
using System;

namespace CmsApp.Controllers
{
    public class TeamPlayersController : AdminController
    {
        public ActionResult Edit(int id, int seasonId)
        {
            var vm = new TeamPlayerForm();
            vm.IsActive = true;
            vm.TeamId = id;
            vm.SeasonId = seasonId;
            vm.Positions = new SelectList(posRepo.GetByTeam(id), "PosId", "Title");

            return PartialView("_Edit", vm);
        }

        [HttpPost]
        public ActionResult Edit(TeamPlayerForm frm)
        {
            var user = playersRepo.GetUserByIdentNum(frm.IdentNum);
            if (user == null)
            {
                ModelState.AddModelError("IdentNum", Messages.PlayerNotExists);
            }
            else
            {
                var tu = playersRepo.GetTeamPlayer(frm.TeamId, user.UserId, frm.PosId);
                if (tu != null)
                {
                    ModelState.AddModelError("PosId", Messages.PlayerAlreadyOnThisPosition);
                }
                if (playersRepo.ShirtNumberExists(frm.TeamId, frm.ShirtNum))
                {
                    ModelState.AddModelError("ShirtNum", Messages.ShirtAlreadyExists);
                }
            }

            if (!ModelState.IsValid)
            {
                frm.Positions = new SelectList(posRepo.GetByTeam(frm.TeamId), "PosId", "Title");
                return PartialView("_Edit", frm);
            }

            var item = new TeamsPlayer();
            item.InjectFrom(frm);
            item.UserId = user.UserId;
            playersRepo.AddToTeam(item);
            playersRepo.Save();

            return RedirectToAction("Edit", new { id = frm.TeamId });
        }


        public ActionResult List(int id, int seasonId, int? leagueId = null, int? clubId = null)
        {
            var seasonIdReal = seasonId;
            if (clubId.HasValue)
            {
                var club = clubsRepo.GetById(clubId.Value);
                if (club.IsSectionClub ?? true)
                {
                    seasonIdReal = teamRepo.GetSeasonIdByTeamId(id, DateTime.Now) ?? seasonId;
                }
            }
            ViewBag.Positions = posRepo.GetByTeam(id);
            ViewBag.TeamId = id;
            ViewBag.SeasonId = seasonIdReal;
            ViewBag.LeagueId = leagueId;
            ViewBag.ClubId = clubId;
            var resList = playersRepo.GetTeamPlayers(id, seasonIdReal);
            ViewBag.JobRole = usersRepo.GetTopLevelJob(AdminId);
            return PartialView("_List", resList);
        }

        public ActionResult Delete(int id, int? seasonId)
        {
            var p = playersRepo.GetTeamPlyaerBySeasonId(id, seasonId);
            playersRepo.ReoveFromTeam(p);
            playersRepo.Save();

            return RedirectToAction("List", new { id = p.TeamId, seasonId });
        }

        [HttpPost]
        public ActionResult Update(int shirtNum, int? posId, int updateId, bool? isActive, int teamId, int seasonId)
        {
            if (playersRepo.ShirtNumberExists(teamId, shirtNum, updateId, seasonId))
            {
                HttpContext.Response.StatusCode = (int)HttpStatusCode.Ambiguous;
                var oldNum = playersRepo.GetTeamsPlayerById(updateId).ShirtNum;
                return Json(new { statusText = Messages.ShirtAlreadyExists, Id = updateId, oldShirtNum = oldNum }, JsonRequestBehavior.DenyGet);
            }

            var p = playersRepo.GetTeamsPlayerById(updateId);
            p.ShirtNum = shirtNum;
            p.PosId = posId;
            p.IsActive = isActive == true;
            playersRepo.Save();
            TempData["SavedId"] = updateId;
            return RedirectToAction("List", new { id = teamId, seasonId });
        }

        [HttpPost]
        public ActionResult Search(string term, int teamId)
        {
            int sectionId = secRepo.GetSectionByTeamId(teamId).SectionId;

            var resList = usersRepo.SearchUserByIdent(sectionId, AppRole.Players, term, 8);

            return Json(resList);
        }

        public ActionResult CreatePlayer(int? leagueId, int teamId, int seasonId)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("he-IL");
            var vm = new TeamPlayerForm
            {
                Genders = new SelectList(playersRepo.GetGenders(), "GenderId", "Title"),
                IsActive = true,
                TeamId = teamId,
                SeasonId = seasonId,
                Positions = new SelectList(posRepo.GetByTeam(teamId), "PosId", "Title")
            };

            if (leagueId != null)
            {
                League league = leagueRepo.GetById(leagueId.Value);
                if (league != null)
                {
                    vm.IsHadicapEnabled = league.Union.IsHadicapEnabled;
                }
            }

            return PartialView("_EditPlayer", vm);
        }

        public ActionResult MovePlayerToTeam(int teamId, int? leagueId, int seasonId, int? clubId, int? unionId)
        {
            var vm = new MovePlayerForm();

            if (User.IsInAnyRole(AppRole.Workers))
            {
                switch (usersRepo.GetTopLevelJob(AdminId))
                {
                    case JobRole.LeagueManager:
                        //League manager is able to move players/teams at the league that he is managing.
                        //except current team. No sense move player to team where he is presented at current time.
                        var teamsInLeague = leagueRepo.GetTeamsByManager(AdminId, teamId, seasonId, unionId);
                        vm.Teams = teamsInLeague.Select(x=> new TeamDto {TeamId = x.TeamId, Title = x.Title}).ToList();
                        break;
                    //Association(Union) manager is capable to move players/teams at the level of all leagues / association.
                    case JobRole.UnionManager:
                        var teamsAllLeaguesAssociation = teamRepo.GetAllExceptCurrent(teamId, seasonId, unionId);
                        vm.Teams = teamsAllLeaguesAssociation;
                        break;
                }
            }
            else if (User.IsInAnyRole(AppRole.Admins))
            {
                if (clubId.HasValue)
                {
                    vm.Teams = teamRepo.GetTeamsByClubSeasonId(clubId.Value, seasonId, unionId).Where(x=>x.TeamId != teamId).Select(x => new TeamDto { TeamId = x.TeamId, Title = x.Title}).ToList();
                }
                else
                {
                    vm.Teams = teamRepo.GetAllExceptCurrent(teamId, seasonId, unionId);
                }
                
            }
            vm.CurrentTeamId = teamId;
            vm.CurrentLeagueId = leagueId;
            vm.SeasonId = seasonId;
            vm.ClubId = clubId;
            return PartialView("_MovePlayerToTeam", vm);
        }

        [HttpPost]
        public ActionResult MovePlayerToTeam(MovePlayerForm frm)
        {
            playersRepo.MovePlayersToTeam(frm.TeamId, frm.Players, frm.CurrentTeamId, frm.SeasonId);
            return RedirectToAction("List", new {id = frm.CurrentTeamId, seasonId = frm.SeasonId });

        }

        [HttpPost]
        public ActionResult CreatePlayer(TeamPlayerForm frm)
        {
            //Thread.CurrentThread.CurrentCulture = new CultureInfo("he-IL");
            var user = playersRepo.GetUserByIdentNum(frm.IdentNum);
            frm.Genders = new SelectList(playersRepo.GetGenders(), "GenderId", "Title");
            frm.Positions = new SelectList(posRepo.GetByTeam(frm.TeamId), "PosId", "Title");

            if (playersRepo.ShirtNumberExists(frm.TeamId, frm.ShirtNum))
            {
                //Shirt number taken
                ModelState.AddModelError("ShirtNum", Messages.ShirtAlreadyExists);
            }

            if (user != null)
            {
                if (playersRepo.PlayerExistsInTeam(frm.TeamId, user.UserId))
                {
                    //Player exists in team
                    ModelState.AddModelError("IdentNum", Messages.PlayerAlreadyInTeam);
                }
            }

            if (!ModelState.IsValid)
            {
                return PartialView("_EditPlayer", frm);
            }


            if (user == null)
            {
                //New User
                user = new User();
                UpdateModel(user);
                user.Password = Protector.Encrypt(frm.IdentNum);
                user.TypeId = 4; // player
                usersRepo.Create(user);
            }

            var tp = new TeamsPlayer();
            UpdateModel(tp);
            tp.UserId = user.UserId;
            tp.SeasonId = frm.SeasonId;
            playersRepo.AddToTeam(tp);
            playersRepo.Save();
            TempData["Success"] = true;
            return PartialView("_EditPlayer", frm);
        }


        [HttpPost]
        public ActionResult ExistingPlayer(TeamPlayerForm frm)
        {
            var user = playersRepo.GetUserByIdentNum(frm.IdentNum);
            var tp = playersRepo.GetTeamPlayerByIdentNumAndTeamId(frm.IdentNum, frm.TeamId);
            frm.Genders = new SelectList(playersRepo.GetGenders(), "GenderId", "Title");
            frm.Positions = new SelectList(posRepo.GetByTeam(frm.TeamId), "PosId", "Title");
            if (user == null)
            {
                ModelState.AddModelError("IdentNum", Messages.PlayerNotExists);
                return PartialView("_EditPlayerFormBody", frm);
            }


            frm.InjectFrom<NullableInjection>(user);
            if (tp != null)
            {
                frm.InjectFrom<NullableInjection>(tp);
            }
            return PartialView("_EditPlayerFormBody", frm);
        }

        [HttpPost]
        public JsonResult CreateTeam(int? leagueId, int? clubId, int seasonId, TeamDto model)
        {
            var team = new Team {Title = model.Title};

            teamRepo.Create(team);
            if (leagueId.HasValue)
            {
                var league = leagueRepo.GetByLeagueSeasonId(leagueId.Value, seasonId);
                var leagueTeam = new LeagueTeams
                {
                    TeamId = team.TeamId,
                    LeagueId = leagueId.Value,
                    SeasonId = seasonId
                };

                league.LeagueTeams.Add(leagueTeam);
                leagueRepo.Save();
            }
            else if(clubId.HasValue)
            {
                var club = clubsRepo.GetById(clubId.Value);
                if (club != null)
                {
                    club.ClubTeams.Add(new ClubTeam
                    {
                        ClubId = clubId.Value,
                        SeasonId = seasonId,
                        TeamId = team.TeamId
                    });

                    clubsRepo.Save();
                }
            }
            
            var data = new TeamDto
            {
                Title = team.Title,
                TeamId = team.TeamId
            };
            return Json(data, JsonRequestBehavior.AllowGet);
        }
    }
}