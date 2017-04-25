using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataService;
using AppModel;
using CmsApp.Models;
using Omu.ValueInjecter;
using System.Web.UI.WebControls;
using System.IO;
using System.Web.UI;
using Resources;
using CmsApp.Helpers;
using MetascanHelper;

namespace CmsApp.Controllers
{
    public class UnionsController : AdminController
    {
        public ActionResult Edit(int id, int? seasonId = null)
        {
            var union = unionsRepo.GetById(id);

            if (union.IsArchive)
            {
                return RedirectToAction("NotFound", "Error");
            }

            if (User.IsInAnyRole(AppRole.Workers) && !AuthSvc.AuthorizeUnionByIdAndManagerId(id, AdminId))
            {
                return RedirectToAction("Index", "NotAuthorized");
            }

            var seasons = seasonsRepository.GetSeasonsByUnion(id).ToList();

            Session["CurrentUnionId"] = id;

            var model = new EditUnionForm
            {
                UnionId = id,
                UnionName = union.Name,
                SectionId = union.Section.SectionId,
                SectionName = union.Section.Name,
                IsCatchBall = union.Section.Alias == GamesAlias.NetBall,
                SeasonId = seasonId ?? GetUnionCurrentSeasonFromSession(),
                Seasons = seasons
            };            

            return View(model);
        }

        #region Info

        public ActionResult Details(int id)
        {
            var item = unionsRepo.GetById(id);

            var vm = new UnionDetailsForm();

            vm.InjectFrom(item);

            var doc = unionsRepo.GetTermsDoc(id);
            if (doc != null)
            {
                vm.DocId = doc.DocId;
            }

            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            return PartialView("_Details", vm);
        }

        [HttpPost]
        public ActionResult Details(UnionDetailsForm frm)
        {
            var maxFileSize = GlobVars.MaxFileSize * 1000;
            var savePath = Server.MapPath(GlobVars.ContentPath + "/league/");
            var item = unionsRepo.GetById(frm.UnionId);

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
                var isValid = SaveDocument(docFile, frm.UnionId);
                if (!isValid)
                {
                    ModelState.AddModelError("DocFile", Messages.FileError);
                }
            }

            if (ModelState.IsValid)
            {
                unionsRepo.Save();

                TempData["Saved"] = true;
            }
            else
            {
                TempData["ViewData"] = ViewData;
            }

            return RedirectToAction("Details", new { id = item.UnionId });
        }

        public ActionResult DeleteImage(int unionId, string image)
        {
            var item = unionsRepo.GetById(unionId);

            if (item == null || string.IsNullOrEmpty(image))
                return RedirectToAction("Edit", new { id = unionId });

            if (image == "PrimaryImage")
            {
                item.PrimaryImage = null;
            }

            if (image == "IndexImage")
            {
                item.IndexImage = null;
            }

            if (image == "Logo")
            {
                item.Logo = null;
            }
            unionsRepo.Save();

            return RedirectToAction("Edit", new { id = unionId });
        }

        public ActionResult ShowDoc(int id)
        {
            var doc = unionsRepo.GetDocById(id);

            Response.AddHeader("content-disposition", "inline;filename=" + doc.FileName + ".pdf");

            return this.File(doc.DocFile, "application/pdf");
        }

        [NonAction]
        private bool SaveDocument(HttpPostedFileBase file, int unionId)
        {
            string ext = Path.GetExtension(file.FileName).ToLower();

            if (ext != ".pdf")
            {
                return false;
            }

            var doc = unionsRepo.GetTermsDoc(unionId);
            if (doc == null)
            {
                doc = new UnionsDoc { UnionId = unionId };
                unionsRepo.CreateDoc(doc);
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
                unionsRepo.Save();
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

            var savePath = Server.MapPath(GlobVars.ContentPath + "/union/");

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

        #endregion

        public ActionResult List(int id)
        {
            var vm = new UnionsForm { SectionId = id };

            if (User.IsInAnyRole(AppRole.Workers))
            {
                switch (usersRepo.GetTopLevelJob(base.AdminId))
                {
                    case JobRole.UnionManager:
                        vm.UnionsList = new UnionsRepo().GetByManagerId(base.AdminId);
                        break;
                    case JobRole.LeagueManager:
                        break;
                    case JobRole.TeamManager:
                        break;
                }
            }
            else
                vm.UnionsList = unionsRepo.GetBySection(id);

            return PartialView("_List", vm);
        }

        public ActionResult Delete(int id)
        {
            var u = unionsRepo.GetById(id);

            bool isHasLeagues = u.Leagues.Any(t => t.IsArchive == false);
            if (isHasLeagues)
            {
                TempData["ErrId"] = id;
            }
            else
            {
                u.IsArchive = true;
                unionsRepo.Save();
            }

            return RedirectToAction("List", new { id = u.SectionId });
        }

        [HttpPost]
        public ActionResult Save(UnionsForm frm)
        {
            var u = new Union { SectionId = frm.SectionId, Name = frm.Name };
            unionsRepo.Create(u);
            unionsRepo.Save();

            return RedirectToAction("List", new { id = frm.SectionId });
        }

        [HttpPost]
        public ActionResult Update(int unionId, string name)
        {
            var u = unionsRepo.GetById(unionId);
            u.Name = name;
            unionsRepo.Save();

            TempData["SavedId"] = unionId;

            return RedirectToAction("List", new { id = u.SectionId });
        }

        public ActionResult Leagues(int id, int seasonId)
        {
            var lRepo = new LeagueRepo();
            var resList = new List<League>();

            if (User.IsInAnyRole(AppRole.Workers))
            {
                switch (usersRepo.GetTopLevelJob(base.AdminId))
                {
                    case JobRole.UnionManager:
                        resList = lRepo.GetByUnion(id, seasonId).Where(x => x.EilatTournament == null || ((bool)x.EilatTournament) == false).OrderBy(x => x.SortOrder).ToList();
                        break;
                    case JobRole.LeagueManager:
                        resList = lRepo.GetByManagerId(base.AdminId, seasonId).Where(x => x.EilatTournament == null || ((bool)x.EilatTournament) == false).OrderBy(x => x.SortOrder).ToList();
                        break;
                    case JobRole.TeamManager:
                        break;
                }
            }
            else
            {
                resList = lRepo.GetByUnion(id, seasonId).Where(x => x.EilatTournament == null || ((bool)x.EilatTournament) == false).OrderBy(x => x.SortOrder).ToList();
            }
            var result = new TournamentsPDF
            {
                UnionId = id,
                listLeagues = resList,
                SeasonId = seasonId,
                Et = TournamentsPDF.EditType.LgUnion
            };

            return PartialView("_Leagues", result);
        }

        [HttpPost]
        public ActionResult Leagues(TournamentsPDF model, HttpPostedFileBase PDF1_file, HttpPostedFileBase PDF2_file, HttpPostedFileBase PDF3_file, HttpPostedFileBase PDF4_file)
        {
            var routeToPDF = GlobVars.PdfRoute;
            HttpPostedFileBase[] pdfArr = new HttpPostedFileBase[] { PDF1_file, PDF2_file, PDF3_file, PDF4_file };
            for (int i = 0; i < pdfArr.Length; i++)
            {
                var fileFullName = $"{routeToPDF}PDF{i + 1}.pdf";
                if ((string.IsNullOrEmpty(model[i]) || pdfArr[i] != null) 
                    && System.IO.File.Exists(fileFullName))
                {
                    System.IO.File.Delete(fileFullName);
                }
                if (pdfArr[i] != null)
                {
                    pdfArr[i].SaveAs(fileFullName);
                }
            }
            return RedirectToAction("Edit", model.UnionId != null ? new { id = model.UnionId } : null);
        }

        public ActionResult ShowGlobalDoc(string name)
        {
            var url = GlobVars.PdfUrl + name.Split('/').Last();
            return Redirect(url);
        }

        public ActionResult EilatTournament(int? unionId, int? clubId, int seasonId)
        {
            var result = new TournamentsPDF();
            result.UnionId = unionId;
            result.Et = TournamentsPDF.EditType.TmntSectionClub;
            result.SeasonId = seasonId;
            result.ClubId = clubId;

            if (clubId.HasValue)
            {
                result.listLeagues = leagueRepo.GetByClub(clubId.Value, seasonId)
                    .Where(x => x.EilatTournament != null && ((bool)x.EilatTournament) == true)
                    .ToList();
                if (unionId.HasValue)
                {
                    result.Et = TournamentsPDF.EditType.TmntUnionClub;
                }
                else
                {
                    result.Et = TournamentsPDF.EditType.TmntSectionClub;
                }
            }
            else if (unionId.HasValue)
            {
                result.Et = TournamentsPDF.EditType.TmntUnion;
                if (User.IsInAnyRole(AppRole.Workers))
                {
                    switch (usersRepo.GetTopLevelJob(base.AdminId))
                    {
                        case JobRole.UnionManager:
                            result.listLeagues = leagueRepo.GetByUnion(unionId.Value, seasonId)
                                .Where(x => x.EilatTournament != null && ((bool) x.EilatTournament) == true)
                                .ToList();
                            break;
                        case JobRole.LeagueManager:
                            result.listLeagues = leagueRepo.GetByManagerId(base.AdminId, seasonId)
                                .Where(x => x.EilatTournament != null && ((bool) x.EilatTournament) == true)
                                .ToList();
                            break;
                        case JobRole.TeamManager:
                            break;
                    }
                }
                else
                {
                    result.listLeagues = leagueRepo.GetByUnion(unionId.Value, seasonId)
                        .Where(x => x.EilatTournament != null && ((bool) x.EilatTournament) == true)
                        .ToList();
                }

                var routeToPDF = GlobVars.PdfRoute;
                for (int i = 0; i < result.Count; i++)
                {
                    if (System.IO.File.Exists($"{routeToPDF}PDF{i + 1}.pdf"))
                    {
                        result[i] = $"PDF{i + 1}.pdf";
                    }
                }
            }

            return PartialView("_Leagues", result);
        }

        public ActionResult EilatClubTournament(int clubId)
        {
            return PartialView("_ClubLeagues");
        }

        public ActionResult DeleteLeagues(int id)
        {
            var lRepo = new LeagueRepo();
            var item = lRepo.GetById(id);

            bool isHasTeams = item.LeagueTeams.Any(t => t.Teams.IsArchive == false);
            if (isHasTeams)
            {
                TempData["ErrId"] = id;
            }
            else
            {
                item.IsArchive = true;
                lRepo.Save();
            }

            return RedirectToAction("Leagues", new { id = item.UnionId, seasonId = item.SeasonId });
        }

        public void ExportReferees(int id)
        {
            var repo = new GamesRepo();

            var grid = new GridView { DataSource = repo.GetRefereesExcel(id) };
            grid.DataBind();

            var rer = grid.RowHeaderColumn;
            Response.ClearContent();
            Response.AddHeader("content-disposition", "attachment; filename=ExportReferees.xls");
            Response.ContentType = "application/ms-excel";
            StringWriter sw = new StringWriter();
            HtmlTextWriter hw = new HtmlTextWriter(sw);
            grid.RenderControl(hw);
            Response.Write(sw.ToString());
            Response.End();
        }

        [HttpPost]
        public void ChangeOrder(int unionId, short[] ids, int seasonId)
        {
            short sortOrder = 0;
            List<League> resList = null;

            if (User.IsInAnyRole(AppRole.Workers))
            {
                switch (usersRepo.GetTopLevelJob(base.AdminId))
                {
                    case JobRole.UnionManager:
                        resList = leagueRepo.GetByUnion(unionId, seasonId).Where(x => x.EilatTournament == null || ((bool)x.EilatTournament) == false).OrderBy(x => x.SortOrder).ToList();
                        break;
                    case JobRole.LeagueManager:
                        resList = leagueRepo.GetByManagerId(base.AdminId, seasonId).Where(x => x.EilatTournament == null || ((bool)x.EilatTournament) == false).OrderBy(x => x.SortOrder).ToList();
                        break;
                    case JobRole.TeamManager:
                        break;
                }
            }
            else
            {
                resList = leagueRepo.GetByUnion(unionId, seasonId).Where(x => x.EilatTournament == null || ((bool)x.EilatTournament) == false).OrderBy(x => x.SortOrder).ToList();
            }

            if (resList != null && resList.Count > 0)
            {
                foreach (var id in ids)
                {
                    var firstOrDefault = resList.FirstOrDefault(x => x.LeagueId == id);
                    if (firstOrDefault != null)
                        firstOrDefault.SortOrder = sortOrder;

                    sortOrder++;

                }
                leagueRepo.Save();
            }
        }
    }
}