using System.Linq;
using System.Web.Http;
using DataService;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    [RoutePrefix("api/Referee")]
    //[Authorize]
    public class RefereeController : BaseLogLigApiController
    {
        // GET: api/Referee/Home
        /// <summary>
        /// Get referee information
        /// </summary>
        /// <param name="id"></param>
        /// <param name="unionId"></param>
        /// <returns></returns>
        [Route("{id}")]
        public IHttpActionResult Get(int id, int unionId)
        {
            var refServ = new RefereeSevice();
            var seasonServ = new SeasonsRepo();
            int? seasonId = seasonServ.GetLasSeasonByUnionId(unionId);
            var gamesList = refServ.GetRefereeGames(id, seasonId);

            var vm = new RefereeViewModel();
            if (gamesList.Count > 0)
            {

                vm.LiveGame = gamesList.FirstOrDefault(t => t.Status == GameStatus.Started);

                vm.ClosedGames = gamesList.Where(t => t.Status != GameStatus.Started && t.Status != GameStatus.Next)
                    .OrderBy(t => t.StartDate)
                    .ToList();

                if (vm.ClosedGames != null && vm.ClosedGames.Count > 0)
                {
                    vm.CloseGame = vm.ClosedGames.Last();
                    vm.NextGames = gamesList.Where(t => t.StartDate > vm.CloseGame.StartDate && t.Status != GameStatus.Started)
                        .OrderBy(t => t.StartDate)
                        .ToList();
                }
                else 
                {
                    vm.NextGames = gamesList.Where(t=> t.Status != GameStatus.Started)
                        .OrderBy(t => t.StartDate)
                        .ToList();
                }
            }
            
            
            return Ok(vm);
        }
    }
}
