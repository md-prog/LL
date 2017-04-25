using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using WebApi.Models;
using WebApi.Services;
using AppModel;
using DataService;

namespace WebApi.Controllers
{
    [RoutePrefix("api/Player")]
    public class PlayerController : BaseLogLigApiController
    {
        readonly SeasonsRepo _seasonsRepo = new SeasonsRepo();

        // GET: api/Player/5
        /// <summary>
        /// Get player by id
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unionId"></param>
        /// <param name="leagueId"></param>
        /// <returns></returns>
        [ResponseType(typeof(PlayerProfileViewModel))]
        public IHttpActionResult Get(int id, int? unionId = null, int? leagueId = null)
        {
            User player = db.Users.FirstOrDefault(u => u.UserId == id && u.IsArchive == false && u.IsActive);
            if (player == null)
            {
                return NotFound();
            }

            if (unionId == null && leagueId != null)
            {
                LeagueRepo leagueRepo = new LeagueRepo();
                League league = leagueRepo.GetById((int)leagueId);
                unionId = league != null ? league.UnionId : null;
            }
            int? seasonId = unionId != null ? _seasonsRepo.GetLastSeasonByCurrentUnionId(unionId.Value) : (int?)null;

            PlayerProfileViewModel vm = PlayerService.GetPlayerProfile(player);

            var teamsRepo = new TeamsRepo();
            vm.Teams = teamsRepo.GetPlayerPositions(id, seasonId);

            vm.FriendshipStatus = FriendsService.AreFriends(id, CurrUserId);

            if (User.Identity.IsAuthenticated)
            {
                vm.Friends = FriendsService.GetAllFanFriends(id, base.CurrUserId);
            }

            vm.Games = GamesService.GetPlayerGames(player.UserId, seasonId);

            GamesService.UpdateGameSets(vm.Games);

            return Ok(vm);
        }

        /// <summary>
        /// Get player games
        /// </summary>
        /// <param name="teamId"></param>
        /// <param name="unionId"></param>
        /// <returns></returns>
        [Route("Games/{teamId}")]
        public IHttpActionResult GetPlayerGames(int teamId, int? unionId = null)
        {
            int? seasonId = unionId != null ? _seasonsRepo.GetLastSeasonByCurrentUnionId(unionId.Value) :
                                              (int?)null;

            var gamesList = GamesService.GetPlayerLastGames(teamId, seasonId);
            GamesService.UpdateGameSets(gamesList);
            return Ok(gamesList);
        }

        /// <summary>
        /// Get ranked teams
        /// </summary>
        /// <param name="leagueId"></param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        [Route("Ranked/{leagueId}/{teamId}")]
        public IHttpActionResult GetRankedTeams(int leagueId, int teamId)
        {
            League league = db.Leagues.Find(leagueId);
            if (league == null)
            {
                return NotFound();
            }

            Team team = db.Teams.Find(teamId);
            if (team == null)
            {
                return NotFound();
            }

            int? seasonId = league.SeasonId;

            var resTeams = TeamsService.GetRankedTeams(leagueId, teamId, seasonId);
            return Ok(resTeams);
        }
    }
}
