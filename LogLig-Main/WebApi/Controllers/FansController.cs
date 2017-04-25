using System;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;
using AppModel;
using DataService;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [RoutePrefix("api/Fans")]
    public class FansController : BaseLogLigApiController
    {
        readonly SeasonsRepo _seasonsRepo = new SeasonsRepo();

        /// <summary>
        /// מחזיר דף הבית של אוהד בהתייחסות לקבוצה בליגה
        /// </summary>
        /// <param name="teamId">קבוצה ID</param>
        /// <param name="leagueId">ליגה ID</param>
        /// <returns></returns>
        // GET: api/Fans/Home/Team/{teamId}/League/{leagueId}
        [Route("Home/Team/{teamId}/League/{leagueId}")]
        [Authorize]
        //[ResponseType(typeof(FanOwnPrfileViewModel))]
        public IHttpActionResult GetFanHomePage(int teamId, int leagueId)
        {
            User user = CurrentUser;
            if (user == null)
            {
                return NotFound();
            }

            Team team = db.Teams.Find(teamId);
            if (team == null)
            {
                return NotFound();
            }

            if (!team.LeagueTeams.Any(l => l.LeagueId == leagueId))
            {
                return NotFound();
            }

            int? seasonId = _seasonsRepo.GetLastSeasonByLeagueId(leagueId);

            var vm = new FanOwnPrfileViewModel();

            vm.TeamInfo = TeamsService.GetTeamInfo(team, leagueId);

            var teamGames = team.GuestTeamGamesCycles
                                    .Concat(team.HomeTeamGamesCycles)
                                            .Where(tg => tg.Stage.LeagueId == leagueId && tg.IsPublished)
                                            .ToList();

            //Next Game
            vm.NextGame = GamesService.GetNextGame(teamGames, Convert.ToInt32(User.Identity.Name), leagueId, seasonId);
            //Last Game
            vm.LastGame = GamesService.GetLastGame(teamGames, seasonId);
            //Team Fans
            vm.TeamFans = TeamsService.GetTeamFans(team.TeamId, leagueId, CurrentUser.UserId);
            //Friends
            vm.Friends = FriendsService.GetAllConfirmedFriendsAsUsers(user).Select(u =>
                new FanFriendViewModel
                {
                    Id = u.UserId,
                    UserName = u.UserName,
                    FullName = u.FullName,
                    UserRole = u.UsersType.TypeRole,
                    Image = u.Image,
                    CanRcvMsg = true,
                    FriendshipStatus = FriendshipStatus.Yes,
                    Teams = TeamsService.GetFanTeams(u)
                }).Where(u => u.Id != this.CurrUserId).ToList();

            return Ok(vm);
        }


        /// <summary>
        /// מחזיר דף אוהד כפי שמשתמש אחר רואה אותו
        /// </summary>
        /// <param name="id">ID משתמש</param>
        /// <param name="unionId"></param>
        /// <returns></returns>
        /// // GET: api/Fans/5
        [AllowAnonymous]
        [Route("{id}")]
        [ResponseType(typeof(FanPrfileViewModel))]
        public IHttpActionResult GetFan(int id, int? unionId = null)
        {
            User fan = db.Users.FirstOrDefault(u => u.UserId == id &&
                                                    u.IsArchive == false &&
                                                    u.UsersType.TypeRole == AppRole.Fans);

            if (fan == null) return NotFound();

            User user = CurrentUser;

            int? seasonId = unionId != null ? _seasonsRepo.GetLastSeasonByCurrentUnionId(unionId.Value) :
                                              (int?)null;

            FanPrfileViewModel vm = user == null ?
                                    FansService.GetFanProfileAsAnonymousUser(fan, seasonId) :
                                    FansService.GetFanProfileAsLoggedInUser(user, fan, seasonId);

            return Ok(vm);
        }

        /// <summary>
        /// עריכת פרופיל אוהד
        /// </summary>
        /// <param name="id">ID אוהד</param>
        /// <param name="bm">Fan Edit Profile Binding Model</param>
        /// <returns></returns>
        // Post: api/Fans/Edit
        [Route("Edit/{id}")]
        [ResponseType(typeof(void))]
        public async Task<IHttpActionResult> PutFanProfile(int id, FanEditProfileBindingModel bm)
        {

            if (User == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != CurrentUser.UserId)
            {
                return BadRequest();
            }

            var usr = db.Users.Find(id);

            if (usr == null)
            {
                return BadRequest();
            }

            if (!string.IsNullOrEmpty(bm.UserName))
                usr.UserName = bm.UserName;
            if (!string.IsNullOrEmpty(bm.Email))
                usr.Email = bm.Email;
            if (!string.IsNullOrEmpty(bm.Password))
                usr.Password = Protector.Encrypt(bm.Password);
            if (bm.Teams != null)
            {
                usr.TeamsFans.Clear();
                foreach (var t in bm.Teams)
                {
                    usr.TeamsFans.Add(new TeamsFan
                    {
                        TeamId = t.TeamId,
                        UserId = usr.UserId,
                        LeageId = t.LeagueId
                    });
                }
            }
            db.Entry(usr).State = EntityState.Modified;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool UserExists(int id)
        {
            return db.Users.Count(e => e.UserId == id) > 0;
        }

        // POST: api/Fans/Going
        [Route("Going")]
        public IHttpActionResult PostFanGoing(GoingDTO item)
        {
            User user = CurrentUser;
            var game = db.GamesCycles.Find(item.Id);
            var fan = game.Users.FirstOrDefault(t => t.UserId == user.UserId);

            if (item.IsGoing == 1)
            {
                if (fan == null)
                {
                    game.Users.Add(user);
                    db.SaveChanges();
                }
            }
            else
            {
                if (fan != null)
                {
                    game.Users.Remove(fan);
                    db.SaveChanges();
                }
            }

            return Ok();
        }
    }
}