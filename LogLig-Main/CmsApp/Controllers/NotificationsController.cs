using System.Web.Mvc;
using CmsApp.Models;
using DataService;
using AppModel;

namespace CmsApp.Controllers
{
    public class NotificationsController : AdminController
    {
        NotesMessagesRepo notesRep = new NotesMessagesRepo();
        SeasonsRepo seasonsRepo = new SeasonsRepo();

        // GET: Notifications
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Add(int entityId, LogicaName logicalName, int? unionId, int? clubId)
        {
            int currentUnionIdFromSession = GetCurrentUnionFromSession();
            int? currentSeasonIdFromSession = GetUnionCurrentSeasonFromSession();
            int? currentSeasonId = seasonsRepo.GetLasSeasonByUnionId(currentUnionIdFromSession);

            var vm = new NotificationsForm
            {
                SeasonId = currentSeasonIdFromSession,
                RelevantEntityLogicalName = logicalName,
                EntityId = entityId,
                NeedHideTextField = currentSeasonIdFromSession != currentSeasonId
            };

            return PartialView("_AddNew", vm);
        }

        [HttpPost]
        public ActionResult Add(NotificationsForm frm)
        {
            if (!ModelState.IsValid)
            {
                return Content("Error");
            }

            if (frm.SeasonId != null)
            {
                switch (frm.RelevantEntityLogicalName)
                {
                    case LogicaName.Team:
                        notesRep.SendToTeam(frm.SeasonId.Value, frm.EntityId, frm.Message);
                        break;
                    case  LogicaName.League:
                        notesRep.SendToLeague(frm.SeasonId.Value, frm.EntityId, frm.Message);
                        break;
                    case LogicaName.Union:
                        notesRep.SendToUnion(frm.SeasonId.Value, frm.EntityId, frm.Message);
                        break;
                    case LogicaName.Club:
                        //notesRep.SendToClub(frm.SeasonId.Value, frm.EntityId, frm.Message);
                        break;
                }
            }

            var notsServ = new GamesNotificationsService();
            notsServ.SendPushToDevices(GlobVars.IsTest);

            return RedirectToAction("List", new { entityId = frm.EntityId, logicalName = frm.RelevantEntityLogicalName });
        }

        [HttpPost]
        public ActionResult Delete(int id, int entityId, LogicaName logicalName)
        {
            notesRep.DeleteMessage(id);
            notesRep.Save();

            return RedirectToAction("List", new { entityId, logicalName });
        }

        public ActionResult List(int entityId, LogicaName logicalName)
        {
            int? seasonId = GetUnionCurrentSeasonFromSession();
            var nvm = new NotificationsViewModel
            {
                EntityId = entityId,
                RelevantEntityLogicalName = logicalName
            };

            if (seasonId.HasValue)
            {
                switch (logicalName)
                {
                    case LogicaName.Team:
                        nvm.Notifications = notesRep.GetLeagueTeamMessages(seasonId.Value, entityId);
                        break;
                    case LogicaName.League:
                        nvm.Notifications = notesRep.GetLeagueMessages(seasonId.Value, entityId);
                        break;
                    case LogicaName.Union:
                        nvm.Notifications = notesRep.GetUnionMessages(seasonId.Value, entityId);
                        break;
                    case LogicaName.Club:
                        //notiticationsViewModel.Notifications = notesRep.GetClubMessages(entityId);
                        break;
                }
            }
            if(nvm.Notifications != null)
            {
                nvm.Notifications = nvm.Notifications.FindAll(n => ((n.TypeId & MessageTypeEnum.NoPushNotify) == 0));
            }

            return PartialView("_List", nvm);
        }
    }
}