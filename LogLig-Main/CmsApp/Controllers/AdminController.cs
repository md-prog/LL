using CmsApp.Helpers;
using DataService;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AppModel;

namespace CmsApp.Controllers
{
    [AppAuthorize]
    public class AdminController : Controller
    {
        private const string UNION_CURRENT_SEASON_ID = "UnionCurrentSeasonId";
        private const string CURRENT_UNION_ID = "CurrentUnionId";
        private const string CLUB_CURRENT_SEASON_ID = "ClubCurrentSeasonId";
        private const string CURRENT_CLUB_ID = "CurrentClubId";
        private const string CURRENT_SECTION = "CurrentSection";

        protected AuthorizationEntitiesService AuthSvc;

        protected DataEntities db;

        public AdminController()
        {
            db = new DataEntities();
            AuthSvc = new AuthorizationEntitiesService();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }

        #region Repositories
        private UsersRepo _usersRepo;
        private SeasonsRepo _seasonsRepository;
        private GamesRepo _gamesRepo;
        private LeagueRepo _leagueRepo;
        private ClubsRepo _clubsRepo;
        private EventsRepo _eventsRepo;
        private TeamsRepo _teamRepo;
        private PlayersRepo _playersRepo;
        private PositionsRepo _posRepo;
        private UnionsRepo _unionsRepo;
        private AuditoriumsRepo _auditoriumsRepo;
        private SectionsRepo _secRepo;
        private JobsRepo _jobsRepo;
        private GroupsRepo _groupRepo;
        private StagesRepo _stagesRepo;

        protected UsersRepo usersRepo
        {
            get
            {
                if (_usersRepo == null)
                {
                    _usersRepo = new UsersRepo();
                }
                return _usersRepo;
            }
        }
        protected GamesRepo gamesRepo
        {
            get
            {
                if (_gamesRepo == null)
                {
                    _gamesRepo = new GamesRepo(db);
                }
                return _gamesRepo;
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
        protected ClubsRepo clubsRepo
        {
            get
            {
                if (_clubsRepo == null)
                {
                    _clubsRepo = new ClubsRepo(db);
                }
                return _clubsRepo;
            }
        }
        protected EventsRepo eventsRepo
        {
            get
            {
                if (_eventsRepo == null)
                {
                    _eventsRepo = new EventsRepo(db);
                }
                return _eventsRepo;
            }
        }
        protected TeamsRepo teamRepo
        {
            get
            {
                if (_teamRepo == null)
                {
                    _teamRepo = new TeamsRepo(db);
                }
                return _teamRepo;
            }
        }
        protected PlayersRepo playersRepo
        {
            get
            {
                if (_playersRepo == null)
                {
                    _playersRepo = new PlayersRepo(db);
                }
                return _playersRepo;
            }
        }
        protected PositionsRepo posRepo
        {
            get
            {
                if (_posRepo == null)
                {
                    _posRepo = new PositionsRepo(db);
                }
                return _posRepo;
            }
        }
        protected UnionsRepo unionsRepo
        {
            get
            {
                if (_unionsRepo == null)
                {
                    _unionsRepo = new UnionsRepo(db);
                }
                return _unionsRepo;
            }
        }
        protected AuditoriumsRepo auditoriumsRepo
        {
            get
            {
                if (_auditoriumsRepo == null)
                {
                    _auditoriumsRepo = new AuditoriumsRepo(db);
                }
                return _auditoriumsRepo;
            }
        }
        protected SeasonsRepo seasonsRepository
        {
            get
            {
                if (_seasonsRepository == null)
                {
                    _seasonsRepository = new SeasonsRepo(db);
                }
                return _seasonsRepository;
            }
        }
        protected SectionsRepo secRepo
        {
            get
            {
                if (_secRepo == null)
                {
                    _secRepo = new SectionsRepo();
                }
                return _secRepo;
            }
        }
        public JobsRepo jobsRepo
        {
            get
            {
                if (_jobsRepo == null)
                {
                    _jobsRepo = new JobsRepo(db);
                }
                return _jobsRepo;
            }
        }
        public GroupsRepo groupRepo
        {
            get
            {
                if (_groupRepo == null)
                {
                    _groupRepo = new GroupsRepo(db);
                }
                return _groupRepo;
            }
        }
        public StagesRepo stagesRepo
        {
            get
            {
                if (_stagesRepo == null)
                {
                    _stagesRepo = new StagesRepo(db);
                }
                return _stagesRepo;
            }
        }
        #endregion

        protected CultEnum getCulture()
        {
            string culture = "he-IL";

            HttpCookie cookie = Request.Cookies["_culture"];
            if (cookie != null)
            {
                culture = cookie.Value;
            }
            if (culture == "he-IL")
            {
                return CultEnum.Heb_IL;
            } else
            {
                return CultEnum.Eng_UK;
            }
        }

        protected int AdminId
        {
            get { return int.Parse(User.Identity.Name); }
        }

        [HttpPost]
        public ActionResult SetUnionCurrentSeason(int seasonId)
        {
            Session[UNION_CURRENT_SEASON_ID] = seasonId;

            return Json(seasonId, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult SetClubCurrentSeason(int seasonId)
        {
            Session[CLUB_CURRENT_SEASON_ID] = seasonId;

            return Json(seasonId, JsonRequestBehavior.AllowGet);
        }

        protected Season getCurrentSeason()
        {
            int seasonId = GetUnionCurrentSeasonFromSession();
            return seasonsRepository.GetSeasons().FirstOrDefault(s => s.Id == seasonId);
        }

        [HttpGet]
        public ActionResult GetCurrentSeason()
        {
            var season = getCurrentSeason();
            if (season != null)
            {
                string seasonName = season.Name;

                return Json(seasonName, JsonRequestBehavior.AllowGet);
            }

            return Json(string.Empty, JsonRequestBehavior.AllowGet);
        }

        protected int GetUnionCurrentSeasonFromSession()
        {
            var seasonId = Session[UNION_CURRENT_SEASON_ID];
            var currentUnionId = Session[CURRENT_UNION_ID];

            if (currentUnionId == null)
            {
                if (seasonId == null || (int)seasonId <= 0)
                {
                    var season = seasonsRepository.GetLastSeason();
                    Session[UNION_CURRENT_SEASON_ID] = season.Id;
                    seasonId = season.Id;
                }
            }
            else
            {
                if (seasonId == null || (int)seasonId <= 0)
                {
                    seasonId = seasonsRepository.GetLastSeason().Id;
                }
                var name = seasonsRepository.GetSeasons().SingleOrDefault(s => s.Id == (int)seasonId).Name;
                var seasons = seasonsRepository.GetSeasonsByUnion((int)currentUnionId).ToList();
                if (!seasons.Select(s => s.Id).Contains((int)seasonId))
                {
                    seasonId = seasons.Select(s => s.Name).Contains(name) ?
                        seasons.FirstOrDefault(s => s.Name.Equals(name)).Id :
                        seasonsRepository.GetLastSeasonByCurrentUnionId((int)currentUnionId);
                    Session[UNION_CURRENT_SEASON_ID] = seasonId;
                }
            }

            return (int)seasonId;
        }

        protected int GetClubCurrentSeasonFromSession()
        {
            var seasonId = Session[CLUB_CURRENT_SEASON_ID];
            var currentClubId = Session[CURRENT_CLUB_ID];

            if (currentClubId == null)
            {
                if (seasonId == null)
                {
                    var season = seasonsRepository.GetLastClubSeason();
                    Session[CLUB_CURRENT_SEASON_ID] = season.Id;
                    seasonId = season.Id;
                }
            }
            else
            {
                if (seasonId == null)
                {
                    seasonId = seasonsRepository.GetLastClubSeason().Id;
                }

                var season = seasonsRepository.GetSeasons().FirstOrDefault(s => s.Id == (int)seasonId);
                if (season != null)
                {
                    var name = season.Name;
                    var seasons = seasonsRepository.GetSeasonsByClub((int)currentClubId).ToList();

                    if (!seasons.Select(s => s.Id).Contains((int)seasonId))
                    {
                        seasonId = seasons.Select(s => s.Name).Contains(name) ?
                            seasons.FirstOrDefault(s => s.Name.Equals(name)).Id :
                            seasonsRepository.GetLastSeasonIdByCurrentClubId((int)currentClubId);
                        Session[CLUB_CURRENT_SEASON_ID] = seasonId;
                    }
                }
            }

            return (int)seasonId;
        }

        protected int GetCurrentUnionFromSession()
        {
            int unionId;
            if (Session[CURRENT_UNION_ID] != null && int.TryParse(Session[CURRENT_UNION_ID].ToString(), out unionId))
            {
                return unionId;
            }
            return -1;
        }

        protected void SaveCurrentSectionAliasIntoSession(Section model)
        {
            Session[CURRENT_SECTION] = model.Alias;
        }
        protected void SaveSectionIntoCookie(Section model)
        {
            var sectionAlias = new HttpCookie(CURRENT_SECTION);
            sectionAlias.Values["alias"] = model.Alias;
            System.Web.HttpContext.Current.Response.Cookies.Set(sectionAlias);
        }

        protected string GetSectionAliasFromCookie()
        {
            var cookie = System.Web.HttpContext.Current.Request.Cookies[CURRENT_SECTION];
            if (cookie != null)
            {
                return cookie.Value;
            }
            return "";
        }

        protected string GetAliasSectionFromSession()
        {
            var section = Session[CURRENT_SECTION];
            if (section != null)
            {
                return (string)Session[CURRENT_SECTION];
            }
            return string.Empty;
        }
    }
}
