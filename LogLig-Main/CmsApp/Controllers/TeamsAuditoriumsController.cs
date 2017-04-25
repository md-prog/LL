using System.Web.Mvc;
using Resources;
using CmsApp.Models;
using DataService;
using AppModel;

namespace CmsApp.Controllers
{
    public class TeamsAuditoriumsController : AdminController
    {
        // GET: TeamsAuditoriums
        public ActionResult List(int id, int seasonId)
        {
            int unionId = teamRepo.GetTeamsUnion(id);

            var vm = new TeamsAuditoriumForm();
            vm.TeamId = id;
            vm.TeamAuditoriums = auditoriumsRepo.GetByTeam(id);
            vm.SeasonId = seasonId;
            vm.Auditoriums = new SelectList(auditoriumsRepo.GetByTeamAndSeason(id, seasonId), nameof(Auditorium.AuditoriumId), nameof(Auditorium.Name));

            if (TempData["ViewData"] != null)
            {
                ViewData = (ViewDataDictionary)TempData["ViewData"];
            }

            return PartialView("_List", vm);
        }

        [HttpPost]
        public ActionResult Add(TeamsAuditoriumForm frm, int seasonId)
        {
            bool isExists = auditoriumsRepo.IsExistsInTeam(frm.AuditoriumId, frm.TeamId);

            if (!isExists)
            {
                var item = new TeamsAuditorium();

                UpdateModel(item);

                auditoriumsRepo.AddToTeam(item);
                auditoriumsRepo.Save();
            }
            else
            {
                ModelState.AddModelError("AuditoriumId", Messages.AuditoriumAlreadyExsits);
                TempData["ViewData"] = ViewData;
            }

            return RedirectToAction("List", new { id = frm.TeamId, seasonId});
        }

        public ActionResult Delete(int id, int seasonId)
        {
            var item = auditoriumsRepo.GetAuditoriumById(id);
            auditoriumsRepo.RemoveFromTeam(item);
            auditoriumsRepo.Save();

            return RedirectToAction("List", new { id = item.TeamId, seasonId = seasonId });
        }
    }
}