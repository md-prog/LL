using System.Web.Mvc;
using CmsApp.Models;
using AppModel;

namespace CmsApp.Controllers
{
    public class AuditoriumsController : AdminController
    {
        private int? currClubId = null;
        private bool isUnionClub = false;
        
        // GET: Auditoriums
        public ActionResult List(int? unionId, int? clubId, int? seasonId)
        {
            var vm = new AuditoriumForm
            {
                UnionId = unionId,
                ClubId = clubId,
                Auditoriums = clubId.HasValue ? auditoriumsRepo.GetByClubAndSeason(clubId.Value, seasonId) :
                                                auditoriumsRepo.GetByUnionAndSeason(unionId, seasonId.Value),
                SeasonId = seasonId
            };

            return PartialView("_List", vm);
        }

        [HttpPost]
        public ActionResult Create(AuditoriumForm frm)
        {
            Auditorium aud;
            if (frm.AuditoriumId > 0)
            {
                aud = auditoriumsRepo.GetById(frm.AuditoriumId);
                aud.Name = frm.Name;
                aud.Address = frm.Address;
            }
            else
            {
                aud = new Auditorium
                {
                    ClubId = frm.ClubId,
                    UnionId = isClubUnderUnion(frm.ClubId) ? null : frm.UnionId,
                    SeasonId = isClubUnderUnion(frm.ClubId) ? null : frm.SeasonId,
                    Name = frm.Name,
                    Address = frm.Address
                };
                auditoriumsRepo.Create(aud);
            }

            auditoriumsRepo.Save();

            TempData["SavedId"] = aud.AuditoriumId;

            return RedirectToAction("List", new { unionId = aud.UnionId, clubId = aud.ClubId, seasonId = frm.SeasonId });
        }

        private bool isClubUnderUnion(int? clubId)
        {
            if (!clubId.HasValue)
                return false;
            if (!currClubId.HasValue || currClubId.Value != clubId.Value)
            {
                currClubId = clubId;
                isUnionClub = clubsRepo.GetById(clubId.Value).IsUnionClub ?? true;
            }
            return isUnionClub;
        }

        public ActionResult Delete(int id)
        {
            var aud = auditoriumsRepo.GetById(id);
            aud.IsArchive = true;
            auditoriumsRepo.Save();

            return RedirectToAction("List", new { unionId = aud.UnionId, clubId = aud.ClubId, seasonId = aud.SeasonId });
        }

    }
}