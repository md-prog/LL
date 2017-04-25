using System.Collections.Generic;
using System.Web.Mvc;

using CmsApp.Models;
using DataService;

namespace CmsApp.Controllers
{
    public class SearchController : AdminController
    {
        SearchService searchServ = new SearchService();
        TeamsRepo teamsRepo = new TeamsRepo();
        UsersRepo userRepo = new UsersRepo();

        // GET: Search
        public ActionResult Index()
        {
            var vm = new SearchViewModel();
            vm.IsSearchLeagueVisible = true;
            vm.IsSearchTeamVisible = true;
            vm.IsSearchPlayerVisible = true;

            if (User.IsInRole(AppRole.Workers))
            {
                var usersRepo = new UsersRepo();
                int userId = base.AdminId;
                var roleName = usersRepo.GetTopLevelJob(userId);
                switch (roleName)
                {
                    case JobRole.LeagueManager:
                        vm.IsSearchLeagueVisible = false;
                        break;
                    case JobRole.TeamManager:
                        vm.IsSearchLeagueVisible = false;
                        vm.IsSearchTeamVisible = false;
                        break;
                }
            }
            return View(vm);
        }

        [HttpPost]
        public ActionResult FindTeam(string term, int? sectionId)
        {
            IEnumerable<ListItemDto> items = new List<ListItemDto>();

            items = teamsRepo.FindByNameAndSection(term, sectionId, 999);

            return Json(items);
        }

        [HttpPost]
        public ActionResult FindLeague(string term)
        {
            IEnumerable<ListItemDto> items = new List<ListItemDto>();

            items = leagueRepo.FindByName(term, 999);

            return Json(items);
        }

        [HttpPost]
        public ActionResult FindPlayer(string term)
        {
            IEnumerable<ListItemDto> items = new List<ListItemDto>();

            items = userRepo.SearchUser(AppRole.Players, term, 999);

            return Json(items);
        }
    }
}