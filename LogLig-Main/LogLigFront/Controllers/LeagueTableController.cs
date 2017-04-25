using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using DataService;
using DataService.LeagueRank;
using LogLigFront.Models;
using AppModel;

namespace LogLigFront.Controllers
{
    public class LeagueTableController : Controller
    {
        #region Fields & constructor
        private readonly TeamsRepo _teamsRepo;
        private readonly UnionsRepo _unionsRepo;
        private readonly SectionsRepo _sectionsRepo;
        private GamesRepo _gamesRepo = null;

        public GamesRepo gamesRepo
        {
            get
            {
                if (_gamesRepo == null)
                {
                    _gamesRepo = new GamesRepo();
                }
                return _gamesRepo;
            }
        }

        public LeagueTableController()
        {
            _teamsRepo = new TeamsRepo();
            _unionsRepo = new UnionsRepo();
            _sectionsRepo = new SectionsRepo();
        }
        #endregion

        // GET: LeagueTable
        public ActionResult Index(int id, int seasonId, int? union = null)
        {
            string sectionAlias;

            if (union.HasValue)
            {
                var section = _unionsRepo.GetSectionByUnionId(union.Value);
                sectionAlias = section.Alias;
            }
            else
            {
                var section = _sectionsRepo.GetByLeagueId(id);
                sectionAlias = section?.Alias;
            }

            LeagueRankService svc = new LeagueRankService(id);
            RankLeague rLeague = svc.CreateLeagueRankTable(seasonId);
            // rLeague.Stages = rLeague.Stages.Where(x => x.Groups.All(y => !y.IsAdvanced)).ToList();


            if (rLeague.Stages.Count == 0)
            {
                rLeague = svc.CreateEmptyRankTable(seasonId);
                rLeague.IsEmptyRankTable = true;
            }

            if (rLeague.Stages.Count == 0)
            {
                rLeague.Teams = _teamsRepo.GetTeams(seasonId, id).ToList();
            }
            rLeague.SeasonId = seasonId;

            switch (sectionAlias)
            {
                case GamesAlias.WaterPolo:
                    return View("Waterpolo/Index", rLeague);
                case GamesAlias.BasketBall:
                    return View("Basketball/Index", rLeague);

                case GamesAlias.NetBall:
                case GamesAlias.VolleyBall:
                    return View("Netball_VolleyBall/Index", rLeague);

                default:
                    return View(rLeague);

            }
        }

        public ActionResult Schedules(int id, string gameIds, int? seasonId = null)
        {
            var games = string.IsNullOrWhiteSpace(gameIds)
            ? new int[] { }
          : gameIds.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => int.Parse(s))
            .ToArray();
            var resList = gamesRepo.GetCyclesByLeagueAndExcludeGames(id, seasonId, games);
            var lRepo = new LeagueRepo();
            var league = lRepo.GetById(id);

            ViewBag.SeasonId = seasonId;
            if (league != null)
            {
                ViewBag.LeagueId = league.LeagueId;
                ViewBag.Logo = UIHelper.GetLeagueLogo(league.Logo);
                ViewBag.ResTitle = league.Name.Trim();

                var alials = league?.Union?.Section?.Alias;
                switch (alials)
                {
                    case GamesAlias.WaterPolo:
                    case GamesAlias.BasketBall:
                        return View("WaterPoloBasketBall/Schedules", resList);
                    default:
                        return View(resList);
                }
            }



            return View(resList);
        }

        public ActionResult AuditoriumSchedules(int id, int? seasonId = null)
        {
            var resList = gamesRepo.GetCyclesByAuditorium(id, seasonId);
            var aRepo = new AuditoriumsRepo();
            var aud = aRepo.GetById(id);
            ViewBag.AudTitle = aud.Name;
            ViewBag.AudAddress = aud.Address;
            ViewBag.SeasonId = seasonId;
            return View(resList);
        }

        public ActionResult TeamSchedules(int id, int leagueId, int seasonId)
        {
            var gamesRepo = new GamesRepo();
            var resList = gamesRepo.GetCyclesByTeam(id).Where(g => g.LeagueId == leagueId);

            var tRepo = new TeamsRepo();
            var team = tRepo.GetById(id);
            ViewBag.TeamId = id;
            ViewBag.LeagueId = leagueId;
            ViewBag.SeasonId = seasonId;
            if (team != null)
            {
                ViewBag.Logo = UIHelper.GetTeamLogo(team.Logo);
                ViewBag.ResTitle = team.Title.Trim();
                if (team.LeagueTeams.Count > 1)
                {
                    var leagues = team.LeagueTeams.Where(l => (l.Leagues.Stages.Any(s => s.GamesCycles.Any(gc => gc.HomeTeamId == id && gc.IsPublished)) || l.Leagues.Stages.Any(s => s.GamesCycles.Any(gc => gc.GuestTeamId == id && gc.IsPublished))) && l.SeasonId == seasonId);
                    List<SelectListItem> items = new List<SelectListItem>();
                    foreach (var league in leagues)
                    {
                        items.Add(new SelectListItem { Text = league.Leagues.Name, Value = league.LeagueId.ToString(), Selected = league.LeagueId == leagueId });
                    }
                    ViewBag.Leagues = items;
                }

            }
            var lRepo = new LeagueRepo();
            var leag = lRepo.GetById(leagueId);
            if (leag != null)
            {
                var alias = leag.Union?.Section?.Alias;
                switch (alias)
                {
                    case GamesAlias.WaterPolo:
                    case GamesAlias.BasketBall:
                        return View("WaterPoloBasketBall/Schedules", new SchedulesDto { GameCycles = resList });
                    default:
                        return View(nameof(Schedules), 
                            new SchedulesDto { GameCycles = resList });

                }
            }

            return View(nameof(Schedules), new SchedulesDto { GameCycles = resList });
        }

        public ActionResult GameSet(int id)
        {
            var gc = gamesRepo.GetGameCycleById(id);
            var alias = gc.Stage?.League?.Union?.Section?.Alias;


            var resList = gc.GameSets.ToList();
            ViewBag.HomeTeam = gc.HomeTeam.Title;
            ViewBag.GuestTeam = gc.GuestTeam.Title;

            switch (alias)
            {
                case GamesAlias.WaterPolo:
                case GamesAlias.BasketBall:
                    return PartialView("WaterPoloBasketBall/_GameSet", resList);
                default:
                    return PartialView("_GameSet", resList);
            }
        }

        public ActionResult PotentialTeams(int id, int index)
        {
            BracketsRepo repo = new BracketsRepo();
            List<Team> list = repo.GetAllPotintialTeams(id, index);
            return PartialView("_PotentialTeams", list);
        }
    }
}
