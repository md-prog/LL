using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using AppModel;
using DataService;
using DataService.DTO;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("api/Teams")]
    public class TeamsController : BaseLogLigApiController
    {
        private SeasonsRepo _seasonsRepo;
        private TeamsService _teamsService = null;
        private SectionsRepo _sectionsRepo = null;
        private LeagueRepo _leagueRepo = null;

        protected SeasonsRepo seasonsRepo
        {
            get
            {
                if (_seasonsRepo == null)
                {
                    _seasonsRepo = new SeasonsRepo(db);
                }
                return _seasonsRepo;
            }
        }

        protected TeamsService teamsService
        {
            get
            {
                if (_teamsService == null)
                {
                    _teamsService = new TeamsService(db);
                }
                return _teamsService;
            }
        }

        public SectionsRepo sectionsRepo
        {
            get
            {
                if (_sectionsRepo == null)
                {
                    _sectionsRepo = new SectionsRepo(db);
                }
                return _sectionsRepo;
            }
        }

        protected LeagueRepo leagueRepo
        {
            get
            {
                if (_leagueRepo == null)
                {
                    _leagueRepo = new LeagueRepo(db);
                }
                return _leagueRepo;
            }
        }

        /// <summary>
        /// מחזיר קיר הודעות של קבוצה
        /// </summary>
        /// <param name="teamId">ID קבוצה</param>
        /// <returns></returns>
        [ResponseType(typeof(List<WallThreadViewModel>))]
        [Route("Messages/{teamId}")]
        public IHttpActionResult GetTeamMessages(int teamId)
        {
            List<WallThreadViewModel> MessageThreads = MessagesService.GetTeamMessages(teamId);
            return Ok(MessageThreads);
        }

        /// <summary>
        /// מחזיר דף קבוצה לפי ליגה
        /// </summary>
        /// <param name="teamId">ID קבוצה</param>
        /// <param name="leagueId">ID ליגה</param>
        /// <returns></returns>
        [ResponseType(typeof(LeaguePageVeiwModel))]
        [Route("{teamId}/League/{leagueId}")]
        public IHttpActionResult GetTeam(int teamId, int leagueId)
        {
            try
            {
                var team = teamsService.GetTeamById(teamId);
                if (team == null)
                {
                    return NotFound();
                }

                TeamPageViewModel vm = new TeamPageViewModel();
                if (!team.LeagueTeams.Any(l => l.LeagueId == leagueId))
                {
                    return NotFound();
                }

                vm.TeamInfo = TeamsService.GetTeamInfo(team, leagueId);
                if (vm.TeamInfo == null)
                {
                    return NotFound();
                }

                var section = sectionsRepo.GetByLeagueId(leagueId);

                int? currentSeasonId = seasonsRepo.GetLastSeasonByLeagueId(leagueId);

                var teamGames = team.GuestTeamGamesCycles
                                        .Concat(team.HomeTeamGamesCycles)
                                        .Where(tg => tg.Stage.LeagueId == leagueId && tg.IsPublished).ToList();
                int currentUserId = Convert.ToInt32(User.Identity.Name);

                GamesService.UpdateGameSets(teamGames, section: section?.Alias);
                //Next Game
                vm.NextGame = GamesService.GetNextGame(teamGames, currentUserId, leagueId, currentSeasonId);
                //List of all next games
                vm.NextGames = GamesService.GeTeamNextGames(leagueId, teamId, DateTime.Now, currentSeasonId);
                //Last Game
                vm.LastGame = GamesService.GetLastGame(teamGames, currentSeasonId);
                //Last Games
                vm.LastGames = GamesService.GetLastGames(teamGames, currentSeasonId).OrderBy(x => x.StartDate);
                //League Info
                var leagues = leagueRepo.GetLastSeasonLeaguesBySection(section.SectionId)
                    .Where(l => db.LeagueTeams.Where(lt => lt.TeamId == team.TeamId).Select(lt => lt.LeagueId).Contains(l.LeagueId))
                    .ToList();
                vm.Leagues = leagues.Select(l => new LeagueInfoVeiwModel(l)).ToList();
                //Fans
                vm.Fans = TeamsService.GetTeamFans(team.TeamId, leagueId, CurrentUser.UserId);
                //Game Cycles
                vm.GameCycles = teamGames.Select(gc => gc.CycleNum).Distinct().OrderBy(c => c).ToList();
                //Players
                vm.Players = currentSeasonId != null ? PlayerService.GetActivePlayersByTeamId(teamId, currentSeasonId.Value) :
                                                       new List<CompactPlayerViewModel>();
                // Set friends status for each of the players
                FriendsService.AreFriends(vm.Players, currentUserId);
                //Jobs
                vm.Jobs = TeamsService.GetTeamJobsByTeamId(teamId, currentUserId);

                vm.MessageThreads = MessagesService.GetTeamMessages(teamId);

                return Ok(vm);
            }
            catch (Exception ex)
            {

                return InternalServerError(ex);
            }

        }

        /// <summary>
        /// מחזיר רשימת ליגות והקבוצות מתחתם לפי שם ענף
        /// </summary>
        /// <param name="section">שם ענף</param>
        /// <returns></returns>
        /// // GET: api/Teams/Section/{section}
        [ResponseType(typeof(List<LeagueTeamsViewModel>))]
        [Route("Section/{section}")]
        public IHttpActionResult GetTeams(string section)
        {
            var sectionObj = db.Sections.Where(s => s.Alias == section)
                .Include(s => s.Unions)
                .FirstOrDefault();

            var unions = sectionObj.Unions.Where(u => u.Leagues.Count > 0 && u.IsArchive == false);
            var allLeagues = new List<League>();

            foreach (var union in unions)
            {
                allLeagues.AddRange(union.Leagues.Where(l => l.LeagueTeams.Count > 0 && l.IsArchive == false && l.SeasonId == (int)seasonsRepo.GetLasSeasonByUnionId(union.UnionId)));
            }

            // WARNING: The code below gets the "global" last season for all section and unions, this will NOT return the correct value for the current union or section.
            //var lastSeason = _seasonsRepository.GetLastSeason();

            List<LeagueTeamsViewModel> result = allLeagues.Select(l =>
                new LeagueTeamsViewModel
                {
                    LeagueId = l.LeagueId,
                    Name = l.Name,
                    Teams = l.LeagueTeams.Where(t => t.Teams.IsArchive == false && t.SeasonId == (int)seasonsRepo.GetLasSeasonByUnionId((int)l.UnionId))
                    .Select(t => new TeamCompactViewModel(t.Teams, l.LeagueId, t.SeasonId))
                }).ToList();

            return Ok(result);
        }

        /// <summary>
        /// מחזיר רשימת ליגות והקבוצות מתחתם לפי שם ענף
        /// </summary>
        /// <param name="section">שם ענף</param>
        /// <param name="unionId">id of the union to determine last season</param>
        /// <returns></returns>
        /// // GET: api/Teams/Section/{section}
        [ResponseType(typeof(List<LeagueTeamsViewModel>))]
        [Route("Section/{section}/{unionId}")]
        public IHttpActionResult GetTeams(string section, int unionId)
        {
            var sectionObj = db.Sections.Include(s => s.Unions).FirstOrDefault(s => s.Alias == section);

            if (sectionObj == null)
            {
                return NotFound();
            }

            var unions = sectionObj.Unions.Where(u => u.Leagues.Count > 0 && u.IsArchive == false);
            var allLeagues = new List<League>();

            foreach (var union in unions)
            {
                allLeagues.AddRange(union.Leagues.Where(l => l.LeagueTeams.Count > 0 && l.IsArchive == false));
            }

            int? lastSeasonId = seasonsRepo.GetLasSeasonByUnionId(unionId);
            if (lastSeasonId.HasValue)
            {
                var result = allLeagues.Select(l =>
                    new LeagueTeamsDto()
                    {
                        LeagueId = l.LeagueId,
                        Name = l.Name,
                        Teams = l.LeagueTeams.Where(t => t.Teams.IsArchive == false && t.SeasonId == lastSeasonId)
                            .Select(t => new TeamInformationDto()
                            {
                                Team = new TeamDto()
                                {
                                    TeamId = t.TeamId,
                                    Title = t.Teams.Title,
                                    LeagueId = l.LeagueId,
                                    Logo = t.Teams.Logo
                                },
                                TeamInformation =
                                    t.Teams != null &&
                                    t.Teams.TeamsDetails.FirstOrDefault(td => td.SeasonId == lastSeasonId) != null
                                        ? new TeamDetailsDto
                                        {
                                            TeamName =
                                                t.Teams.TeamsDetails.FirstOrDefault(_td => _td.SeasonId == lastSeasonId)
                                                    .TeamName
                                        }
                                        : null
                            })
                    })
                    .ToList()
                    .Select(x => new LeagueTeamsViewModel
                    {
                        LeagueId = x.LeagueId,
                        Name = x.Name,
                        Teams = x.Teams.Select(t => new TeamCompactViewModel()
                        {
                            TeamId = t.Team.TeamId,
                            Title = t.TeamInformation != null ? t.TeamInformation.TeamName : t.Team.Title,
                            LeagueId = x.LeagueId,
                            Logo = t.Team.Logo
                        })
                    })
                    .ToList();

                return Ok(result);
            }

            return Ok(new List<LeagueTeamsViewModel>());
        }



        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool TeamExists(int id)
        {
            return db.Teams.Count(e => e.TeamId == id) > 0;
        }
    }

}