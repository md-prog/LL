using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using CmsApp.Models;
using AppModel;
using DataService;
using Omu.ValueInjecter;
using System.IO;
using System.Threading;
using CmsApp.Helpers;
using CmsApp.Models.Mappers;
using MetascanHelper;
using Resources;
using DataService.DTO;

namespace CmsApp.Controllers
{
    public class PlayersController : AdminController
    {
        // GET: Players
        public ActionResult Index()
        {
            //var query = repo.GetQuery(false);
            return View();
        }

        public ActionResult Delete(int id)
        {
            var pl = usersRepo.GetById(id);
            pl.IsArchive = true;
            usersRepo.Save();

            return Redirect(Request.UrlReferrer.ToString());
        }

        public ActionResult Edit(int id = 0, int seasonId = 0, int leagueId = 0, int clubId = 0, int teamId = 0)
        {
            //
            Thread.CurrentThread.CurrentCulture = new CultureInfo("he-IL");
            var vm = new PlayerFormView
            {
                LeagueId = leagueId,
                Genders = new SelectList(usersRepo.GetGenders(), "GenderId", "Title")
            };

            if (id != 0)
            {
                vm.ClubId = clubId;
                vm.CurrentTeamId = teamId;
                vm.SeasonId = seasonId;

                if (leagueId > 0)
                {
                    League league = leagueRepo.GetById(leagueId);
                    if (league != null)
                    {
                        vm.IsHadicapEnabled = league.Union.IsHadicapEnabled;
                        vm.SeasonId = league.SeasonId ?? vm.SeasonId;
                    }
                }

                User pl = usersRepo.GetById(id);
                if (pl == null && pl.IsArchive)
                {
                    return RedirectToAction("NotFound", "Error");
                }


                if (teamId > 0)
                {
                    TeamsPlayer teamsPlayer = pl.TeamsPlayers
                                                .FirstOrDefault(x => x.UserId == pl.UserId &&
                                                                     x.SeasonId == vm.SeasonId &&
                                                                     x.TeamId == teamId);
                    if (teamsPlayer == null)
                    {
                        return RedirectToAction("NotFound", "Error");
                    }

                    vm.HandicapLevel = teamsPlayer.HandicapLevel;
                    vm.IsPlayereInTeamLessThan3year = teamsPlayer.IsPlayereInTeamLessThan3year;
                    vm.NumberOfTeamsUserPlays = pl.TeamsPlayers.Count;
                }

                vm.InjectFrom<IgnoreNulls>(pl);

                if (!string.IsNullOrEmpty(pl.Password))
                    vm.Password = Protector.Decrypt(pl.Password);
                vm.IsValidUser = User.IsInAnyRole(AppRole.Admins, AppRole.Editors);
                vm.ManagerTeams = new List<TeamDto>();
                vm.PlayerTeams = new List<TeamDto>();
                vm.PlayerHistories = new List<PlayerHistoryFormView>();

                var currDate = DateTime.Now;
                if (pl.UsersType.TypeRole == AppRole.Players)
                {
                    List<TeamDto> teams = pl.TeamsPlayers.Where(t => !t.Team.IsArchive 
                                && seasonsRepository.GetAllCurrent().Select(s => s.Id).Contains(t.SeasonId ?? 0))
                        .Select(t => new TeamDto
                        {
                            TeamId = t.TeamId,
                            Title = t.Team.Title,
                            SeasonId = t.SeasonId,
                            ClubId = t.Team.ClubTeams.Where(ct => ct.SeasonId == t.SeasonId).FirstOrDefault()?.ClubId ?? 0,
                            LeagueId = t.Team.LeagueTeams.Where(lt => lt.SeasonId == t.SeasonId).FirstOrDefault()?.LeagueId ?? 0
                        })
                        .ToList();
                    vm.PlayerTeams = teams;
                    if (!vm.IsValidUser)
                    {
                        var managerTeams = AuthSvc.FindTeamsByManagerId(base.AdminId)
                            .Select(t => new {
                                TeamId = t.TeamId,
                                Title = t.Title,
                                Club = teamRepo.GetClubByTeamId(t.TeamId, currDate),
                                League = teamRepo.GetLeagueByTeamId(t.TeamId, currDate)
                            })
                            .Select(t => new TeamDto
                        {
                            TeamId = t.TeamId,
                            Title = t.Title,
                            SeasonId = t.League.SeasonId ?? t.Club?.SeasonId,
                            ClubId = t.Club?.ClubId ?? 0,
                            LeagueId = t.League?.LeagueId ?? 0
                        });
                        vm.ManagerTeams = managerTeams;
                    }
                }

                vm.PlayerHistories = usersRepo.GetPlayerHistory(id, vm.SeasonId).ToViewModel();
            }

            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            return View(vm);
        }

        [HttpPost]
        public ActionResult Edit(PlayerFormView frm)
        {
            //
            Thread.CurrentThread.CurrentCulture = new CultureInfo("he-IL");
            int maxFileSize = GlobVars.MaxFileSize * 1000;
            var savePath = Server.MapPath(GlobVars.ContentPath + "/players/");

            if (!ModelState.IsValid)
            {
                if (frm.LeagueId != 0)
                {
                    return RedirectToAction("Edit",
                        new
                        {
                            id = frm.UserId,
                            seasonId = frm.SeasonId,
                            leagueId = frm.LeagueId,
                            teamId = frm.CurrentTeamId
                        });
                }
                return RedirectToAction("Edit", new { id = frm.UserId, seasonId = frm.SeasonId, clubId = 0, teamId = frm.CurrentTeamId });
            }

            var pl = new User();

            if (frm.UserId != 0)
            {
                pl = usersRepo.GetById(frm.UserId);

                TeamsPlayer teamsPlayer = pl.TeamsPlayers
                                            .FirstOrDefault(x => x.UserId == pl.UserId &&
                                                                 x.SeasonId == frm.SeasonId &&
                                                                 x.TeamId == frm.CurrentTeamId);
                if (teamsPlayer == null)
                {
                    return RedirectToAction("NotFound", "Error");
                }

                teamsPlayer.IsPlayereInTeamLessThan3year = frm.IsPlayereInTeamLessThan3year;
                teamsPlayer.HandicapLevel = frm.HandicapLevel;
            }
            else
            {
                usersRepo.Create(pl);
            }

            UpdateModel(pl);

            pl.Password = Protector.Encrypt(frm.Password);
            pl.TypeId = 4;

            usersRepo.Save();

            var imageFile = GetPostedFile("ImageFile");
            if (imageFile != null)
            {
                if (imageFile.ContentLength > maxFileSize)
                {
                    ModelState.AddModelError("ImageFile", Messages.FileSizeError);
                }
                else
                {
                    var newName = SaveFile(imageFile, pl.UserId);
                    if (newName == null)
                    {
                        ModelState.AddModelError("ImageFile", Messages.FileError);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(pl.Image))
                            FileUtil.DeleteFile(savePath + pl.Image);

                        pl.Image = newName;
                    }
                }
            }


            if (ModelState.IsValid)
            {
                usersRepo.Save();
                TempData["Saved"] = true;
            }
            else
            {
                TempData["ViewData"] = ViewData;
            }

            if (frm.LeagueId != 0)
            {
                return RedirectToAction("Edit", new { id = pl.UserId, seasonId = frm.SeasonId, leagueId = frm.LeagueId, teamId = frm.CurrentTeamId });
            }

            return RedirectToAction("Edit", new { id = pl.UserId, seasonId = frm.SeasonId, clubId = 0, teamId = frm.CurrentTeamId });
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
        private string SaveFile(HttpPostedFileBase file, int id)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (!GlobVars.ValidImages.Contains(ext))
            {
                return null;
            }

            string newName = id + "_" + AppFunc.GetUniqName() + ext;

            var savePath = Server.MapPath(GlobVars.ContentPath + "/players/");

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
    }
}