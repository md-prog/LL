using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using AppModel;
using DataService;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [AllowAnonymous]
    [RoutePrefix("api/Games")]
    public class GamesController : BaseLogLigApiController
    {
        readonly GamesRepo _gamesRepo = new GamesRepo();
        readonly DataService.Services.GamesService _gamesService = new DataService.Services.GamesService();
        readonly SeasonsRepo _seasonsRepo = new SeasonsRepo();
        readonly SectionsRepo _sectionRepo = new SectionsRepo();
        /// <summary>
        /// מחזיר קיר הודעות של משחק
        /// </summary>
        /// <param name="teamId">ID משחק</param>
        /// <returns></returns>
        [ResponseType(typeof(List<WallThreadViewModel>))]
        [Route("Messages/{gameId}")]
        public IHttpActionResult GetGameMessages(int gameId)
        {
            List<WallThreadViewModel> MessageThreads = MessagesService.GetGameMessages(gameId);
            return Ok(MessageThreads);
        }


        /// <summary>
        /// מחזיר דף משחק
        /// </summary>
        /// <param name="id">משחק ID</param>
        /// <param name="unionId"></param>
        /// <returns></returns>
        // GET: api/Games/5
        [ResponseType(typeof(GamePageViewModel))]
        public IHttpActionResult Get(int id, int? unionId = null)
        {
            int? seasonId = unionId != null ? _seasonsRepo.GetLastSeasonByCurrentUnionId(unionId.Value) :
                                             (int?)null;

            var game = GamesService.GetGameById(id, seasonId);

            if (game == null)
            {
                return NotFound();
            }

            var section = _sectionRepo.GetByLeagueId(game.LeagueId)?.Alias;

            GamesService.UpdateGameSet(game, section);

            var vm = new GamePageViewModel { GameInfo = game };

            if (User.Identity.IsAuthenticated)
            {
                vm.GoingFriends = GetTeamsFansList(id);
            }

            vm.Sets = GamesService.GetGameSets(id).ToList();


            vm.History = GamesService.GetGameHistory(game.GuestTeamId, game.HomeTeamId);
            GamesService.UpdateGameSets(vm.History);

            GamesService.UpdateGameSet(vm.GameInfo, section);
   
            return Ok(vm);
        }

        /// <summary>
        /// מחזיר רשימת אוהדם שבאים למשחק
        /// </summary>
        /// <param name="gameId">ID משחק</param>
        /// <returns></returns>
        /// GET: api/Games/Fans/{teamId}
        [Authorize]
        [Route("Fans/{gameId}")]
        public IHttpActionResult GetTeams(int gameId)
        {
            var resList = GetTeamsFansList(gameId);

            return Ok(resList);
        }

        private IEnumerable<UserBaseViewModel> GetTeamsFansList(int gameId)
        {
            var user = base.CurrentUser;

            return GamesService.GetGoingFriends(gameId, user)
                     .Select(u => new UserBaseViewModel
                     {
                         Id = u.UserId,
                         UserName = u.UserName,
                         FullName = u.FullName,
                         UserRole = u.UsersType.TypeRole,
                         Image = u.Image,
                         FriendshipStatus = FriendshipStatus.Yes,
                         CanRcvMsg = true
                     });
        }

        /// <summary>
        /// End game by game Id
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        [Authorize]
        [Route("{gameId}/Actions/EndGame")]
        [HttpPost]
        public IHttpActionResult EndGame(int gameId)
        {
            GamesCycle game = _gamesRepo.GetGameCycleById(gameId);
            if (game == null)
            {
                return NotFound();
            }

            _gamesRepo.EndGame(gameId);

            return Ok();
        }

        /// <summary>
        /// Set technical win for game
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="teamId"></param>
        /// <returns></returns>
        [Authorize]
        [Route("{gameId}/{teamId}/Actions/TechnicalWin")]
        [HttpPost]
        public IHttpActionResult SetTechnicalWin(int gameId, int teamId)
        {
            GamesCycle game = _gamesRepo.GetGameCycleById(gameId);
            if (game == null)
            {
                return NotFound();
            }

            _gamesService.SetTechnicalWinForGame(gameId, teamId);

            return Ok();
        }
    }
}
