using System.Collections.Generic;
using System.Web.Http;
using System.Web.Http.Description;
using DataService;
using WebApi.Models;
using WebApi.Services;

namespace WebApi.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [RoutePrefix("api/Clubs")]
    public class ClubsController : BaseLogLigApiController
    {
        /// <summary>
        /// Get info about sepcific club by his id
        /// </summary>
        /// <param name="id">id of club</param>
        /// <param name="unionId"></param>
        /// <returns></returns>
        [Route("{id}")]
        [ResponseType(typeof(ClubInfoViewModel))]
        public IHttpActionResult Get(int id, int? unionId = null)
        {
            SeasonsRepo seasonsRepo = new SeasonsRepo();
            int? seasonId = unionId != null ? seasonsRepo.GetLastSeasonByCurrentUnionId(unionId.Value) :
                                              (int?)null;

            var clubInfo = ClubService.GetClub(id, seasonId);
            return Ok(clubInfo);
        }

        /// <summary>
        /// Get list of clubs
        /// <param name="id"></param>
        /// <param name="seasonId"></param>
        /// </summary>
        /// <returns></returns>
        //[Route("list")]
        [ResponseType(typeof(List<ClubTeamInfoViewModel>))]
        [Route("section/{id}")]
        public IHttpActionResult GetAll(int id, int seasonId)
        {
            var clubs = ClubService.GetAll(id, seasonId);

            return Ok(clubs);
        }
    }
}
