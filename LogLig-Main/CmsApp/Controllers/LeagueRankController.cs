using DataService.LeagueRank;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DataService;

namespace CmsApp.Controllers
{
    public class LeagueRankController : AdminController
    {
        #region Fields & constructor
        private readonly TeamsRepo _teamsRepo;
        private readonly UnionsRepo _unionsRepo;
        public LeagueRankController()
        {
            _teamsRepo = new TeamsRepo();
            _unionsRepo = new UnionsRepo();
        }
        #endregion



        // GET: LeagueRank/Details/5
        public ActionResult Details(int id, int seasonId, int unionId)
        {
            var section = _unionsRepo.GetSectionByUnionId(unionId);
            var sectionAlias = section.Alias;

            LeagueRankService svc = new LeagueRankService(id);
            RankLeague rLeague = svc.CreateLeagueRankTable(seasonId);

            if (rLeague == null)
                rLeague = new RankLeague();

            else if (rLeague.Stages.Count == 0)
            {
                rLeague = svc.CreateEmptyRankTable(seasonId);
                rLeague.IsEmptyRankTable = true;

                if (rLeague.Stages.Count == 0)
                {
                    if (User.IsInAnyRole(AppRole.Workers))
                    {
                        switch (usersRepo.GetTopLevelJob(base.AdminId))
                        {
                            case JobRole.UnionManager:
                                rLeague.Teams = _teamsRepo.GetTeams(seasonId, id).ToList();
                                break;
                            case JobRole.LeagueManager:
                                rLeague.Teams = _teamsRepo.GetTeams(seasonId, id).ToList();
                                break;
                            case JobRole.TeamManager:
                                rLeague.Teams = _teamsRepo.GetByManagerId(base.AdminId, seasonId);
                                break;
                        }
                    }
                    else
                    {
                        rLeague.Teams = _teamsRepo.GetTeams(seasonId, id).ToList();
                    }
                }
            }


            switch (sectionAlias)
            {
                case GamesAlias.WaterPolo:
                    return PartialView("Waterpolo/_Details", rLeague);

                case GamesAlias.BasketBall:
                    return PartialView("Basketball/_Details", rLeague);

                case GamesAlias.NetBall:
                case GamesAlias.VolleyBall:
                    //TODO display extended table
                    return PartialView("Netball_VolleyBall/_Details", rLeague);

                default:
                    return PartialView("_Details", rLeague);
            }
        }
    }
}
