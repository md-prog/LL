using AppModel;
using CmsApp.Models;
using CmsApp.Models.Mappers;
using System;
using System.Net;
using System.Linq;
using System.Web.Mvc;

namespace CmsApp.Controllers
{
    public class EventsController : AdminController
    {
        public ActionResult Index(int? leagueId, int? clubId)
        {
            return PartialView("List", new EventModel { LeagueId = leagueId, ClubId = clubId });
        }

        // GET: Events
        public ActionResult List(int? leagueId, int? clubId)
        {
            EventModel ev = new EventModel();
            if (leagueId.HasValue)
            {
                ev.isCollapsable = true;
                ev.EventList = leagueRepo.GetById(leagueId.Value).Events.ToList();
            } else
            {
                ev.isCollapsable = false;
                ev.EventList = leagueRepo.GetClubById(clubId.Value).Events.ToList();
            }
            return PartialView("_Events", ev);
        }

        [HttpGet]
        public ActionResult AddEvent(int? leagueId, int? clubId)
        {
            var ef = new EventForm
            {
                LeagueId = leagueId,
                LeagueName = leagueId.HasValue ? leagueRepo.GetById(leagueId.Value).Name : null,
                ClubId = clubId,
                ClubName = clubId.HasValue ? leagueRepo.GetClubById(clubId.Value).Name : null
            };

            return PartialView("_AddEvent", ef);
        }

        [HttpPost]
        public ActionResult AddEvent(EventForm ef)
        {
            if (!ModelState.IsValid)
            {
                return PartialView("_AddEvent", ef);
            }
            eventsRepo.Create(ef.ToEvent());
            return RedirectToAction("List", new { leagueId = ef.LeagueId, clubId = ef.ClubId });
        }

        [HttpPost]
        public ActionResult UpdateEvent(Event ev)
        {
            eventsRepo.Update(ev);
            return Json(new { stat = "ok", id = ev.EventId });
        }

        public ActionResult DeleteEvent(int eventId, int? leagueId, int? clubId)
        {
            eventsRepo.Delete(eventId);
            return RedirectToAction("List", new { leagueId = leagueId, clubId = clubId });
        }

        public ActionResult PublishEvent(int eventId, bool isPublished)
        {
            try
            {
                var ev = eventsRepo.GetById(eventId);
                ev.IsPublished = isPublished;
                eventsRepo.Update(ev);
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch (Exception e)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, e.ToString());
            }
        }
    }
}