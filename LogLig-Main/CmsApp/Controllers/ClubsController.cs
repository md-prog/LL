using AppModel;
using CmsApp.Models;
using Omu.ValueInjecter;
using Resources;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsApp.Helpers;
using System.Collections.Generic;

namespace CmsApp.Controllers
{
    public class ClubsController : AdminController
    {
        // GET: Positions
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult ListBySection(int id)
        {
            var vm = new ClubsForm { SectionId = id };
            vm.Clubs = clubsRepo.GetBySection(id);
            return PartialView("_List", vm);
        }

        public ActionResult ListByUnion(int id, int seasonId)
        {
            var vm = new ClubsForm { UnionId = id, SeasonId = seasonId };
            vm.Clubs = clubsRepo.GetByUnion(id, seasonId);
            return PartialView("_List", vm);
        }

        [HttpPost]
        public ActionResult Save(ClubsForm item)
        {
            var pos = new Club();

            if (item.ClubId.HasValue)
            {
                pos = clubsRepo.GetById(item.ClubId.Value);
                UpdateModel(pos);
            }
            else
            {
                UpdateModel(pos);
                clubsRepo.Create(pos);
            }

            clubsRepo.Save();

            return RedirectToClubList(pos);
        }

        private ActionResult RedirectToClubList(Club pos)
        {
            if (pos.SectionId.HasValue)
            {
                return RedirectToAction(nameof(ListBySection), new { id = pos.SectionId });
            }
            else
            {
                return RedirectToAction(nameof(ListByUnion), new { id = pos.UnionId, seasonId = pos.SeasonId });
            }
        }

        public ActionResult Delete(int id)
        {
            var item = clubsRepo.GetById(id);
            item.IsArchive = true;

            clubsRepo.Save();

            return RedirectToClubList(item);
        }

        [HttpPost]
        public ActionResult Update(int clubId, string name)
        {
            var club = clubsRepo.GetById(clubId);
            club.Name = name;
            clubsRepo.Save();

            TempData["SavedId"] = clubId;

            return RedirectToClubList(club);
        }

        public ActionResult Edit(int id, int? seasonId, int? sectionId, int? unionId)
        {
            var club = clubsRepo.GetById(id);

            if (club.IsArchive)
            {
                return RedirectToAction("NotFound", "Error");
            }

            if (User.IsInAnyRole(AppRole.Workers))
            {
                return RedirectToAction("Index", "NotAuthorized");
            }

            var viewModel = new EditClubViewModel()
            {
                Id = id,
                Name = club.Name,
                SectionId = club.SectionId,
                SectionName = club?.Section?.Name,
                UnionId = club.UnionId,
                UnionName = club?.Union?.Name,
                SeasonId = seasonId,
                CurrentSeasonId = seasonId ?? seasonsRepository.GetLastSeasonByCurrentClub(club).Id,
                CurrentSeasonName = seasonId.HasValue ? seasonsRepository.GetById(seasonId.Value).Name : "",
                Seasons = club.IsSectionClub ?? true ? seasonsRepository.GetClubsSeasons(id) : new List<Season>()
            };

            return View(viewModel);
        }

        public ActionResult Details(int id)
        {
            var item = clubsRepo.GetById(id);

            var vm = new ClubDetailsForm();

            vm.InjectFrom(item);
            vm.SportCenterList = db.SportCenters.OrderBy(sc => sc.Id).ToList();
            vm.Culture = getCulture();

            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            return PartialView("_Details", vm);
        }

        [HttpPost]
        public ActionResult Details(ClubDetailsForm frm)
        {
            var maxFileSize = GlobVars.MaxFileSize * 1000;
            var savePath = Server.MapPath(GlobVars.ClubContentPath);
            var item = clubsRepo.GetById(frm.ClubId);

            UpdateModel(item);

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
                        if (!string.IsNullOrEmpty(item.PrimaryImage))
                            FileUtil.DeleteFile(savePath + item.PrimaryImage);

                        item.PrimaryImage = newName;
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

            var indexFile = GetPostedFile("IndexFile");
            if (indexFile != null)
            {
                if (indexFile.ContentLength > maxFileSize)
                {
                    ModelState.AddModelError("IndexFile", Messages.FileSizeError);
                }
                else
                {
                    var newName = SaveFile(indexFile, "img");
                    if (newName == null)
                    {
                        ModelState.AddModelError("IndexFile", Messages.FileError);
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(item.IndexImage))
                            FileUtil.DeleteFile(savePath + item.IndexImage);

                        item.IndexImage = newName;
                    }
                }
            }

            var docFile = GetPostedFile("DocFile");
            if (docFile != null)
            {
                if (docFile.ContentLength > maxFileSize)
                {
                    ModelState.AddModelError("DocFile", Messages.FileSizeError);
                }
                var isValid = SaveDocument(docFile, frm.ClubId);
                if (!isValid)
                {
                    ModelState.AddModelError("DocFile", Messages.FileError);
                }
            }

            if (ModelState.IsValid)
            {
                clubsRepo.Save();

                TempData["Saved"] = true;
            }
            else
            {
                TempData["ViewData"] = ViewData;
            }

            return RedirectToAction("Details", new { id = item.ClubId });
        }

        [NonAction]
        private bool SaveDocument(HttpPostedFileBase file, int unionId)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".pdf")
            {
                return false;
            }

            return false;
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

        public ActionResult MainTab(int id)
        {
            var item = clubsRepo.GetById(id);

            var playersCount = item.ClubTeams.Sum(x => x.Team.TeamsPlayers.Count);

            var vm = new ClubMainTabForm();

            vm.PlayersCount = playersCount;

            vm.OfficialsCount = jobsRepo.CountOfficialsInClub(id);
            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            return PartialView("_MainTab", vm);
        }
        public ActionResult ClubTeams(int clubId, int seasonId)
        {
            if (User.IsInAnyRole(AppRole.Workers))
            {
                return RedirectToAction("Index", "NotAuthorized");
            }
            var teams = teamRepo.GetTeamsByClubSeasonId(clubId, seasonId);
            var club = clubsRepo.GetClubById(clubId);
            var model = new ClubTeamsForm
            {
                ClubId = clubId,
                Teams = teams,
                SeasonId = seasonId,
                CurrentSeasonId = seasonId,
                SectionId = club.IsSectionClub.Value ? club.SectionId.Value : club.Union.SectionId
            };
            return PartialView("_ClubTeams", model);
        }

        [HttpPost]
        public ActionResult CreateClubTeam(ClubTeamsForm model)
        {
            var team = new Team();

            if (model.IsNew)
            {
                team.Title = model.TeamName.Trim();
                teamRepo.Create(team);
            }

            else if (model.TeamId != 0 && !model.IsNew)
            {
                team = teamRepo.GetById(model.TeamId, model.SeasonId);
            }
            else
            {
                TempData["ErrExist"] = Messages.TeamNotFound;
                return RedirectToAction(nameof(ClubTeams), new { clubId = model.ClubId, seasonId = model.CurrentSeasonId, sectionId = model.SectionId });
            }

            var clubTeam = new ClubTeam
            {
                ClubId = model.ClubId,
                TeamId = team.TeamId,
                SeasonId = model.CurrentSeasonId
            };
            var isExistClubTeam = clubsRepo.IsExistClubTeamForCurrentSeason(clubTeam.ClubId, clubTeam.TeamId, clubTeam.SeasonId);
            if (isExistClubTeam)
            {
                TempData["ErrExist"] = Messages.TeamExists;
                return RedirectToAction(nameof(ClubTeams), new { clubId = clubTeam.ClubId, seasonId = model.CurrentSeasonId, sectionId = model.SectionId });
            }
            clubsRepo.CreateTeamClub(clubTeam);

            clubsRepo.Save();

            return RedirectToAction(nameof(ClubTeams), new { clubId = clubTeam.ClubId, seasonId = model.CurrentSeasonId, sectionId = model.SectionId });
        }

        public ActionResult DeleteTemClub(int clubId, int teamId, int seasonId, int sectionId)
        {
            var clubTeam = clubsRepo.GetTeamClub(clubId, teamId, seasonId);
            if (clubTeam != null)
            {
                clubsRepo.RemoveTemClub(clubTeam);
                clubsRepo.Save();

                if (teamRepo.GetNumberOfLeaguesAndClubs(teamId) == 0)
                {
                    Team team = teamRepo.GetById(teamId);
                    team.IsArchive = true;
                }

                teamRepo.Save();
            }

            return RedirectToAction("ClubTeams", new { clubId, seasonId, sectionId });
        }

        public ActionResult EditTeam(int teamId, int clubId, int seasonId, int sectionId)
        {
            if (User.IsInRole(AppRole.Workers) && AuthSvc.AuthorizeTeamByIdAndManagerId(teamId, base.AdminId))
                return RedirectToAction("Index", "NotAuthorized");
            else
            {
                var team = teamRepo.GetById(teamId);
                if (team.IsArchive)
                {
                    return RedirectToAction("NotFound", "Error");
                }
                var vm = new TeamNavView
                {
                    SeasonId = seasonId,
                    TeamName = team.Title,
                    IsValidUser = User.IsInAnyRole(AppRole.Admins, AppRole.Editors),
                    TeamId = team.TeamId,
                    leagues = leagueRepo.GetByTeamAndSeasonShort(teamId, seasonId),
                    clubs = clubsRepo.GetByTeamAndSeasonShort(teamId, seasonId),
                    SectionId = sectionId
                };
                ViewBag.ClubId = clubId;
                return View("EditTeamClub", vm);
            }
        }

        [NonAction]
        private string SaveFile(HttpPostedFileBase file, string name)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (!GlobVars.ValidImages.Contains(ext))
                return null;

            string newName = name + "_" + AppFunc.GetUniqName() + ext;

            var savePath = Server.MapPath(GlobVars.ClubContentPath);

            var di = new DirectoryInfo(savePath);
            if (!di.Exists)
                di.Create();

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