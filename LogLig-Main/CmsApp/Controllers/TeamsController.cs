using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Resources;
using Omu.ValueInjecter;
using CmsApp.Models;
using DataService;
using AppModel;
using ClosedXML.Excel;
using CmsApp.Helpers;
using CmsApp.Models.Mappers;
using DataService.Services;
using DataService.Utils;
using DataService.DTO;

namespace CmsApp.Controllers
{
    public class TeamsController : AdminController
    {
        public ActionResult Edit(int id, int? seasonId, int? unionId)
        {
            if (User.IsInRole(AppRole.Workers) && !AuthSvc.AuthorizeTeamByIdAndManagerId(id, base.AdminId))
            {
                return RedirectToAction("Index", "NotAuthorized");
            }
            else
            {
                if (!seasonId.HasValue)
                {
                    seasonId = teamRepo.GetSeasonIdByTeamId(id);
                }
                var team = teamRepo.GetById(id, seasonId);

                if (team.IsArchive)
                {
                    return RedirectToAction("NotFound", "Error");
                }

                var userLeagues = AuthSvc.FindLeaguesByTeamAndManagerId(id, base.AdminId);

                var vm = new TeamNavView
                {
                    TeamId = id,
                    TeamName = team.Title,
                    IsValidUser = User.IsInAnyRole(AppRole.Admins, AppRole.Editors),
                    leagues = leagueRepo.GetByTeamAndSeasonShort(id, seasonId.Value),
                    UserLeagues = userLeagues.Select(ul => new LeagueShort { Id = ul.LeagueId, Name = ul.Name, UnionId = ul.UnionId, Check = true }).ToList(),
                    SeasonId = seasonId.Value,
                    clubs = clubsRepo.GetByTeamAndSeasonShort(id, seasonId.Value),
                    UnionId = unionId,
                    JobRole = usersRepo.GetTopLevelJob(AdminId)
                };

                return View(vm);
            }
        }

        public ActionResult Details(int id, int seasonId)
        {
            var team = teamRepo.GetTeamByTeamSeasonId(id, seasonId);

            var vm = new TeamInfoForm();
            vm.InjectFrom(team);

            vm.SeasonId = seasonId;
            vm.leagues = leagueRepo.GetByTeamAndSeasonShort(id, seasonId);
            vm.clubs = clubsRepo.GetByTeamAndSeasonShort(id, seasonId);

            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            return PartialView("_Details", vm);
        }

        public ActionResult TeamStandings(int teamId, int seasonId)
        {
            var tsm = new TeamStandingsModel();
            tsm.TeamId = teamId;
            tsm.SectionAlias = secRepo.GetSectionByTeamId(teamId).Alias;
            tsm.Leagues = leagueRepo.GetByTeamAndSeasonShort(teamId, seasonId);
            tsm.ClubStandings = new List<TeamStandingsGameForm>();

            var clubs = clubsRepo.GetByTeamAnsSeason(teamId, seasonId).Where(c => c.IsSectionClub.Value).ToList();

            foreach (var club in clubs)
            {
                var teamStandings = teamRepo.GetTeamStandings(club.ClubId, teamId);

                var clubStanding = new TeamStandingsGameForm();

                clubStanding.TeamStandings = teamStandings.ToViewModel();
                var teamStandingGame = teamRepo.GetTeamStandingGame(teamId, club.ClubId);

                clubStanding.TeamName = teamStandingGame?.ExternalTeamName;
                clubStanding.GamesUrl = teamStandingGame?.GamesUrl;
                clubStanding.TeamId = teamId;
                clubStanding.ClubId = club.ClubId;
                tsm.ClubStandings.Add(clubStanding);
            }

            return PartialView("TeamStandings", tsm);
        }

        [HttpPost]
        public ActionResult Details(TeamInfoForm frm)
        {
            var savePath = Server.MapPath(GlobVars.ContentPath + "/teams/");
            int maxFileSize = GlobVars.MaxFileSize * 1000;
            Team item;
            if (frm?.clubs?.Count > 0)
            {
                var team = clubsRepo.GetTeamClub(frm.clubs.First().Id, frm.TeamId, frm.SeasonId);
                item = team != null ? team.Team : new Team();
            }
            else if (frm?.leagues?.Count > 0)
            {
                item = teamRepo.GetLeagueTeam(frm.TeamId, frm.leagues.First().Id, frm.SeasonId);
            }
            else
            {
                item = teamRepo.GetById(frm.TeamId);
            }


            //  UpdateModel(item);

            var imageFile = GetPostedFile("ImageFile");
            if (imageFile != null)
            {
                if (imageFile.ContentLength > maxFileSize)
                {
                    ModelState.AddModelError("ImageFile", Messages.FileSizeError);
                }
                else
                {
                    var newName = SaveFile(imageFile, "img");
                    if (newName == null)
                    {
                        ModelState.AddModelError("ImageFile", Messages.FileError);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.PersonnelPic))
                            FileUtil.DeleteFile(savePath + item.PersonnelPic);

                        item.PersonnelPic = newName;
                    }
                }
            }

            var logoFile = GetPostedFile("LogoFile");
            if (logoFile != null)
            {
                if (logoFile.ContentLength > maxFileSize)
                {
                    ModelState.AddModelError("LogoFile", Messages.FileSizeError);
                }
                else
                {
                    var newName = SaveFile(logoFile, "logo");
                    if (newName == null)
                    {
                        ModelState.AddModelError("LogoFile", Messages.FileError);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.Logo))
                            FileUtil.DeleteFile(savePath + item.Logo);

                        item.Logo = newName;
                    }
                }
            }

            if (ModelState.IsValid)
            {
                teamRepo.UpdateTeamNameInSeason(item, frm.SeasonId, frm.Title);
                item.IsReserved = frm.IsReserved;
                item.IsUnderAdult = frm.IsUnderAdult;
                teamRepo.Save();
                TempData["Saved"] = true;
            }
            else
            {
                TempData["ViewData"] = ViewData;
            }

            return RedirectToAction("Details", new { id = item.TeamId, seasonId = frm.SeasonId });
        }

        public ActionResult DeleteImage(int leagueId, int teamId, int seasonId, string image)
        {
            DataEntities db = new DataEntities();
            var item = db.Teams.FirstOrDefault(x => x.TeamId == teamId);
            if (item == null || string.IsNullOrEmpty(image))
                return RedirectToAction("Edit", new { id = leagueId });
            if (image == "Image")
            {
                item.PersonnelPic = null;
            }
            if (image == "Logo")
            {
                item.Logo = null;
            }
            db.SaveChanges();
            return RedirectToAction("Edit", new { id = teamId, currentLeagueId = leagueId, seasonId });
        }

        public ActionResult List(int id, int seasonId, int? unionId)
        {
            var vm = new TeamForm
            {
                LeagueId = id,
                SeasonId = seasonId,
                UnionId = unionId,
                SectionId = leagueRepo.GetById(id).Union.SectionId
            };

            if (User.IsInAnyRole(AppRole.Workers))
            {
                switch (usersRepo.GetTopLevelJob(base.AdminId))
                {
                    case JobRole.UnionManager:
                        // vm.TeamsList = _teamsRepo.GetTeamsByLeague(id);
                        vm.TeamsList = teamRepo.GetTeams(seasonId, id);
                        break;
                    case JobRole.LeagueManager:
                        vm.TeamsList = teamRepo.GetTeams(seasonId, id);
                        break;
                    case JobRole.TeamManager:
                        vm.TeamsList = teamRepo.GetByManagerId(base.AdminId, seasonId);
                        break;
                }
            }
            else
            {
                vm.TeamsList = teamRepo.GetTeams(seasonId, id);
            }
            return PartialView("_List", vm);
        }

        [HttpPost]
        public ActionResult Create(TeamForm frm)
        {
            var team = new Team();

            if (frm.IsNew)
            {
                team.Title = frm.Title.Trim();

                // TODO: Update the LeagueTeams with correct SeasonId
                //team.SeasonId = frm.SeasonId;
                teamRepo.Create(team);
                teamRepo.AddTeamDetailToSeason(team, frm.SeasonId);
            }
            else if (frm.TeamId != 0 && !frm.IsNew)
            {
                team = teamRepo.GetById(frm.TeamId);
            }
            else
            {
                TempData["ErrExists"] = Messages.TeamNotFound;
                return RedirectToAction("List", new { id = frm.LeagueId, seasonId = frm.SeasonId });
            }

            var league = teamRepo.GetLeague(frm.LeagueId);
            var isExistsInLeague = league.LeagueTeams.Any(t => t.TeamId == team.TeamId);

            if (!isExistsInLeague)
            {
                var lt = new LeagueTeams
                {
                    TeamId = team.TeamId,
                    LeagueId = league.LeagueId,
                    SeasonId = frm.SeasonId
                };
                league.LeagueTeams.Add(lt);
                teamRepo.Save();
            }
            else
            {
                TempData["ErrExists"] = Messages.TeamExists;
            }

            return RedirectToAction("List", new { id = frm.LeagueId, seasonId = frm.SeasonId });
        }

        public ActionResult Delete(int id, int leagueId, int seasonId)
        {
            var league = teamRepo.GetLeague(leagueId);
            var teamToRemove = league.LeagueTeams.SingleOrDefault(lt => lt.TeamId == id && lt.SeasonId == seasonId && lt.LeagueId == leagueId);
            if (teamToRemove != null)
            {
                teamRepo.RemoveTeamDetails(teamToRemove.Teams, seasonId);
                league.LeagueTeams.Remove(teamToRemove);

                if (teamRepo.GetNumberOfLeaguesAndClubs(id) == 0)
                {
                    Team team = teamRepo.GetById(id);
                    team.IsArchive = true;
                }

                teamRepo.Save();
            }

            return RedirectToAction("List", new { id = leagueId, seasonId = seasonId });
        }

        [NonAction]
        private HttpPostedFileBase GetPostedFile(string name)
        {
            if (Request.Files[name] == null)
                return null;

            if (Request.Files[name].ContentLength == 0)
                return null;

            return Request.Files[name];
        }

        [NonAction]
        private string SaveFile(HttpPostedFileBase file, string name)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (!GlobVars.ValidImages.Contains(ext))
                return null;

            string newName = name + "_" + AppFunc.GetUniqName() + ext;

            var savePath = Server.MapPath(GlobVars.ContentPath + "/teams/");

            var di = new DirectoryInfo(savePath);
            if (!di.Exists)
                di.Create();

            // start security checking
            byte[] imgData;
            using (var reader = new BinaryReader(file.InputStream))
            {
                imgData = reader.ReadBytes(file.ContentLength);
            }


            System.IO.File.WriteAllBytes(savePath + newName, imgData);
            return newName;
        }

        [HttpPost]
        public ActionResult MoveToLeague(int[] teams, int leagueId, int seasonId)
        {
            var vm = new MoveTeamToLeagueViewModel();
            var currentUnionId = leagueRepo.GetById(leagueId).UnionId;

            if (currentUnionId.HasValue)
            {
                var leagues = leagueRepo.GetLeaguesForMoveByUnionSeasonId(currentUnionId.Value, seasonId, leagueId);
                vm.Leagues = leagues.Select(x => new SelectListItem
                {
                    Text = x.Name,
                    Value = x.LeagueId.ToString()
                }).ToList();

                if (leagues.Count > 0)
                {
                    vm.LeagueId = leagues.Last().LeagueId;
                }
            }
            vm.CurrentLeagueId = leagueId;
            vm.TeamIds = teams;
            vm.SeasonId = seasonId;
            return PartialView("_MoveTeamToLeaguePartial", vm);
        }

        [HttpPost]
        public ActionResult MoveTeams(MoveTeamToLeagueViewModel model)
        {
            teamRepo.MoveTeams(model.LeagueId, model.TeamIds, model.CurrentLeagueId, model.SeasonId);
            return Redirect(Request.UrlReferrer.ToString());
        }

        [HttpPost]
        public ActionResult Import(ImportFromExcelViewModel viewModel)
        {
            if (viewModel.ImportFile != null)
            {
                using (XLWorkbook workBook = new XLWorkbook(viewModel.ImportFile.InputStream))
                {
                    //Read the first Sheet from Excel file.
                    IXLWorksheet workSheet = workBook.Worksheet(1);
                    bool firstRow = true;

                    var games = new List<ExcelGameDto>();

                    var localCulture = System.Globalization.CultureInfo.CurrentCulture.ToString();
                    const int gameIdColumn = 1;
                    const int dateColumn = 5;
                    const int timeColumn = 6;

                    try
                    {
                        foreach (IXLRow row in workSheet.Rows())
                        {
                            if (firstRow)
                            {
                                firstRow = false;
                            }
                            else
                            {
                                int gameId;
                                DateTime date;
                                if (int.TryParse(row.Cell(gameIdColumn).Value.ToString(), out gameId) &&
                                    DateTime.TryParseExact(row.Cell(dateColumn).Value.ToString(), "dd/MM/yyyy",
                                        new CultureInfo(localCulture), new DateTimeStyles(), out date))
                                {
                                    games.Add(new ExcelGameDto
                                    {
                                        GameId = gameId,
                                        Date = date,
                                        Time = row.Cell(timeColumn).Value.ToString()
                                    });
                                }
                            }
                        }

                        gamesRepo.UpdateGamesDate(games);
                    }
                    catch (Exception ex)
                    {
                        // ignored
                    }
                }

            }
            if (viewModel.ClubId.HasValue)
            {
                return RedirectToAction("Edit",
                new { Id = viewModel.TeamId, clubId = viewModel.ClubId, seasonId = viewModel.SeasonId });
            }

            return RedirectToAction("Edit",
                new { Id = viewModel.TeamId, currentLeagueId = viewModel.CurrentLeagueId, seasonId = viewModel.SeasonId });
        }

        [HttpPost]
        public JsonResult SaveGamesUrl(int teamId, string url, string teamName, int clubId)
        {
            try
            {
                var service = new ScrapperService();

                var result = service.SchedulerScraper(url);

                service.Quit();

                var isTeamExist = teamRepo.GetById(teamId) != null;


                if (!isTeamExist && result.Any(t => t.HomeTeam != teamName || t.GuestTeam != teamName))
                {
                    return Json(new { Success = false, Error = "Such team doesn't exist" });
                }

                var scheduleId = gamesRepo.SaveTeamGameUrl(teamId, url, clubId, teamName);

                gamesRepo.UpdateGamesSchedulesFromDto(result, clubId, scheduleId, url);

                ProcessHelper.ClosePhantomJSProcess();

                return Json(new { Success = true, Data = result });
            }
            catch (Exception e)
            {
                ProcessHelper.ClosePhantomJSProcess();

                return Json(new { Success = false, Error = e.Message });
            }

        }

        [HttpPost]
        public JsonResult SaveTeamStandingGameUrl(int teamId, int clubId, string url, string teamName)
        {
            try
            {
                var service = new ScrapperService();
                var isTeamExist = teamRepo.GetById(teamId) != null;
                var result = service.StandingScraper(url);

                if (!isTeamExist && result.Any(t => t.Team != teamName))
                {
                    ProcessHelper.ClosePhantomJSProcess();
                    return Json(new { Success = false, Error = "Such team not exist." });
                }

                int standingGameId = teamRepo.SaveTeamStandingUrl(teamId, clubId, url, teamName);

                bool isSuccess = standingGameId > 0;

                if (isSuccess)
                {
                    gamesRepo.UpdateTeamStandingsFromModel(result, standingGameId, url);

                    service.Quit();
                }
                ProcessHelper.ClosePhantomJSProcess();
                return Json(new { Success = isSuccess, Data = result });
            }
            catch (Exception e)
            {

                return Json(new { Sucess = false, Error = e.Message });
            }

        }
    }

    public class ImportFromExcelViewModel
    {
        public int TeamId { get; set; }
        public int CurrentLeagueId { get; set; }
        public int SeasonId { get; set; }
        public HttpPostedFileBase ImportFile { get; set; }
        public int? ClubId { get; set; }
    }
}
