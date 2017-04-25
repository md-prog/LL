using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using AppModel;
using CmsApp.Models;
using DataService;

namespace CmsApp.Controllers
{
    public class SeasonsController : Controller
    {
        private readonly SeasonsRepo _seasonsRepository;
        private readonly LeagueRepo _leagueRepo;

        public SeasonsController()
        {
            _seasonsRepository = new SeasonsRepo();
            _leagueRepo = new LeagueRepo();
        }

        [HttpGet]
        public ActionResult List(int entityId, LogicaName logicalName, int? seasonId)
        {
            var viewModel = new SeasonViewModel()
            {
                LogicalName = logicalName,
                EntityId = entityId,
                SeasonId = seasonId
            };

            switch (logicalName)
            {
                case LogicaName.Union:
                    viewModel.Seasons = _seasonsRepository.GetSeasonsByUnion(entityId);
                    break;
                case LogicaName.Club:
                    viewModel.Seasons = _seasonsRepository.GetClubsSeasons(entityId);
                    break;
            }

            return PartialView("_List", viewModel);
        }

        [HttpGet]
        public ActionResult Create(int entityId, LogicaName logicalName, int? seasonId)
        {
            ViewBag.Leagues = logicalName == LogicaName.Union && seasonId.HasValue ?
                               _leagueRepo.GetLeaguesBySesonUnion(entityId, seasonId.Value) :
                               null;

            var model = new CreateSeason
            {
                EntityId = entityId,
                RelevantEntityLogicalName = logicalName
            };

            return PartialView("_CreateSeason", model);
        }

        [HttpPost]
        public ActionResult Create(CreateSeason model)
        {
            if (ModelState.IsValid)
            {
                var newSeason = new Season
                {
                    Name = model.Name,
                    StartDate = model.StartDate.Value,
                    EndDate = model.EndDate.Value,
                    Description = model.Description
                };

                switch (model.RelevantEntityLogicalName)
                {
                    case LogicaName.Union:
                        newSeason.UnionId = model.EntityId;

                        var lastSeasonId = _seasonsRepository.GetLastSeasonByCurrentUnionId(model.EntityId);
                        _seasonsRepository.Create(newSeason);
                        _seasonsRepository.Save();

                        var leaguesToDuplicate = model.Leagues;
                        if (model.IsDuplicate != null && model.IsDuplicate.Value)
                        {
                            _seasonsRepository.Duplicate(leaguesToDuplicate, lastSeasonId, newSeason.Id);
                            _seasonsRepository.Save();
                        }

                        return RedirectToAction("Edit", "Unions", new { id = model.EntityId });
                    case LogicaName.Club:
                        newSeason.ClubId = model.EntityId;

                        _seasonsRepository.Create(newSeason);
                        _seasonsRepository.Save();

                        return RedirectToAction("Edit", "Clubs", new { id = model.EntityId });
                }
            }

            return PartialView("_CreateSeason", model);
        }

        [HttpGet]
        public ActionResult GetSeason(int seasonId, int unionId)
        {
            var season = _seasonsRepository.Get(seasonId, unionId);

            var mappedSeason = new Season
            {
                Description = season.Description,
                StartDate = season.StartDate,
                EndDate = season.EndDate
            };

            return Json(mappedSeason, JsonRequestBehavior.AllowGet);
        }
    }
}
