using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

using Omu.ValueInjecter;
using AppModel;
using CmsApp.Models;
using DataService.LeagueRank;

namespace CmsApp.Controllers
{
    public class GroupsController : AdminController
    {
        // GET: Groups
        public ActionResult Index(int id, int seasonId)
        {
            var resList = stagesRepo.GetAll(id, seasonId);
            ViewBag.SeasonId = seasonId;

            return PartialView("_List", resList);
        }

        public ActionResult Delete(int id, int seasonId)
        {
            var gr = groupRepo.GetById(id);
            int leagId = gr.Stage.LeagueId;
            gr.IsArchive = true;
            groupRepo.Save();
            return RedirectToAction("Index", new { id = leagId, seasonId });
        }


        public ActionResult Create(int id, int seasonId)
        {
            var vm = new GroupsForm();
            vm.StageId = id;
            vm.NumberOfCycles = 1;
            var st = stagesRepo.GetById(id);
            vm.FirstStage = stagesRepo.CountStage(st.LeagueId) == 1;
            vm.LeagueId = st.LeagueId;
            UpdateGroupFormListsFromDB(vm);
            vm.PointId = 2;
            ViewBag.SeasonId = seasonId;
            vm.SeasonId = seasonId;
            return PartialView("_Edit", vm);
        }

        public ActionResult Edit(int id)
        {
            var vm = new GroupsForm();

            var gr = groupRepo.GetById(id);
            vm.InjectFrom(gr);
            if (gr.NumberOfCycles.HasValue)
            {
                vm.NumberOfCycles = gr.NumberOfCycles.Value;
            }
            else
            {
                vm.NumberOfCycles = 1;
            }

            vm.LeagueId = gr.Stage.LeagueId;


            UpdateGroupFormListsFromDB(vm);
            //vm.GroupsTeams = grRepo.GetTeamsGroups(vm.LeagueId);

            return PartialView("_Edit", vm);
        }

        private void UpdateGroupFormListsFromDB(GroupsForm vm)
        {
            var stageId = vm.StageId;
            var leagueTeams = teamRepo.GetTeamsByLeague(vm.LeagueId);

            var groupeTeams = groupRepo.GetTeamsGroups(vm.LeagueId)
                .Where(gt => gt.StageId == stageId)
                .OrderBy(gt => gt.Pos);

            var notIncludedTeams = leagueTeams.Where(t => !groupeTeams.Where(gt => gt.StageId == stageId)
                .Select(gt => gt.TeamId)
                .ToList()
                .Contains(t.TeamId))
                .ToList();

            var selectedTeams = new List<Team>();
            if (vm.GroupId != 0)
            {
                selectedTeams = groupRepo.GetGroupTeamsByGroupId(vm.GroupId).OrderBy(gt => gt.Pos).Select(gt => gt.Team).ToList();
            }
            vm.TeamsList = new SelectList(notIncludedTeams, "TeamId", "Title");
            vm.SelectedTeamsList = new SelectList(selectedTeams, "TeamId", "Title");
            vm.GamesTypes = new SelectList(groupRepo.GetGamesTypes(), "TypeId", "Name", vm.TypeId);
            var group = groupRepo.GetById(vm.GroupId);
            if (group != null)
                vm.PointId = group.PointEditType != null ? (int)group.PointEditType + 1 : 2;
        }

        private void UpdateGroupFormListsFromSelection(GroupsForm vm)
        {
            var notIncludedTeams = new List<Team>();
            var selectedTeams = new List<Team>();

            if (vm.TeamsArr != null)
            {
                notIncludedTeams = teamRepo.GetTeamsByIds(vm.TeamsArr);
            }


            if (vm.SelectedTeamsArr != null)
            {
                selectedTeams = teamRepo.GetTeamsByIds(vm.SelectedTeamsArr);
            }

            vm.TeamsList = new SelectList(notIncludedTeams, "TeamId", "Title");
            vm.SelectedTeamsList = new SelectList(selectedTeams, "TeamId", "Title");
            vm.GamesTypes = new SelectList(groupRepo.GetGamesTypes(), "TypeId", "Name", vm.TypeId);

            vm.GamesTypes = new SelectList(groupRepo.GetGamesTypes(), "TypeId", "Name", vm.TypeId);
        }

        [HttpPost]
        public ActionResult Edit(GroupsForm vm)
        {
            if (vm.TeamsArr != null || vm.SelectedTeamsArr != null)
            {
                UpdateGroupFormListsFromSelection(vm);
            }
            else
            {
                UpdateGroupFormListsFromDB(vm);
            }

            if (vm.SelectedTeamsArr == null || vm.SelectedTeamsArr.Count() == 0)
            {
                ModelState.AddModelError("SelectedTeamsArr", "נא להוסיף קבוצות לפני שמירה");
                return PartialView("_Edit", vm);
            }

            var item = new Group();
            if (vm.GroupId != 0)
                item = groupRepo.GetById(vm.GroupId);
            else
                groupRepo.Create(item);
            if (vm.PointId < 1 || vm.PointId > 3)
                vm.PointId = 0;

            item.PointEditType = vm.PointId - 1;
            TryUpdateModel(item);
            item.IsAdvanced = false;



            LeagueRankService svc = new LeagueRankService(vm.LeagueId);
            var teams = new List<RankTeam>();
            foreach (var team in vm.SelectedTeamsArr)
            {
                var res = svc.AddTeamIfNotExist(team, teams);
            }
            vm.Points = new int[teams.Count];
            vm.Names = new string[teams.Count];
            vm.IdTeams = new int?[teams.Count];
            for (var i = 0; i < teams.Count; i++)
            {
                vm.Points[i] = 0;
                var team = teamRepo.GetGroupTeam((int)item.GroupId, (int)teams[i].Id);
                if (team != null)
                    vm.Points[i] = team.Points!=null ? (int)team.Points:0;
                vm.Names[i] = teams[i].Title;
                vm.IdTeams[i] = teams[i].Id;
            }
            groupRepo.UpdateTeams(item, vm.SelectedTeamsArr);
            vm.InjectFrom(item);
            groupRepo.Save();
            vm.GroupId = item.GroupId;
            if (vm.PointId == 3)
            {
                TempData["GroupId"] = vm.GroupId;
                return PartialView("_EditPoints", vm);
            }
            ViewBag.SeasonId = vm.SeasonId;
            TempData["LeagueId"] = vm.LeagueId;
            return PartialView("_Edit", vm);
        }

        [HttpPost]
        public ActionResult EditPoints(GroupsForm vm)
        {
            for (var i = 0; i < vm.IdTeams.Count(); i++)
            {
                if (vm.IdTeams[i] == null || TempData["GroupId"] == null)
                    continue;
                var team = teamRepo.GetGroupTeam((int)TempData["GroupId"], (int)vm.IdTeams[i]);
                if (team != null)
                    team.Points = vm.Points[i];
            }
            teamRepo.Save();
            TempData["LeagueId"] = vm.LeagueId;
            return PartialView("_Edit", vm);
        }


    }
}