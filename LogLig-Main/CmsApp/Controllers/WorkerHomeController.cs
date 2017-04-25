using CmsApp.Models;
using DataService;
using Resources;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DataService.DTO;

namespace CmsApp.Controllers
{
    public class WorkerHomeController : AdminController
    {
        // GET: WorkerHome
        public ActionResult Index()
        {
            UsersRepo usersRepo = new UsersRepo();
            int userId = base.AdminId;
            var roleName = usersRepo.GetTopLevelJob(userId);

            List<ManagedItemViewModel> tree = new List<ManagedItemViewModel>();

            switch (roleName)
            {
                case JobRole.UnionManager:
                    ViewBag.Title = Messages.Unions;
                    var unions = new UnionsRepo().GetByManagerId(userId);
                    if (unions.Count == 1)
                    {
                        var seasons = unions.First().Seasons;
                        var season = 1;
                        if (seasons != null && seasons.Count > 0)
                        {
                            season = seasons.Last().Id;
                        }
                        return RedirectToAction("Edit", "Unions", new { id = unions.First().UnionId, seasonId = season });
                    }
                    foreach (var item in unions)
                    {
                        ManagedItemViewModel vm = new ManagedItemViewModel();
                        vm.Id = item.UnionId;
                        vm.Name = item.Name;
                        vm.Controller = "Unions";
                        vm.SeasonId = item.Seasons.Last().Id;
                        tree.Add(vm);
                    }
                    break;
                case JobRole.LeagueManager:
                    ViewBag.Title = Messages.Leagues;
                    var leagues = new LeagueRepo().GetByManagerId(userId, null);
                    if (leagues.Count == 1)
                    {
                        return RedirectToAction("Edit", "Leagues", new { id = leagues.First().LeagueId, seasonId = leagues.First().SeasonId });
                    }

                    foreach (var item in leagues)
                    {
                        ManagedItemViewModel vm = new ManagedItemViewModel();
                        vm.Id = item.LeagueId;
                        vm.Name = item.Name;
                        vm.SeasonId = item.SeasonId;
                        vm.Controller = "Leagues";
                        tree.Add(vm);
                    }
                    break;
                case JobRole.TeamManager:
                    ViewBag.Title = Messages.Teams;
                    List<TeamManagerTeamInformationDto> teamManagers = new TeamsRepo().GetByTeamManagerId(userId);
                    if (teamManagers.Count == 1)
                    {
                        var teamManager = teamManagers[0];

                        if (teamManager.LeagueId != null)
                        {
                            return RedirectToAction("Edit", "Teams", new { id = teamManager.TeamId, currentLeagueId = teamManager.LeagueId, seasonId = teamManager.SeasonId, unionId = teamManager.UnionId });
                        }

                        if (teamManager.ClubId != null)
                        {
                            return RedirectToAction("Edit", "Teams", new { id = teamManager.TeamId, seasonId = teamManager.SeasonId, unionId = teamManager.UnionId, clubId = teamManager.ClubId.Value });
                        }
                    }

                    var items = (from teamManager in teamManagers
                                 let teamId = teamManager.TeamId
                                 where teamId != null
                                 select new ManagedItemViewModel
                                 {
                                     Id = teamId.Value,
                                     Name = teamManager.Title,
                                     LeagueName = teamManager.LeagueName,
                                     Controller = "Teams",
                                     SeasonId = teamManager.SeasonId,
                                     LeagueId = teamManager.LeagueId,
                                     UnionId = teamManager.UnionId,
                                     ClubId = teamManager.ClubId
                                 });
                    tree.AddRange(items);

                    break;
            }
            return View(tree);
        }
    }
}