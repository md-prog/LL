using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

using Omu.ValueInjecter;
using AppModel;
using CmsApp.Models;
using DataService;
using Resources;

namespace CmsApp.Controllers
{
    public class StagesController : AdminController
    {
        StagesRepo stRepo = new StagesRepo();
        GamesRepo gamesRepo = new GamesRepo();

        // GET: Stages
        public ActionResult Index(int id, int? seasonId)
        {
            ViewBag.SeasonId = seasonId;
            var resList = stRepo.GetAll(id);
            return PartialView("_List", resList);
        }

        public ActionResult Create(int id, int seasonId)
        {
            var stage = stRepo.Create(id);
            
            stRepo.Save();
            return RedirectToAction("Index", new { id = id, seasonId });

            //var model = new GameForm();
            //model.DaysList = Messages.WeekDays.Split(',');
            //model.StartDate = DateTime.Now;
            //model.StageId = stage.StageId;
            //model.LeagueId = stage.LeagueId;
            //return PartialView("_Settings", model);
        }

        public ActionResult Delete(int id, int? seasonId)
        {

            stRepo.DeleteAllGameCycles(id);
            stRepo.DeleteAllGroups(id);
            var st = stRepo.GetById(id);
            st.IsArchive = true;
            stRepo.Save();

            return RedirectToAction("Index", new { id = st.LeagueId, seasonId });
        }

        public ActionResult CreateSetting(GameForm frm, string[] daysArr)
        {
            var item = new Game();
            item.StageId = frm.StageId;
            item.SortDescriptors = "0,1,2";
            item.GameDays = string.Join(",", daysArr);
            item.StartDate = frm.StartDate;
            gamesRepo.Create(item);
            UpdateModel(item);
            gamesRepo.Save();
            TempData["Id"] = frm.LeagueId;
            TempData["Close"] = "close";
            return PartialView("_Settings", frm);
        }
    }
}