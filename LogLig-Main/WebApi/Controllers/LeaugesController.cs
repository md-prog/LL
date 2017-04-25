using AppModel;
using DataService;
using DataService.LeagueRank;
using Resources;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [Authorize]
    [RoutePrefix("api/Leauges")]
    public class LeaugesController : BaseLogLigApiController
    {
        private TeamsRepo _teamsRepo = null;
        private SeasonsRepo _seasonsRepo = null;
        private LeagueRepo _leagueRepo = null;

        public TeamsRepo teamsRepo
        {
            get
            {
                if (_teamsRepo == null)
                {
                    _teamsRepo = new TeamsRepo();
                }
                return _teamsRepo;
            }
        }

        public SeasonsRepo seasonsRepo
        {
            get
            {
                if (_seasonsRepo == null)
                {
                    _seasonsRepo = new SeasonsRepo();
                }
                return _seasonsRepo;
            }
        }

        public LeagueRepo leagueRepo
        {
            get
            {
                if (_leagueRepo == null)
                {
                    _leagueRepo = new LeagueRepo();
                }
                return _leagueRepo;
            }
        }

        /// <summary>
        /// מחזיר דף ליגה
        /// </summary>
        /// <param name="id">ID ליגה</param>
        /// <param name="ln"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ResponseType(typeof(LeaguePageVeiwModel))]
        [Route("{id}")]
        public IHttpActionResult GetLeague(int id, string ln)
        {
            CultureModel.ChangeCulture(ln);

            League league = db.Leagues.Find(id);
            if (league == null)
            {
                return NotFound();
            }

            int? currentSeasonId = league.SeasonId;

            LeagueRankService lrsvc = new LeagueRankService(id);
            LeaguePageVeiwModel vm = new LeaguePageVeiwModel();
            //League Info
            vm.LeagueInfo = new LeagueInfoVeiwModel(league);

            //var teamWithMostFans = league.Teams.OrderByDescending(t => t.TeamsFans.Where(tf => tf.LeageId == id).Count()).FirstOrDefault();

            //Team with the most fans
            var teamRepo = new TeamsRepo();
            var topTeamTuple = teamRepo.GetByMostFans(league.LeagueId);
            var topTeam = topTeamTuple == null ? null : teamRepo.GetById(topTeamTuple.Item1);
            if (topTeam != null)
            {
                vm.TeamWithMostFans = new TeamCompactViewModel
                {
                    FanNumber = topTeamTuple.Item2,
                    TeamId = topTeam.TeamId,
                    Logo = topTeam.Logo,
                    Title = topTeam.Title,
                    LeagueId = league.LeagueId
                };
            }

            var leagueGames = db.GamesCycles.Include(t => t.Auditorium)
                .Include(t => t.GuestTeam)
                .Include(t => t.HomeTeam)
                .Where(gc => gc.Stage.LeagueId == id && gc.IsPublished).ToList();

            //Next Game
            vm.NextGame = GamesService.GetNextGame(leagueGames, base.CurrUserId, id, currentSeasonId);

            //List of all next games
            if (vm.NextGame != null)
            {
                vm.NextGames = GamesService.GetNextGames(leagueGames, vm.NextGame.StartDate, currentSeasonId);
            }

            //List of all last games
            vm.LastGames = GamesService.GetLastGames(leagueGames, currentSeasonId);

            //League table
            var rLeague = lrsvc.CreateLeagueRankTable(currentSeasonId);
            if (rLeague != null)
            {
                vm.LeagueTableStages = rLeague.Stages; // lrsvc.CreateLeagueRankTable(currentSeasonId).Stages;
                MakeGroupStages(vm.LeagueTableStages, isEmpty: false);
            }

            if (vm.LeagueTableStages == null || vm.LeagueTableStages.Count == 0)
            {
                vm.LeagueTableStages = lrsvc.CreateEmptyRankTable(currentSeasonId).Stages;
                MakeGroupStages(vm.LeagueTableStages, isEmpty: true);
            }

            vm.GameCycles = leagueGames.Select(gc => gc.CycleNum).Distinct().OrderBy(c => c).ToList();
            vm.LeagueTableStages = vm.LeagueTableStages.Where(x => x.Groups.All(y => !y.IsAdvanced)).ToList();
            return Ok(vm);
        }

        private void MakeGroupStages(List<RankStage> stages, bool isEmpty)
        {
            if (isEmpty)
            {
                foreach (var team in stages.SelectMany(x => x.Groups).SelectMany(t => t.Teams))
                {
                    team.Points = 0;
                    team.Draw = 0;
                    team.Address = string.Empty;
                    team.Position = "";
                    team.PositionNumber = 0;
                    team.Games = 0;
                    team.Wins = 0;
                    team.Loses = 0;
                    team.SetsWon = 0;
                    team.SetsLost = 0;
                    team.HomeTeamFinalScore = 0;
                    team.GuestTeamScore = 0;
                    team.TotalPointsScored = 0;
                    team.TotalHomeTeamPoints = 0;
                    team.TotalGuesTeamPoints = 0;
                    team.TotalPointsLost = 0;
                    team.HomeTeamScore = 0;
                    team.GuestTeamScore = 0;
                    team.GuesTeamFinalScore = 0;
                    team.Logo = string.Empty;
                }
            }

            foreach (var stage in stages)
            {
                var nameStage = "";
                if (stage.Groups.Count() > 0 && stage.Groups.All(g => g.IsAdvanced))
                {
                    var firstGroup = stage.Groups.FirstOrDefault();
                    if (firstGroup != null && firstGroup.PlayoffBrackets != null)
                    {
                        int numOfBrackets = (int)firstGroup.PlayoffBrackets;
                        switch (numOfBrackets)
                        {
                            case 1:
                                nameStage = Messages.Final; break;
                            case 2:
                                nameStage = Messages.Semifinals; break;
                            case 4:
                                nameStage = Messages.Quarter_finals;
                                break;
                            case 8:
                                nameStage = Messages.Final_Eighth;
                                break;
                            default:
                                nameStage = (numOfBrackets * 2) + Messages.FinalNumber;
                                break;
                        }
                    }
                }
                else
                {
                    stage.Playoff = false;
                    foreach (var group in stage.Groups)
                    {
                        var teamsPositions = group.Teams.Select(x => x.Position).ToArray();
                        for (var i = 0; i < group.Teams.Count; i++)
                        {
                            int numOfBrackets = (int)group.PlayoffBrackets;
                            if (i != 0 && teamsPositions[i] == teamsPositions[i - 1])
                            {
                                group.Teams[i].Position = "-";
                                group.Teams[i].PositionNumber = i;
                            }
                            else
                            {
                                group.Teams[i].Position = (i + 1).ToString();
                                group.Teams[i].PositionNumber = (i + 1);
                            }
                        }
                    }
                    continue;
                }
                stage.Playoff = true;
                stage.Name = nameStage;
                foreach (var group in stage.Groups)
                {
                    for (var i = 0; i < group.Teams.Count; i++)
                    {
                        int numOfBrackets = (int)group.PlayoffBrackets;
                        if (i % ((numOfBrackets)) == 0)
                        {
                            group.Teams[i].Position = (i + 1).ToString();
                            group.Teams[i].PositionNumber = (i + 1);
                        }
                        else
                        {
                            group.Teams[i].Position = "-";
                            group.Teams[i].PositionNumber = i;
                        }
                    }

                }
            }
        }

        /// <summary>
        /// מחזירת רשימת ליגות בלי קבוצות
        /// </summary>
        /// <param name="section">שם ענף</param>
        /// <returns></returns>
        /// // GET: api/Leauges/Section/{section}
        [AllowAnonymous]
        [ResponseType(typeof(List<LeaguesListItemViewModel>))]
        [Route("Section/{section}")]
        public IHttpActionResult GetLeagues(string section)
        {
            var sectionObj = db.Sections.Where(s => s.Alias == section)
                .Include(s => s.Unions)
                .FirstOrDefault();

            var unions = sectionObj.Unions.Where(u => u.Leagues.Count > 0 && u.IsArchive == false);
            var allLeagues = new List<League>();

            foreach (var union in unions)
            {
                var lastSeason = db.Seasons.OrderByDescending(x => x.Id).Where(l => l.UnionId == union.UnionId).First();
                allLeagues.AddRange(union.Leagues.Where(l => l.EilatTournament == null && !l.IsArchive && l.SeasonId == lastSeason.Id));
            }

            var result = allLeagues.OrderBy(l => l.SortOrder)
                .Select(l => new LeaguesListItemViewModel
                {
                    Id = l.LeagueId,
                    Title = l.Name,
                    TotalTeams = l.LeagueTeams.Where(t => t.Teams.IsArchive == false).Count(),
                    TotalFans = l.LeagueTeams.Join(db.TeamsFans, tf => tf.TeamId, lt => lt.TeamId, (lt, tf) => tf.UserId).Distinct().Count(),
                    Logo = l.Logo
                }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// מחזירת רשימת ליגות בלי קבוצות
        /// </summary>
        /// <param name="section">שם ענף</param>
        /// <returns></returns>
        /// // GET: api/Leauges/Section/{section}
        [AllowAnonymous]
        [ResponseType(typeof(List<LeaguesListItemViewModel>))]
        [Route("SectionET/{section}")]
        public IHttpActionResult GetLeaguesET(string section)
        {
            var sectionId = db.Sections.Where(s => s.Alias == section)
                .Include(s => s.Unions)
                .FirstOrDefault().SectionId;

            var allLeagues = leagueRepo.GetLastSeasonLeaguesBySection(sectionId)
                    .Where(l => l.EilatTournament ?? false && !l.IsArchive)
                .OrderByDescending(l => l.Place.HasValue).ThenBy(l => l.Place)
                .Select(l => new LeaguesListItemViewModel
                {
                    Id = l.LeagueId,
                    Title = l.Name,
                    TotalTeams = l.LeagueTeams.Where(t => t.Teams.IsArchive == false).Count(),
                    TotalFans = l.TeamsFans.Count,
                    Logo = l.Logo
                }).ToList();

            return Ok(allLeagues);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="ln"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [ResponseType(typeof(LeagueRank))]
        [Route("Rank/{id}")]
        public IHttpActionResult Rank(int id, string ln)
        {
            CultureModel.ChangeCulture(ln);
            var leagueRank = new LeagueRank
            {
                Stages = new List<StageRank>()
            };

            int? seasonId = seasonsRepo.GetLastSeasonByLeagueId(id);
            LeagueRankService svc = new LeagueRankService(id);
            RankLeague rLeague = svc.CreateLeagueRankTable(seasonId) ?? new RankLeague();

            foreach (var stage in rLeague.Stages)
            {
                if (stage.Groups.All(g => g.IsAdvanced))
                    continue;
                var rankStage = new StageRank();
                var nameStage = "";
                if (stage.Groups.Any() && stage.Groups.All(g => g.IsAdvanced))
                {
                    var firstGroup = stage.Groups.FirstOrDefault();
                    if (firstGroup != null && firstGroup.PlayoffBrackets != null)
                    {
                        int numOfBrackets = (int)firstGroup.PlayoffBrackets;
                        switch (numOfBrackets)
                        {
                            case 1:
                                nameStage = Messages.Final; break;
                            case 2:
                                nameStage = Messages.Semifinals; break;
                            case 4:
                                nameStage = Messages.Quarter_finals;
                                break;
                            case 8:
                                nameStage = Messages.Final_Eighth;
                                break;
                            default:
                                nameStage = (numOfBrackets * 2) + Messages.FinalNumber;
                                break;
                        }
                    }
                }
                else
                {
                    nameStage = Messages.Stage + stage.Number;
                }

                rankStage.NameStage = nameStage;
                rankStage.Groups = new List<GroupRank>();

                foreach (var group in stage.Groups)
                {
                    var rankGroup = new GroupRank
                    {
                        NameGroup = @group.Title,
                        Teams = new List<TeamRank>()
                    };

                    for (var i = 0; i < group.Teams.Count(); i++)
                    {
                        var rankTeam = new TeamRank
                        {
                            Team = @group.Teams[i].Title,
                            Logo = @group.Teams[i].Logo
                        };

                        int numOfBrackets = (int)group.PlayoffBrackets;
                        rankTeam.Position = i % (numOfBrackets) == 0 ? (i + 1).ToString() : "-";

                        rankGroup.Teams.Add(rankTeam);
                    }
                    rankStage.Groups.Add(rankGroup);
                }
                leagueRank.Stages.Add(rankStage);
            }

            return Ok(leagueRank);
        }
    }
}
