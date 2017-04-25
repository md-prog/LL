using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.Entity.Validation;
using System.Globalization;
using System.Security.AccessControl;
using AppModel;
using DataService.DTO;
using DataService.Services;
using DataService.Utils;

namespace DataService
{
    public class GamesRepo : BaseRepo
    {

        private BracketsRepo bracketsRepo;
        private TeamsRepo tRepo;
        private IQueryable<GamesCycle> _gameCycles;

        public class GameFilterConditions
        {
            public int seasonId { get; set; }
            public IEnumerable<LeagueShort> leagues { get; set; }
            public IEnumerable<AuditoriumShort> auditoriums { get; set; }
            //  Always starts from midnight
            private DateTime? _dateFrom;
            public DateTime? dateFrom
            {
                get { return _dateFrom; }
                set
                {
                    if (!value.HasValue)
                    {
                        _dateFrom = null;
                    }
                    else
                    {   //  Cut off time
                        _dateFrom = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day);
                    }
                }
            }
            //  Always ends one millisecond before the next midnight
            private DateTime? _dateTo;
            public DateTime? dateTo
            {
                get { return _dateTo; }
                set
                {
                    if (!value.HasValue)
                    {
                        _dateTo = null;
                    }
                    else
                    {   //  Set time to the last millisecond in the day (include whole day)
                        _dateTo = new DateTime(value.Value.Year, value.Value.Month, value.Value.Day, 23, 59, 59, 999);
                    }
                }
            }
        }

        private void InitPrivates()
        {
            bracketsRepo = new BracketsRepo(db, this);
            tRepo = new TeamsRepo(db);
            _gameCycles = db.GamesCycles.Include(t => t.HomeTeam)
                            .Include(t => t.GuestTeam)
                            .Include(t => t.Auditorium)
                            .Include(t => t.User)
                            .Include(t => t.Stage)
                            .Include(t => t.Group)
                            .Include(t => t.Group.Stage.League.Games)
                            .Include(t => t.HomeTeam.TeamsDetails)
                            .Include(t => t.GuestTeam.TeamsDetails);
        }

        public GamesRepo() : base()
        {
            InitPrivates();
        }
        public GamesRepo(DataEntities db)
            : base(db)
        {
            InitPrivates();
        }


        public Game GetById(int id)
        {
            return db.Games.Find(id);
        }

        public bool IsBasketBallOrWaterPoloGameCycle(int cycleId)
        {
            var game = GetGameCycleById(cycleId);
            var alias = game?.Stage?.League?.Union?.Section?.Alias;
            return alias == GamesAlias.BasketBall || alias == GamesAlias.WaterPolo;
        }

        public Game GetByLeagueStage(int leagueId, int stageId)
        {
            return db.Games.FirstOrDefault(t => t.LeagueId == leagueId && t.StageId == stageId);
        }

        public Game GetByLeague(int leagueId)
        {
            return db.Games.FirstOrDefault(t => t.LeagueId == leagueId);
        }

        public void Create(Game item)
        {
            db.Games.Add(item);
        }

        public Group GetGroupById(int id)
        {
            return db.Groups.Find(id);
        }

        public IEnumerable<GamesCycle> GetGroupsCycles(int leagueId, int? seasonId = null)
        {
            var games = _gameCycles
                .Where(t => t.Stage.LeagueId == leagueId).AsQueryable();
            if (seasonId.HasValue)
            {
                var seasonGames = games.Where(x => x.Stage.League.SeasonId == seasonId.Value).ToList();

                foreach (var game in seasonGames)
                {
                    //if home team has details
                    var homeTeamDetails = game.HomeTeam?.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    if (homeTeamDetails != null)
                    {
                        game.HomeTeam.Title = homeTeamDetails.TeamName;
                    }


                    //if guest team has detials
                    var guesTeamDetails = game.GuestTeam?.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    if (guesTeamDetails != null)
                    {
                        game.GuestTeam.Title = guesTeamDetails.TeamName;
                    }
                }

                return seasonGames.OrderBy(g => g.StartDate)
                    .ThenBy(g => g.Group.Name)
                    .ThenBy(g => g.CycleNum)
                    .ToList();
            }

            games = games.OrderBy(g => g.StartDate)
                .ThenBy(g => g.Group.Name)
                .ThenBy(g => g.CycleNum);

            return games.ToList();
        }

        public IEnumerable<GamesCycle> GetTeamCycles(int teamId)
        {
            return _gameCycles
                .Where(t => t.HomeTeamId == teamId || t.GuestTeamId == teamId)
                .OrderBy(t => t.Stage.LeagueId)
                .ThenBy(g => g.StartDate)
                .ThenBy(g => g.Group.Name)
                .ThenBy(g => g.CycleNum)
                .ToList();
        }

        internal IEnumerable<GamesCycle> GetTeamCycles(int teamId, int leagueId)
        {
            IEnumerable<GamesCycle> allGames = GetTeamCycles(teamId);
            List<GamesCycle> orderdGames = new List<GamesCycle>();
            orderdGames.AddRange(allGames.Where(g => g.Stage.LeagueId == leagueId));
            orderdGames.AddRange(allGames.Where(g => g.Stage.LeagueId != leagueId));
            return orderdGames;
        }

        public IEnumerable<GamesCycleDto> GetCyclesByLeague(int leagueId)
        {
            var gameCycles = GetGamesQuery().Where(t => t.Stage.LeagueId == leagueId);

            return ParseGameCycles(gameCycles);
        }

        public IEnumerable<GamesCycleDto> GetCyclesByAuditorium(int auditoriumId, int? seasonId)
        {
            var query = GetGamesQuery();

            query = query.Where(g => g.AuditoriumId == auditoriumId && g.IsPublished);

            return ParseGameCycles(query, seasonId);
        }

        public IEnumerable<GamesCycle> GetCyclesByFilterConditions(GameFilterConditions cond, bool userIsEditor, bool ignoreCondIfAllChecked = true)
        {
            var someLeaguesChecked = cond.leagues.Any(l => l.Check);
            var allLeaguesChecked = cond.leagues.All(l => l.Check) && ignoreCondIfAllChecked;
            var someAuditoriumsChecked = cond.auditoriums.Any(a => a.Check);
            var allAuditoriumsChecked = cond.auditoriums.All(a => a.Check) && ignoreCondIfAllChecked;
            var leaguesId = cond.leagues.Where(l => l.Check && l.Id > 0).Select(l => l.Id).ToList();
            var auditoriumsId = cond.auditoriums.Where(a => a.Check && a.Id > 0).Select(a => a.Id).ToList();

            IEnumerable<GamesCycle> result = new List<GamesCycle>();
            if (someLeaguesChecked || someAuditoriumsChecked)
            {
                result = GetGamesQuery()
                    .Where(gc => (userIsEditor || gc.IsPublished)
                    && (gc.Stage.League.SeasonId == cond.seasonId)
                    && (allLeaguesChecked || !someLeaguesChecked || leaguesId.Contains(gc.Stage.LeagueId))
                    && (allAuditoriumsChecked || !someAuditoriumsChecked || auditoriumsId.Contains(gc.AuditoriumId ?? 0))
                    && (!cond.dateFrom.HasValue || gc.StartDate >= cond.dateFrom)
                    && (!cond.dateTo.HasValue || gc.StartDate <= cond.dateTo)
                    )
                    .ToList();
            }
            return result;
        }

        public SchedulesDto GetCyclesByLeagueAndExcludeGames(int leagueId, int? seasonId = null, params int[] gameIds)
        {
            var query = GetGamesQuery();

            if (gameIds.Length == 0)
                query = query.Where(t => t.Stage.LeagueId == leagueId && t.IsPublished);
            else
                query = query.Where(t => t.Stage.LeagueId == leagueId && !gameIds.Contains(t.CycleId) && t.IsPublished);

            var events = db.Events.Where(e => e.LeagueId == leagueId && e.IsPublished)
                .Select(e => new EventDto
                {
                    EventId = e.EventId,
                    LeagueId = e.LeagueId,
                    EventTime = e.EventTime,
                    Title = e.Title,
                    Place = e.Place
                }).ToArray();

            return new SchedulesDto
            {
                GameCycles = ParseGameCycles(query, seasonId, events),
                Events = events
            };
        }

        public IEnumerable<GamesCycleDto> GetCyclesByTeam(int teamId)
        {
            IQueryable<GamesCycle> gameCycles = GetGamesQuery().Where(t => (t.GuestTeamId == teamId || t.HomeTeamId == teamId) && t.IsPublished);
            return ParseGameCycles(gameCycles);
        }

        private IEnumerable<GamesCycleDto> ParseGameCycles(IQueryable<GamesCycle> gameCycles, int? seasonId = null, IEnumerable<EventDto> evnts = null)
        {
            List<GamesCycleDto> games = new List<GamesCycleDto>();
            foreach (var t in gameCycles)
            {
                var dto = new GamesCycleDto
                {
                    AuditoriumId = t.Auditorium?.AuditoriumId ?? 0,
                    Auditorium = t.Auditorium?.Name,
                    AuditoriumAddress = t.Auditorium?.Address,
                    CycleId = t.CycleId,
                    LeagueId = t.Stage.LeagueId,
                    LeagueName = t.Stage.League.Name,
                    LeagueLogo = t.Stage.League.Logo,
                    SectionAlias = t.Stage.League.Union.Section.Alias,
                    GameStatus = t.GameStatus,
                    HomeTeamId = t.HomeTeamId,
                    HomeLogo = t.HomeTeam?.Logo,
                    IsHomeTeamKnown = t.HomeTeam != null,
                    HomeTeamScore = t.HomeTeamScore,
                    GuesTeamId = t.GuestTeamId,
                    GuesLogo = t.GuestTeam?.Logo,
                    IsGuestTeamKnown = t.GuestTeam != null,
                    GuestTeamScore = t.GuestTeamScore,
                    StageName = "שלב " + t.Stage.Number,
                    StartDate = t.StartDate,
                    MaxPlayoffPos = t.MaxPlayoffPos,
                    MinPlayoffPos = t.MinPlayoffPos,
                    EventRef = evnts == null ? null :
                        evnts.Where(ev => ev.LeagueId == t.Stage.LeagueId
                            && ev.EventTime <= t.StartDate)
                            .OrderByDescending(ev => ev.EventTime).FirstOrDefault()
                };
                if (t.GameSets.Any())
                {
                    dto.BasketBallWaterpoloHomeTeamScore = t.GameSets.Sum(x => x.HomeTeamScore);
                    dto.BasketBallWaterpoloGuestTeamScore = t.GameSets.Sum(x => x.GuestTeamScore);

                }

                if (seasonId.HasValue)
                {
                    var guestTeamDetails = t.GuestTeam?.TeamsDetails.FirstOrDefault(f => f.SeasonId == seasonId.Value);
                    if (guestTeamDetails != null)
                    {
                        t.GuestTeam.Title = guestTeamDetails.TeamName;
                    }

                    var homeTeamDetails = t.HomeTeam?.TeamsDetails.FirstOrDefault(f => f.SeasonId == seasonId.Value);
                    if (homeTeamDetails != null)
                    {
                        t.HomeTeam.Title = homeTeamDetails.TeamName;
                    }
                }

                dto.HomeTeam = t.HomeTeam != null ? t.HomeTeam.Title : null;
                dto.GuestTeam = t.GuestTeam != null ? t.GuestTeam.Title : null;

                if (t.Group.IsAdvanced)
                {
                    dto.IsAdvanced = true;
                    dto.Bracket = new BracketDto();
                    dto.Bracket.Id = t.BracketId.Value;
                    dto.Bracket.Type = t.PlayoffBracket.Type;



                    int numOfBrackets = t.Group.PlayoffBrackets.Where(b => b.Type != (int)PlayoffBracketType.Loseer).Count();
                    switch (numOfBrackets)
                    {
                        case 1:
                            dto.StageName = "גמר";
                            break;
                        case 2:
                            dto.StageName = "חצי גמר";
                            break;
                        case 4:
                            dto.StageName = "רבע גמר";
                            break;
                        case 8:
                            dto.StageName = "שמינית גמר";
                            break;
                        default:
                            dto.StageName = (numOfBrackets * 2) + " אחרונות";
                            break;
                    }

                    dto.IndexInBracket = t.PlayoffBracket.GamesCycles.ToList().IndexOf(t);

                    switch (t.PlayoffBracket.Type)
                    {
                        case (int)PlayoffBracketType.Root:
                            dto.IsRoot = true;
                            if (t.HomeTeam == null)
                            {
                                dto.HomeTeam = "--";
                            }
                            if (t.GuestTeam == null)
                            {
                                dto.GuestTeam = "--";
                            }
                            break;
                        case (int)PlayoffBracketType.Winner:
                            if (t.HomeTeam == null)
                            {
                                dto.HomeTeam = "מנצחת";
                            }
                            if (t.GuestTeam == null)
                            {
                                dto.GuestTeam = "מנצחת";
                            }
                            break;
                        case (int)PlayoffBracketType.Loseer:
                            if (t.HomeTeam == null)
                            {
                                dto.HomeTeam = "מפסידה";
                            }
                            if (t.GuestTeam == null)
                            {
                                dto.GuestTeam = "מפסידה";
                            }
                            break;
                    }
                }
                games.Add(dto);
            }

            return games;
        }

        public IQueryable<GamesCycle> GetGamesQuery()
        {
            return _gameCycles
                .OrderBy(t => DbFunctions.TruncateTime(t.StartDate))
                .ThenBy(t => t.Auditorium)
                .ThenBy(t => t.StartDate);
        }

        public IEnumerable<ExcelRefereeDto> GetRefereesExcel(int unionId)
        {
            return db.GamesCycles.Where(t => t.Stage.League.UnionId == unionId)
                .Select(t => new ExcelRefereeDto
                {
                    League = t.Stage.League.Name,
                    StartDate = t.StartDate,
                    HomeTeam = t.HomeTeam.Title,
                    GuestTeam = t.GuestTeam.Title,
                    Auditorium = t.Auditorium.Name,
                    Referee = t.User.FullName
                }).OrderBy(t => t.Referee == null)
                .ThenBy(t => t.Referee)
                .ThenBy(t => t.StartDate).ToList();
        }

        public int ToggleTeams(int cycleId)
        {
            var c = db.GamesCycles.Find(cycleId);
            //Toggle teams
            int? tempId = c.GuestTeamId;
            c.GuestTeamId = c.HomeTeamId;
            c.HomeTeamId = tempId;
            //Toggle Scores
            int tempScore = c.GuestTeamScore;
            c.GuestTeamScore = c.HomeTeamScore;
            c.HomeTeamScore = tempScore;

            foreach (var gs in c.GameSets)
            {
                int tempS = gs.GuestTeamScore;
                gs.GuestTeamScore = gs.HomeTeamScore;
                gs.HomeTeamScore = tempS;
            }

            if (c.AuditoriumId.HasValue)
            {
                c.AuditoriumId = tRepo.GetMainOrFirstAuditoriumForTeam(c.HomeTeamId);
            }
            db.SaveChanges();

            return c.Stage.LeagueId;
        }

        public void Update(GamesCycle gc)
        {
            db.Entry(gc).State = EntityState.Modified;
            db.SaveChanges();
        }

        public void Update(IEnumerable<GamesCycle> gamesCycles)
        {
            foreach (var gameCycle in gamesCycles)
            {
                db.Entry(gameCycle).State = EntityState.Modified;
            }
            db.SaveChanges();
        }

        public GamesCycle GetGameCycleById(int cycleId)
        {
            return db.GamesCycles.Find(cycleId);
        }

        public GameSet GetGameSetById(int gameSetId)
        {
            return db.GameSets.FirstOrDefault(t => t.GameSetId == gameSetId);
        }

        public IEnumerable<GameSet> GetGameSets(int cycleId)
        {
            return db.GameSets.Where(t => t.GameCycleId == cycleId).ToList();
        }

        public IEnumerable<GameSet> GetGameSetsByGameId(int gameId)
        {
            return db.GameSets.Where(t => t.GameCycleId == gameId).ToList();
        }

        public void RemoveCycle(GamesCycle item)
        {
            db.GameSets.RemoveRange(item.GameSets);
            foreach (var user in item.Users)
            {
                user.UsersGamesCycles = null;
            }
            db.GamesCycles.Remove(item);
            item.Users = null;
        }

        public void AddGameSet(GameSet set)
        {
            GamesCycle gc = GetGameCycleById(set.GameCycleId);
            var lastSetNumebr = gc.GameSets.OrderBy(c => c.SetNumber).Select(c => c.SetNumber).LastOrDefault();
            set.SetNumber = ++lastSetNumebr;
            db.GameSets.Add(set);
            db.SaveChanges();
        }

        public void DeleteSet(GameSet set)
        {
            db.GameSets.Remove(set);
            db.SaveChanges();
        }

        public GamesCycle UpdateGameScore(int id)
        {
            GamesCycle gc = GetGameCycleById(id);
            return UpdateGameScore(gc);
        }

        public GamesCycle UpdateBasketBallWaterPoloScore(int id)
        {
            var gc = GetGameCycleById(id);
            return UpdateBasketBallWaterPoloScore(gc);
        }

        public void UpdateWatPoloScore(int id)
        {
            var gc = GetGameCycleById(id);
            UpdateWaterPoloScore(gc);
        }

        public GamesCycle UpdateGameScore(GamesCycle gc)
        {
            int hScore = 0;
            int gScore = 0;
            foreach (var set in gc.GameSets)
            {
                if (set.HomeTeamScore > set.GuestTeamScore)
                {
                    hScore++;

                }
                else if (set.HomeTeamScore < set.GuestTeamScore)
                {
                    gScore++;

                }
            }
            gc.HomeTeamScore = hScore;
            gc.GuestTeamScore = gScore;
            Update(gc);
            return gc;
        }

        public GamesCycle UpdateBasketBallWaterPoloScore(GamesCycle gc)
        {
            var lastQuarter = gc.GameSets.OrderByDescending(x => x.GameSetId).FirstOrDefault();
            if (lastQuarter != null)
            {
                if (lastQuarter.HomeTeamScore > lastQuarter.GuestTeamScore)
                {
                    gc.HomeTeamScore = 1;
                    gc.GuestTeamScore = 0;
                }
                else if (lastQuarter.HomeTeamScore < lastQuarter.GuestTeamScore)
                {
                    gc.GuestTeamScore = 1;
                    gc.HomeTeamScore = 0;
                }
            }
            else
            {
                gc.HomeTeamScore = 0;
                gc.GuestTeamScore = 0;
            }

            Update(gc);
            return gc;
        }

        private void UpdateWaterPoloScore(GamesCycle gc)
        {
            if (gc.GameSets.Count > 1)
            {
                var lastSet = gc.GameSets.OrderBy(c => c.SetNumber).LastOrDefault();
                if (lastSet != null)
                {
                    DeleteSet(lastSet);
                }
            }


            int hScore = 0;
            int gScore = 0;
            foreach (var set in gc.GameSets)
            {
                if (set.HomeTeamScore > set.GuestTeamScore)
                {
                    hScore++;
                    set.GuestTeamScore = 0;
                    set.HomeTeamScore = 5;
                }
                else if (set.HomeTeamScore < set.GuestTeamScore)
                {
                    gScore++;
                    set.GuestTeamScore = 5;
                    set.HomeTeamScore = 0;
                }
            }
            gc.HomeTeamScore = hScore;
            gc.GuestTeamScore = gScore;

            Update(gc);
        }

        public GamesCycle StartGame(int id)
        {
            GamesCycle gc = GetGameCycleById(id);
            gc.GameStatus = GameStatus.Started;
            Update(gc);
            return gc;
        }

        public GamesCycle EndGame(int id)
        {
            GamesCycle gc = GetGameCycleById(id);
            return EndGame(gc);
        }

        public GamesCycle EndGame(GamesCycle gc)
        {
            gc.GameStatus = GameStatus.Ended;
            UpdateGameScore(gc);
            bracketsRepo.GameEndedEvent(gc);
            return gc;
        }

        public void TechnicalWin(int gameCycleId, int teamId)
        {
            GamesCycle gc = GetGameCycleById(gameCycleId);
            db.GameSets.RemoveRange(gc.GameSets);
            db.SaveChanges();
            for (int i = 0; i < 2; i++)
            {
                GameSet set = null;
                if (teamId == gc.HomeTeamId)
                {
                    set = new GameSet
                    {
                        HomeTeamScore = 25,
                        GuestTeamScore = 0,
                        GameCycleId = gameCycleId
                    };
                }
                else
                {
                    set = new GameSet
                    {
                        HomeTeamScore = 0,
                        GuestTeamScore = 25,
                        GameCycleId = gameCycleId
                    };
                }
                AddGameSet(set);
            }
            gc.TechnicalWinnnerId = teamId;
            EndGame(gc);
        }

        public void WaterPoloTechnicalWin(int gameCycleId, int teamId)
        {
            GamesCycle gc = GetGameCycleById(gameCycleId);
            db.GameSets.RemoveRange(gc.GameSets);
            db.SaveChanges();

            GameSet set = null;
            if (teamId == gc.HomeTeamId)
            {
                set = new GameSet
                {
                    HomeTeamScore = 5,
                    GuestTeamScore = 0,
                    GameCycleId = gameCycleId
                };
            }
            else
            {
                set = new GameSet
                {
                    HomeTeamScore = 0,
                    GuestTeamScore = 5,
                    GameCycleId = gameCycleId
                };
            }
            AddGameSet(set);
            gc.TechnicalWinnnerId = teamId;
            EndGame(gc);
        }

        public void BasketBallTechnicalWin(int gameCycleId, int teamId)
        {
            GamesCycle gc = GetGameCycleById(gameCycleId);
            db.GameSets.RemoveRange(gc.GameSets);
            db.SaveChanges();

            GameSet set = null;
            if (teamId == gc.HomeTeamId)
            {
                set = new GameSet
                {
                    HomeTeamScore = 20,
                    GuestTeamScore = 0,
                    GameCycleId = gameCycleId
                };
            }
            else
            {
                set = new GameSet
                {
                    HomeTeamScore = 0,
                    GuestTeamScore = 20,
                    GameCycleId = gameCycleId
                };
            }
            AddGameSet(set);
            gc.TechnicalWinnnerId = teamId;
            EndGame(gc);
        }

        public void UpdateGameSet(GameSet set)
        {
            db.GameSets.Attach(set);
            var entry = db.Entry(set);
            entry.State = EntityState.Modified;
            db.SaveChanges();
        }

        public void ResetGame(int id)
        {
            GamesCycle gc = GetGameCycleById(id);
            ResetGame(gc);
        }

        public void ResetGame(GamesCycle gc, bool includingTeams = false)
        {
            gc.TechnicalWinnnerId = null;
            db.GameSets.RemoveRange(gc.GameSets);
            gc.HomeTeamScore = 0;
            gc.GuestTeamScore = 0;
            if (gc.PlayoffBracket != null)
            {
                gc.PlayoffBracket.WinnerId = null;
                gc.PlayoffBracket.LoserId = null;
            }
            if (includingTeams)
            {
                gc.HomeTeamId = null;
                gc.GuestTeamId = null;
            }
            bracketsRepo.GameEndedEvent(gc);
            gc.GameStatus = null;
            db.SaveChanges();
        }

        internal void SaveGames(List<GamesCycle> gamesList)
        {
            db.GamesCycles.AddRange(gamesList.ToList());
            db.SaveChanges();
        }

        internal void ValidateTimePlaceContradictions(List<GamesCycle> gamesList, TimeSpan gamesInterval)
        {
            foreach (var gameC in gamesList)
            {
                DateTime nGameDate = gameC.StartDate;
                while (db.GamesCycles.Any(g =>
                    g.StartDate == nGameDate &&
                    g.AuditoriumId == gameC.AuditoriumId &&
                    g.AuditoriumId != null &&
                    g.AuditoriumId != 0)
                    ||
                    gamesList.Any(g =>
                    g.StartDate == nGameDate &&
                    g.AuditoriumId == gameC.AuditoriumId &&
                    g.AuditoriumId != null &&
                    g.AuditoriumId != 0 &&
                    g != gameC))
                {
                    nGameDate = nGameDate.Add(gamesInterval);
                }
                gameC.StartDate = nGameDate;
            }
        }

        internal void AddGame(GamesCycle item)
        {
            db.GamesCycles.Add(item);
            db.SaveChanges();
        }

        public IEnumerable<GamesCycle> GetGroupsCyclesByGameIds(int[] gameIds)
        {
            return _gameCycles
                .Where(t => gameIds.Contains(t.CycleId))
                .OrderBy(g => g.StartDate)
                .ThenBy(g => g.Group.Name)
                .ThenBy(g => g.CycleNum)
                .ToList();
        }

        public void UpdateGroupCyclesFromExcelImport(List<ExcelGameDto> cyclesForUpdate)
        {
            foreach (var gamesCycle in cyclesForUpdate)
            {
                var gc = db.GamesCycles.FirstOrDefault(g => g.CycleId == gamesCycle.GameId);
                if (gc != null)
                {
                    UpdateIfDifferentObject(gamesCycle, gc);
                }
            }
            db.SaveChanges();
        }

        private void UpdateIfDifferentObject(ExcelGameDto dto, GamesCycle ctxModel)
        {
            #region If/Else
            if (ctxModel.StartDate != dto.Date)
            {
                ctxModel.StartDate = dto.Date;
            }

            else if (ctxModel.Auditorium != null && ctxModel.Auditorium.Name != dto.Auditorium)
            {
                ctxModel.StartDate = dto.Date;
            }

            else if (ctxModel.Group != null && ctxModel.Group.Name != dto.Groupe)
            {
                ctxModel.StartDate = dto.Date;
            }

            else if (ctxModel.GuestTeam != null && ctxModel.GuestTeam.Title != dto.GuestTeam)
            {
                ctxModel.StartDate = dto.Date;
            }

            else if (ctxModel.GuestTeamScore != dto.GuestTeamScore)
            {
                ctxModel.StartDate = dto.Date;
            }
            else if (ctxModel.HomeTeamScore != dto.HomeTeamScore)
            {
                ctxModel.StartDate = dto.Date;
            }
            else if (ctxModel.User != null && ctxModel.User.FullName != dto.Referee)
            {
                ctxModel.StartDate = dto.Date;
            }
            else if (ctxModel.HomeTeam != null && ctxModel.HomeTeam.Title != dto.HomeTeam)
            {
                ctxModel.StartDate = dto.Date;
            }
            else if (ctxModel.GameSets.Any())
            {
                var sets = ctxModel.GameSets.ToArray();

                var set1 = sets.Length > 0 && sets[0].HomeTeamScore > 0 && sets[0].GuestTeamScore > 0 ? string.Format("{0} - {1}", sets[0].HomeTeamScore, sets[0].GuestTeamScore) : "";
                var set2 = sets.Length > 1 && sets[1].HomeTeamScore > 0 && sets[1].GuestTeamScore > 0 ? string.Format("{0} - {1}", sets[1].HomeTeamScore, sets[1].GuestTeamScore) : "";
                var set3 = sets.Length > 2 && sets[2].HomeTeamScore > 0 && sets[2].GuestTeamScore > 0 ? string.Format("{0} - {1}", sets[2].HomeTeamScore, sets[2].GuestTeamScore) : "";
                if (set1 != dto.Set1 || set2 != dto.Set2 || set3 != dto.Set3)
                {
                    ctxModel.StartDate = dto.Date;
                }
            }
            #endregion
        }


        public void UpdateGamesDate(List<ExcelGameDto> games)
        {
            foreach (var game in games)
            {
                var entity = db.GamesCycles.FirstOrDefault(g => g.CycleId == game.GameId);

                if (entity != null)
                {
                    var date = game.Date.Date;
                    TimeSpan time;
                    if (TimeSpan.TryParse(game.Time, out time))
                    {
                        date = date.AddHours(time.Hours).AddMinutes(time.Minutes);
                        entity.StartDate = date;
                    }
                }
            }
            db.SaveChanges();
        }

        public void UpdateTeamStandingsFromModel(List<StandingDTO> standings, int standingId, string newUrl)
        {
            //TODO clear previous data if url is different.
            var existedTeamStandingGame = db.TeamStandings.Where(x => x.TeamStandingGamesId == standingId && x.TeamStandingGame.GamesUrl != newUrl).ToList();
            if (existedTeamStandingGame.Count > 0)
            {
                db.TeamStandings.RemoveRange(existedTeamStandingGame);
                Save();
            }


            foreach (var standing in standings)
            {
                var strToIntRank = Convert.ToInt32(standing.Rank.Replace(".", string.Empty));

                var dbStanding =
                    db.TeamStandings.FirstOrDefault(
                        x =>
                            x.Team == standing.Team && x.TeamStandingGamesId == standingId);

                //if we find the team update it
                if (dbStanding != null)
                {
                    dbStanding.Rank = strToIntRank;
                    dbStanding.Games = standing.Games.ToByte();
                    dbStanding.Wins = standing.Win.ToByte();
                    dbStanding.Lost = standing.Lost.ToByte();
                    dbStanding.Pts = standing.Pts.ToByte();
                    dbStanding.Papf = standing.PaPf;
                    dbStanding.Home = standing.Home;
                    dbStanding.Road = standing.Road;
                    dbStanding.ScoreHome = standing.ScoreHome;
                    dbStanding.ScoreRoad = standing.ScoreRoad;
                    dbStanding.Last5 = standing.Last5;
                    dbStanding.PlusMinusField = standing.PlusMinusField;
                }
                //else create new teamStanding
                else
                {
                    var newTeamStnading = new TeamStanding
                    {
                        Team = standing.Team,
                        Rank = strToIntRank,
                        Games = standing.Games.ToByte(),
                        Wins = standing.Win.ToByte(),
                        Lost = standing.Lost.ToByte(),
                        Pts = standing.Pts.ToByte(),
                        Papf = standing.PaPf,
                        Home = standing.Home,
                        Road = standing.Road,
                        ScoreHome = standing.ScoreHome,
                        ScoreRoad = standing.ScoreRoad,
                        Last5 = standing.Last5,
                        TeamStandingGamesId = standingId,
                        PlusMinusField = standing.PlusMinusField
                    };



                    db.TeamStandings.Add(newTeamStnading);
                }
            }
            try
            {
                Save();
            }

            catch (DbEntityValidationException e)
            {

            }
            catch (Exception)
            {

                throw;
            }
        }

        public void UpdateTeamStandingsFromScrapper(IList<string> gamesUrl)
        {
            var standings = new List<StandingDTO>();
            var service = new ScrapperService();

            foreach (var standingUrl in gamesUrl)
            {
                var items = service.StandingScraper(standingUrl);
                standings.AddRange(items);
            }

            foreach (var standing in standings)
            {
                //var strToIntRank = Convert.ToInt32(standing.Rank.Replace(".", string.Empty));
                int outVal;
                int.TryParse(standing.Rank.Replace(".", string.Empty), out outVal);

                var dbStanding =
                    db.TeamStandings.FirstOrDefault(
                        x =>
                            x.Team == standing.Team);

                //if we find the team update it
                if (dbStanding != null)
                {
                    dbStanding.Rank = outVal;
                    dbStanding.Games = standing.Games.ToByte();
                    dbStanding.Wins = standing.Win.ToByte();
                    dbStanding.Lost = standing.Lost.ToByte();
                    dbStanding.Pts = standing.Pts.ToByte();
                    dbStanding.Papf = standing.PaPf;
                    dbStanding.Home = standing.Home;
                    dbStanding.Road = standing.Road;
                    dbStanding.ScoreHome = standing.ScoreHome;
                    dbStanding.ScoreRoad = standing.ScoreRoad;
                    dbStanding.Last5 = standing.Last5;
                    dbStanding.PlusMinusField = standing.PlusMinusField;
                }
                //else create new teamStanding
                else
                {
                    var standingGameId = db.TeamStandingGames.FirstOrDefault(x => x.GamesUrl == standing.Url)?.Id;

                    var newTeamStnading = new TeamStanding
                    {
                        Team = standing.Team,
                        Rank = outVal,
                        Games = standing.Games.ToByte(),
                        Wins = standing.Win.ToByte(),
                        Lost = standing.Lost.ToByte(),
                        Pts = standing.Pts.ToByte(),
                        Papf = standing.PaPf,
                        Home = standing.Home,
                        Road = standing.Road,
                        ScoreHome = standing.ScoreHome,
                        ScoreRoad = standing.ScoreRoad,
                        Last5 = standing.Last5,
                        TeamStandingGamesId = standingGameId,
                        PlusMinusField = standing.PlusMinusField,
                    };



                    db.TeamStandings.Add(newTeamStnading);
                }
            }
            try
            {
                Save();
                service.Quit();
            }

            catch (DbEntityValidationException e)
            {

            }
            catch (Exception)
            {

                throw;
            }

        }

        public void UpdateGamesSchedulesFromDto(List<SchedulerDTO> gameCycles, int clubId, int scheduleId, string newUrl)
        {
            var existedSchedulerGame = db.TeamScheduleScrappers.Where(x => x.SchedulerScrapperGamesId == scheduleId && x.TeamScheduleScrapperGame.GameUrl != newUrl).ToList();

            if (existedSchedulerGame.Count > 0)
            {
                db.TeamScheduleScrappers.RemoveRange(existedSchedulerGame);
                Save();
            }

            foreach (var game in gameCycles)
            {
                var startDate = DateTime.Parse(game.Time, new CultureInfo("he-IL"));
                var dbSchedule = db.TeamScheduleScrappers.FirstOrDefault(x => x.HomeTeam == game.HomeTeam && x.GuestTeam == game.GuestTeam && x.StartDate == startDate && x.SchedulerScrapperGamesId == scheduleId);
                if (dbSchedule != null)
                {
                    dbSchedule.Auditorium = game.Auditorium;
                    dbSchedule.Score = string.Format("{0}:{1}", game.HomeTeamScore, game.GuestTeamScore);
                }
                else
                {
                    var newGame = new TeamScheduleScrapper
                    {
                        Auditorium = game.Auditorium,
                        GuestTeam = game.GuestTeam,
                        HomeTeam = game.HomeTeam,
                        SchedulerScrapperGamesId = scheduleId,
                        Score = string.Format("{0}:{1}", game.HomeTeamScore, game.GuestTeamScore),
                        StartDate = startDate,
                    };
                    db.TeamScheduleScrappers.Add(newGame);
                }
            }
            Save();
        }

        public void UpdateGamesSchedulersFromScrapper(List<string> gamesUrl)
        {
            var gameCycles = new List<SchedulerDTO>();

            var service = new ScrapperService();

            foreach (var gameUrl in gamesUrl)
            {
                var gameCycle = service.SchedulerScraper(gameUrl);
                gameCycles.AddRange(gameCycle);

            }

            var gamesToUpdate = db.TeamScheduleScrapperGames.Where(x => gamesUrl.Contains(x.GameUrl)).SelectMany(x => x.TeamScheduleScrappers).ToList();
            foreach (var game in gameCycles)
            {
                DateTime outDate;
                DateTime.TryParse(game.Time, new CultureInfo("he-IL"), DateTimeStyles.None, out outDate);
                var existedGame = gamesToUpdate.FirstOrDefault(x => x.GuestTeam == game.GuestTeam && x.HomeTeam == game.HomeTeam && x.Auditorium == game.Auditorium);
                if (existedGame != null)
                {
                    existedGame.Score = string.Format("{0}:{1}", game.HomeTeamScore, game.GuestTeamScore);
                }
                else
                {
                    var schedullerScrapperId = db.TeamScheduleScrapperGames.FirstOrDefault(x => x.GameUrl == game.Url)?.Id;
                    if (schedullerScrapperId.HasValue)
                    {
                        var newGame = new TeamScheduleScrapper
                        {
                            Auditorium = game.Auditorium,
                            GuestTeam = game.GuestTeam,
                            HomeTeam = game.HomeTeam,
                            SchedulerScrapperGamesId = schedullerScrapperId.Value,
                            Score = string.Format("{0}:{1}", game.HomeTeamScore, game.GuestTeamScore),
                            StartDate = outDate,

                        };

                        db.TeamScheduleScrappers.Add(newGame);
                    }

                }
            }
            Save();
            service.Quit();
        }

        public int SaveTeamGameUrl(int teamId, string gameUrl, int clubId, string externalTeamName)
        {
            try
            {
                var teamShcedule = db.TeamScheduleScrapperGames.FirstOrDefault(x => x.ClubId == clubId && x.TeamId == teamId);
                if (teamShcedule != null)
                {
                    teamShcedule.GameUrl = gameUrl;
                    teamShcedule.ExternalTeamName = externalTeamName;
                    db.SaveChanges();
                    return teamShcedule.Id;
                }
                else
                {
                    var newTeamSchedule = new TeamScheduleScrapperGame
                    {
                        ClubId = clubId,
                        GameUrl = gameUrl,
                        TeamId = teamId,
                        ExternalTeamName = externalTeamName
                    };
                    db.TeamScheduleScrapperGames.Add(newTeamSchedule);
                    db.SaveChanges();

                    return newTeamSchedule.Id;
                }


            }
            catch (Exception)
            {

                return -1;
            }
        }
    }
}

