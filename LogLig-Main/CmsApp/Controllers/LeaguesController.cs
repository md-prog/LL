using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using CmsApp.Models;
using AppModel;
using Resources;
using Omu.ValueInjecter;
using System.IO;
using MetascanHelper;
using CmsApp.Helpers;

namespace CmsApp.Controllers
{
    public class LeaguesController : AdminController
    {
        public ActionResult Edit(int id, int? seasonId)
        {
            bool isAdminsRole = User.IsInAnyRole(AppRole.Admins, AppRole.Editors);
            bool isNotValidWorker = (User.IsInRole(AppRole.Workers) && !AuthSvc.AuthorizeLeagueByIdAndManagerId(id, AdminId));

            if(isNotValidWorker)
            {
                return RedirectToAction("Index", "NotAuthorized");
            }
            var item = leagueRepo.GetById(id);

            if (item.IsArchive)
            {
                return RedirectToAction("NotFound", "Error");
            }


            var section = secRepo.GetByLeagueId(item.LeagueId); 
            var vm = new LeagueNavView
            {
                LeagueId = item.LeagueId,
                LeagueName = item.Name,
                UnionId = item.UnionId,
                ClubId = item.ClubId,
                SectionId = section.SectionId,
                SectionAlias = section.Alias,
                UnionName = item.Union == null ? string.Empty : item.Union.Name,
                ClubName = item.Club == null ? string.Empty : item.Club.Name,
                IsUnionValid = isAdminsRole,
                SeasonId = seasonId ?? item.SeasonId ?? getCurrentSeason().Id
            };

            Session["desOrder"] = false;
            if (!isAdminsRole)
            {
                if (vm.UnionId != null)
                    vm.IsUnionValid = AuthSvc.AuthorizeUnionByIdAndManagerId(vm.UnionId.Value, base.AdminId);
            }

            return View(vm);
        }

        [HttpPost]
        public ActionResult Create(LeagueCreateForm frm)
        {
            var item = new League();
            if (frm.Et != TournamentsPDF.EditType.LgUnion)
                item.EilatTournament = true;
            UpdateModel(item);

            leagueRepo.Create(item);
            leagueRepo.Save();

            return RedirectToAction("Edit", new { id = item.LeagueId, seasonId = frm.SeasonId });
        }

        public ActionResult CreatePDF(int id, TournamentsPDF.EditType et)
        {
            var vm = new LeagueCreateForm { UnionId = id, Et = et };
            vm.Ages = new SelectList(leagueRepo.GetAges(), "AgeId", "Title", vm.AgeId);
            vm.Genders = new SelectList(leagueRepo.GetGenders(), "GenderId", "TitleMany", vm.GenderId);

            return PartialView("_CreateForm", vm);
        }

        public ActionResult Create(int? unionId, int? clubId, TournamentsPDF.EditType et, int seasonId)
        {
            var vm = new LeagueCreateForm { UnionId = unionId, ClubId = clubId, Et = et, SeasonId = seasonId };
            vm.Ages = new SelectList(leagueRepo.GetAges(), "AgeId", "Title", vm.AgeId);
            vm.Genders = new SelectList(leagueRepo.GetGenders(), "GenderId", "TitleMany", vm.GenderId);

            return PartialView("_CreateForm", vm);
        }

        public ActionResult Details(int id)
        {
            var item = leagueRepo.GetById(id);

            var playersCount = item.LeagueTeams.Sum(x => x.Teams.TeamsPlayers.Count);

            var vm = new LeagueDetailsForm();

            vm.InjectFrom(item);

            vm.IsHadicapEnabled = item?.Union?.IsHadicapEnabled ?? false;
            vm.Ages = new SelectList(leagueRepo.GetAges(), "AgeId", "Title", vm.AgeId);
            vm.Genders = new SelectList(leagueRepo.GetGenders(), "GenderId", "TitleMany", vm.GenderId);

            var doc = leagueRepo.GetTermsDoc(id);
            if (doc != null)
            {
                vm.DocId = doc.DocId;
            }

            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            vm.PlayersCount = playersCount;

            vm.OfficialsCount = jobsRepo.CountOfficialsInLeague(id);
            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            return PartialView("_Details", vm);

        }

        [HttpPost]
        public ActionResult Details(LeagueDetailsForm frm)
        {
            int maxFileSize = GlobVars.MaxFileSize * 1000;
            var savePath = Server.MapPath(GlobVars.ContentPath + "/league/");

            var item = leagueRepo.GetById(frm.LeagueId);
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
                        if (!string.IsNullOrEmpty(item.Image))
                            FileUtil.DeleteFile(savePath + item.Image);

                        item.Image = newName;
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

            var docFile = GetPostedFile("DocFile");
            if (docFile != null)
            {
                if (docFile.ContentLength > maxFileSize)
                {
                    ModelState.AddModelError("DocFile", Messages.FileSizeError);
                }
                bool isValid = SaveDocument(docFile, frm.LeagueId);
                if (!isValid)
                {
                    ModelState.AddModelError("DocFile", Messages.FileError);
                }
            }

            if (ModelState.IsValid)
            {
                leagueRepo.Save();
                TempData["Saved"] = true;
            }
            else
            {
                TempData["ViewData"] = ViewData;
            }

            return RedirectToAction("Details", new { id = item.LeagueId });
        }

        public ActionResult DeleteImage(int leagueId, string image)
        {
            DataEntities db = new DataEntities();
            var item = db.Leagues.FirstOrDefault(x => x.LeagueId == leagueId);
            if(item == null || string.IsNullOrEmpty(image))
                return RedirectToAction("Edit", new { id = leagueId });
            if (image == "Image")
            {
                item.Image = null;
            }
            if(image == "Logo")
            {
                item.Logo = null;
            }
            db.SaveChanges();
            return RedirectToAction("Edit", new { id = leagueId, seasonId = item.SeasonId });
        }

        public ActionResult ShowDoc(int id)
        {
            var doc = leagueRepo.GetDocById(id);

            Response.AddHeader("content-disposition", "inline;filename=" + doc.FileName + ".pdf");

            return this.File(doc.DocFile, "application/pdf");
        }

        [NonAction]
        private bool SaveDocument(HttpPostedFileBase file, int leagueId)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".pdf")
            {
                return false;
            }

            var doc = leagueRepo.GetTermsDoc(leagueId);
            if (doc == null)
            {
                doc = new LeaguesDoc { LeagueId = leagueId };
                leagueRepo.CreateDoc(doc);
            }

            doc.FileName = file.FileName;

            byte[] docData;
            using (var reader = new BinaryReader(file.InputStream))
            {
                docData = reader.ReadBytes(file.ContentLength);
            }

            var req = new MetascanHelper.MetadataRequest(Guid.NewGuid().ToString(),
                file.FileName,
                docData,
                MetascanHelper.MetaScanAction.PostFileToMetaScan
                );

            string dataid = req.PostFileForScanning();
            MetaScanScanStatus metaScanStatus;
            req.CheckFileScan(dataid, out metaScanStatus);

            int tries = 3;
            while (metaScanStatus == MetaScanScanStatus.GeneralError && dataid != "" && tries > 0)
            {
                tries--;
                System.Threading.Thread.Sleep(2000);
                req.CheckFileScan(dataid, out metaScanStatus);
            }

            if (metaScanStatus == MetaScanScanStatus.Valid)
            {
                doc.DocFile = docData;
                leagueRepo.Save();
                return true;
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

        [NonAction]
        private string SaveFile(HttpPostedFileBase file, string name)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (!GlobVars.ValidImages.Contains(ext))
                return null;

            string newName = name + "_" + AppFunc.GetUniqName() + ext;

            var savePath = Server.MapPath(GlobVars.ContentPath + "/league/");

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
