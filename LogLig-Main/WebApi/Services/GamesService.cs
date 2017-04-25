using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using AppModel;
using WebApi.Models;
using System.Linq.Expressions;
using DataService;
using Omu.ValueInjecter;
using Resources;
using WebApi.Exceptions;
using DataService.DTO;

namespace WebApi.Services
{
    public static class GamesService
    {
        private static readonly GamesRepo _gamesRepo = new GamesRepo();

        public static GameSetViewModel CreateGameSet(CreateGameSetViewModel viewModel)
        {
            GamesCycle gamesCycle = _gamesRepo.GetGameCycleById(viewModel.GameCycleId);

            if (gamesCycle == null) throw new NotFoundEntityException("Game cycle with this Id doesn't exist");
            if (!gamesCycle.IsPublished) throw new NotFoundEntityException("Game cycle with this Id doesn't published");

            GameSet gameSet = new GameSet();
            gameSet.InjectFrom(viewModel);
            _gamesRepo.AddGameSet(gameSet);

            GameSetViewModel gameSetViewModel = new GameSetViewModel();
            gameSetViewModel.InjectFrom(gameSet);

            return gameSetViewModel;
        }

        public static void UpdateGameSet(int gameSetId, CreateGameSetViewModel viewModel)
        {
            viewModel.GameSetId = gameSetId;

            GameSet gameSet = _gamesRepo.GetGameSetById(gameSetId);
            if (gameSet == null) throw new NotFoundEntityException("Game set with this Id doesn't exist");

            GamesCycle gamesCycle = _gamesRepo.GetGameCycleById(viewModel.GameCycleId);
            if (gamesCycle == null) throw new NotFoundEntityException("Game cycle with this Id doesn't exist");
            if (!gamesCycle.IsPublished) throw new NotFoundEntityException("Game cycle with this Id doesn't published");

            gameSet.InjectFrom(viewModel);
            _gamesRepo.UpdateGameSet(gameSet);
        }

        public static List<UserBaseViewModel> GetGoingFans(int cycleId, int currentUserId)
        {
            using (DataEntities db = new DataEntities())
            {
                var result = new List<UserBaseViewModel>();
                var games = db.GamesCycles.Where(x => x.IsPublished).ToList();
                foreach (var g in games.Where(x => x.CycleId == cycleId))
                {
                    foreach (var gu in g.Users)
                    {
                        foreach (var us in gu.Friends.Where(t => t.FriendId == currentUserId && t.UserId == gu.UserId).DefaultIfEmpty())
                        {
                            foreach (var usf in gu.UsersFriends.Where(t => t.UserId == currentUserId && t.FriendId == gu.UserId).DefaultIfEmpty())
                            {
                                result.Add(new UserBaseViewModel
                                {
                                    Id = gu.UserId,
                                    UserName = gu.UserName,
                                    FullName = gu.FullName,
                                    Image = gu.Image,
                                    CanRcvMsg = true,
                                    UserRole = gu.UsersType.TypeRole,
                                    FriendshipStatus = (us == null && usf == null) ? FriendshipStatus.No :
                            ((us != null && us.IsConfirmed) || (usf != null && usf.IsConfirmed)) ? FriendshipStatus.Yes :
                            FriendshipStatus.Pending
                                }

                                    );
                            }
                        }
                    }
                }
                return result;
                //return (from g in db.GamesCycles
                //        from gu in g.Users
                //        from us in gu.Friends.Where(t => t.FriendId == currentUserId && t.UserId == gu.UserId).DefaultIfEmpty()
                //        from usf in gu.UsersFriends.Where(t => t.UserId == currentUserId && t.FriendId == gu.UserId).DefaultIfEmpty()
                //        where g.CycleId == cycleId
                //        select new UserBaseViewModel
                //        {
                //            Id = gu.UserId,
                //            UserName = gu.UserName,
                //            FullName = gu.FullName,
                //            Image = gu.Image,
                //            CanRcvMsg = true,
                //            UserRole = gu.UsersType.TypeRole,
                //            FriendshipStatus = (us == null && usf == null) ? FriendshipStatus.No :
                //            ((us != null && us.IsConfirmed) || (usf != null && usf.IsConfirmed)) ? FriendshipStatus.Yes :
                //            FriendshipStatus.Pending
                //        }).ToList();
            }

            //DataEntities db = new DataEntities();
            //if (currentUserId == 0)
            //{
            //    return db.GamesCycles.Find(cycleId).Users
            //        .Where(u => u.IsActive == true && u.IsArchive == false)
            //        .Select(u =>
            //            new UserBaseViewModel
            //            {
            //                Id = u.UserId,
            //                UserName = u.UserName,
            //                Image = u.Image,
            //                IsFriend = false
            //            });
            //}
            //else
            //{
            //    var fans = db.GamesCycles.Find(cycleId).Users
            //        .Where(u => u.IsActive == true && u.IsArchive == false).
            //        Select(u =>
            //            new UserBaseViewModel
            //            {
            //                Id = u.UserId,
            //                UserName = u.UserName,
            //                Image = u.Image
            //            }).ToList();

            //    FriendsService.AreFansFriends(fans, currentUserId);
            //    return fans;
            //}
        }

        public static NextGameViewModel GetNextGame(IEnumerable<GamesCycle> games, int currentUserId, int leagueId, int? seasonId = null)
        {
            GamesCycle gameCycle = games.Where(g => g.GameStatus == GameStatus.Started && g.IsPublished).OrderBy(g => g.StartDate).FirstOrDefault();
            if (gameCycle == null)
            {
                gameCycle = games.Where(g => g.StartDate >= DateTime.Now && g.IsPublished).OrderBy(g => g.StartDate).FirstOrDefault();
            }

            if (gameCycle == null)
            {
                return null;
            }

            return ParseNextGameCycle(currentUserId, gameCycle, leagueId, seasonId);
        }

        public static GameViewModel GetLastGame(IEnumerable<GamesCycle> games, int? seasonId = null)
        {
            GamesCycle gameCycle = games.Where(g => g.GameStatus == GameStatus.Ended && g.IsPublished).OrderBy(g => g.StartDate).LastOrDefault();
            if (gameCycle == null)
            {
                return null;
            }
            GameViewModel vm = ParseGameCycle(gameCycle, seasonId);
            return vm;
        }

        public static IEnumerable<GameViewModel> GetNextGames(IEnumerable<GamesCycle> leagueGames, DateTime fromDate, int? seasonId = null)
        {
            var temp = leagueGames.Where(g => g.IsPublished && (g.GameStatus == GameStatus.Next || g.GameStatus == null))
                .OrderBy(g => g.StartDate)
                .ToList()
                .Select(g => ParseGameCycle(g, seasonId));
            return temp;
        }

        public static IEnumerable<GameViewModel> GeTeamNextGames(int leagueId, int teamId, DateTime fromDate, int? seasonId)
        {
            using (var db = new DataEntities())
            {
                Func<GameDto, bool> predicate = g => g.StartDate > fromDate &&
                                                    g.LeagueId == leagueId &&
                                                    (g.HomeTeamId == teamId || g.GuestTeamId == teamId) && g.IsPublished;
                List<GameViewModel> teamNextGames = GetGamesQuery(db)
                                                     .Where(predicate)
                                                     .Select(x => CycleToGame(x, seasonId))
                                                     .OrderBy(g => g.StartDate)
                                                     .ToList();
                return teamNextGames;
            }
        }

        public static IEnumerable<GameViewModel> GetLastGames(IEnumerable<GamesCycle> leagueGames, int? seasonId)
        {
            return leagueGames.Where(g => g.StartDate < DateTime.Now)
                .Where(g => g.GuestTeamId.HasValue && g.HomeTeamId.HasValue && g.IsPublished)
                .OrderByDescending(g => g.StartDate)
                .ToList()
                .Select(g => ParseGameCycle(g, seasonId));
        }

        public static IEnumerable<User> GetGoingFriends(int gameId, User currentUser)
        {
            using (DataEntities db = new DataEntities())
            {
                var usersList = (from c in db.GamesCycles
                                 from u in c.Users
                                 where c.CycleId == gameId && c.IsPublished
                                 select u.UserId).ToList();

                var friends = FriendsService.GetAllConfirmedFriendsAsUsers(currentUser);
                return friends.Where(t => usersList.Contains(t.UserId)).ToList();
            }
        }

        internal static GameViewModel ParseGameCycle(int gameId)
        {
            using (DataEntities db = new DataEntities())
            {
                var game = db.GamesCycles.Include(t => t.Auditorium).FirstOrDefault(t => t.CycleId == gameId && t.IsPublished);
                if (game != null)
                {
                    return ParseGameCycle(game);
                }
            }
            return null;
        }

        public static Expression<Func<GamesCycle, GameViewModel>> CycleToGame()
        {
            return c => new GameViewModel
            {
                GameId = c.CycleId,
                GameCycleStatus = c.GameStatus,
                StartDate = c.StartDate,
                HomeTeamId = c.HomeTeamId,
                HomeTeam = c.HomeTeam.Title,
                HomeTeamScore = c.HomeTeamScore,
                GuestTeam = c.GuestTeam.Title,
                GuestTeamId = c.GuestTeamId,
                GuestTeamScore = c.GuestTeamScore,
                Auditorium = c.Auditorium.Name,
                HomeTeamLogo = c.HomeTeam.Logo,
                GuestTeamLogo = c.GuestTeam.Logo,
                CycleNumber = c.CycleNum,
                LeagueId = c.Stage.LeagueId,
                LeagueName = c.Stage.League.Name
            };
        }

        public static GameViewModel CycleToGame(GamesCycle gamesCycle)
        {
            var vm = new GameViewModel
            {
                GameId = gamesCycle.CycleId,
                GameCycleStatus = gamesCycle.GameStatus,
                StartDate = gamesCycle.StartDate,
                HomeTeamId = gamesCycle.HomeTeamId,
                HomeTeam = gamesCycle.HomeTeam.Title,
                HomeTeamScore = gamesCycle.HomeTeamScore,
                GuestTeam = gamesCycle.GuestTeam.Title,
                GuestTeamId = gamesCycle.GuestTeamId,
                GuestTeamScore = gamesCycle.GuestTeamScore,
                Auditorium = gamesCycle.Auditorium.Name,
                HomeTeamLogo = gamesCycle.HomeTeam.Logo,
                GuestTeamLogo = gamesCycle.GuestTeam.Logo,
                CycleNumber = gamesCycle.CycleNum,
                LeagueId = gamesCycle.Stage.LeagueId,
                LeagueName = gamesCycle.Stage.League.Name
            };

            return vm;
        }

        internal static GameViewModel ParseGameCycle(GamesCycle gameCycle, int? seasondId = null)
        {
            if (gameCycle != null)
            {

                int typePlayOff = 2;
                if (gameCycle.PlayoffBracket != null)
                    typePlayOff = gameCycle.PlayoffBracket.Type;

                var gameModel = new GameViewModel
                {
                    HouseName = gameCycle.Group != null ? gameCycle.Group.Name : "",
                    GroupName = gameCycle.Auditorium != null ? gameCycle.Auditorium.Name : "",
                    GameId = gameCycle.CycleId,
                    GameCycleStatus = gameCycle.GameStatus ?? "next",
                    StartDate = gameCycle.StartDate,
                    HomeTeamId = gameCycle.HomeTeamId,
                    HomeTeam = !gameCycle.HomeTeamId.HasValue ? (typePlayOff == 2 ? "Winner" : "Loser") : gameCycle.HomeTeam.Title,
                    HomeTeamScore = gameCycle.HomeTeamScore,
                    GuestTeam = !gameCycle.GuestTeamId.HasValue ? (typePlayOff == 2 ? "Winner" : "Loser") : gameCycle.GuestTeam.Title,
                    GuestTeamId = gameCycle.GuestTeamId,
                    GuestTeamScore = gameCycle.GuestTeamScore,
                    Auditorium = gameCycle.Auditorium != null ? gameCycle.Auditorium.Name : null,
                    HomeTeamLogo = gameCycle.HomeTeam != null ? gameCycle.HomeTeam.Logo : string.Empty,
                    GuestTeamLogo = gameCycle.GuestTeam != null ? gameCycle.GuestTeam.Logo : string.Empty,
                    CycleNumber = gameCycle.CycleNum,
                    MaxPlayoffPos = gameCycle.MaxPlayoffPos,
                    MinPlayoffPos = gameCycle.MinPlayoffPos
                };

                var alias = gameCycle?.Stage?.League?.Union?.Section?.Alias;
                switch (alias)
                {
                    case GamesAlias.WaterPolo:
                    case GamesAlias.BasketBall:
                        if (gameCycle.GameSets.Any())
                        {
                            gameModel.HomeTeamScore = gameCycle.GameSets.Sum(x => x.HomeTeamScore);
                            gameModel.GuestTeamScore = gameCycle.GameSets.Sum(x => x.GuestTeamScore);
                        }
                        break;
                }


                if (gameCycle.Group != null && gameCycle.Group.IsAdvanced && gameCycle.Group.PlayoffBrackets != null)
                {
                    int numOfBrackets = gameCycle.Group.PlayoffBrackets.Count;
                    switch (numOfBrackets)
                    {
                        case 1:
                            gameModel.PlayOffType = Messages.Final;
                            break;
                        case 2:
                            gameModel.PlayOffType = Messages.Semifinals;
                            break;
                        case 4:
                            gameModel.PlayOffType = Messages.Quarter_finals;
                            break;
                        case 8:
                            gameModel.PlayOffType = Messages.Final_Eighth;
                            break;
                        default:
                            gameModel.PlayOffType = (numOfBrackets * 2) + " " + Messages.FinalNumber;
                            break;
                    }
                }

                if (seasondId.HasValue && gameCycle.GuestTeam != null && gameCycle.HomeTeam != null)
                {
                    TeamsDetails homeTeamsDetails = gameCycle.HomeTeam.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasondId);
                    gameModel.HomeTeam = homeTeamsDetails != null ? homeTeamsDetails.TeamName : gameCycle.HomeTeam.Title;

                    TeamsDetails guestTeamsDetails = gameCycle.GuestTeam.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasondId);
                    gameModel.GuestTeam = guestTeamsDetails != null ? guestTeamsDetails.TeamName : gameCycle.GuestTeam.Title;
                }
                else
                {
                    gameModel.HomeTeam = !gameCycle.HomeTeamId.HasValue ? (typePlayOff == 2 ? "Winner" : "Loser") : gameCycle.HomeTeam.Title;
                    gameModel.GuestTeam = !gameCycle.GuestTeamId.HasValue ? (typePlayOff == 2 ? "Winner" : "Loser") : gameCycle.GuestTeam.Title;
                }

                return gameModel;
            }
            return null;
        }

        internal static List<GameViewModel> GetGameHistory(int? guestId, int? homeId, int? seasonId = null)
        {
            using (var db = new DataEntities())
            {
                Func<GameDto, bool> predicate = g => ((g.HomeTeamId == homeId && g.GuestTeamId == guestId) ||
                                                      (g.HomeTeamId == guestId && g.GuestTeamId == homeId)) &&
                                                       g.GameCycleStatus == GameStatus.Ended && g.IsPublished;
                List<GameViewModel> game = GetGamesQuery(db)
                                        .Where(predicate)
                                        .Select(x => CycleToGame(x, seasonId))
                                        .OrderByDescending(g => g.StartDate)
                                        .Take(5)
                                        .ToList();
                return game;
            }
        }

        private static NextGameViewModel ParseNextGameCycle(int currentUserId, GamesCycle gameCycle, int leagueId, int? seasonId = null)
        {
            NextGameViewModel gvm = new NextGameViewModel();
            if (gameCycle != null)
            {
                IEnumerable<UserBaseViewModel> FansList = GetGoingFans(gameCycle.CycleId, currentUserId).OrderByDescending(t => t.FriendshipStatus);
                gvm = new NextGameViewModel
                {
                    LeagueId = leagueId,
                    GameId = gameCycle.CycleId,
                    GameCycleStatus = gameCycle.GameStatus,
                    StartDate = gameCycle.StartDate,
                    HomeTeamId = gameCycle.HomeTeamId,
                    HomeTeamScore = gameCycle.HomeTeamScore,
                    GuestTeamId = gameCycle.GuestTeamId,
                    GuestTeamScore = gameCycle.GuestTeamScore,
                    Auditorium = gameCycle.Auditorium != null ? gameCycle.Auditorium.Name : null,
                    FansList = FansList.ToList(),
                    FriendsGoing = FansList.Count(t => t.FriendshipStatus == FriendshipStatus.Yes),
                    FansGoing = FansList.Count(t => t.FriendshipStatus != FriendshipStatus.Yes),
                    HomeTeamLogo = gameCycle != null && gameCycle.HomeTeam != null ? gameCycle.HomeTeam.Logo : string.Empty,
                    GuestTeamLogo = gameCycle != null && gameCycle.GuestTeam != null ? gameCycle.GuestTeam.Logo : string.Empty,
                    CycleNumber = gameCycle.CycleNum
                };

                var alias = gameCycle?.Stage?.League?.Union?.Section?.Alias;
                switch (alias)
                {
                    case GamesAlias.WaterPolo:
                    case GamesAlias.BasketBall:
                        if (gameCycle.GameSets.Any())
                        {
                            gvm.HomeTeamScore = gameCycle.GameSets.Sum(x => x.HomeTeamScore);
                            gvm.GuestTeamScore = gameCycle.GameSets.Sum(x => x.GuestTeamScore);
                        }
                        break;
                }


                if (gameCycle.Group != null && gameCycle.Group.IsAdvanced && gameCycle.Group.PlayoffBrackets != null)
                {
                    int numOfBrackets = gameCycle.Group.PlayoffBrackets.Count;
                    switch (numOfBrackets)
                    {
                        case 1:
                            gvm.PlayOffType = Messages.Final;
                            break;
                        case 2:
                            gvm.PlayOffType = Messages.Semifinals;
                            break;
                        case 4:
                            gvm.PlayOffType = Messages.Quarter_finals;
                            break;
                        case 8:
                            gvm.PlayOffType = Messages.Final_Eighth;
                            break;
                        default:
                            gvm.PlayOffType = (numOfBrackets * 2) + " " + Messages.FinalNumber;
                            break;
                    }
                }

                var me = gvm.FansList.FirstOrDefault(t => t.Id == currentUserId);
                if (me != null)
                {
                    gvm.IsGoing = 1;
                }

                if (seasonId.HasValue && gameCycle.HomeTeam != null && gameCycle.GuestTeam != null)
                {
                    var homeTeamDetails = gameCycle.HomeTeam.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    gvm.HomeTeam = homeTeamDetails != null ? homeTeamDetails.TeamName : gameCycle.HomeTeam.Title;

                    var guestTeamDetails = gameCycle.GuestTeam.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    gvm.GuestTeam = guestTeamDetails != null ? guestTeamDetails.TeamName : gameCycle.GuestTeam.Title;
                }
                else
                {
                    gvm.HomeTeam = gameCycle.HomeTeam != null ? gameCycle.HomeTeam.Title : string.Empty;
                    gvm.GuestTeam = gameCycle.GuestTeam != null ? gameCycle.GuestTeam.Title : string.Empty;
                }
            }
            return gvm;
        }


        public static IList<GameViewModel> GetPlayerLastGames(int teamId, int? seasonId)
        {
            using (var db = new DataEntities())
            {
                GameViewModel[] resList = new GameViewModel[3];

                var query = (from gameCycle in db.GamesCycles
                             join homeTeam in db.Teams on gameCycle.HomeTeamId equals homeTeam.TeamId
                             join stage in db.Stages on gameCycle.StageId equals stage.StageId
                             join guestTeam in db.Teams on gameCycle.GuestTeamId equals guestTeam.TeamId
                             join auditorium in db.Auditoriums on gameCycle.AuditoriumId equals auditorium.AuditoriumId into aud
                             from gameCycleAuditorium in aud.DefaultIfEmpty()

                             let homeTeamDetails = homeTeam.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)
                             let guestTeamDetails = guestTeam.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)

                             where gameCycle.IsPublished && (gameCycle.GuestTeamId == teamId || gameCycle.HomeTeamId == teamId)
                             select new GameViewModel
                             {
                                 GameId = gameCycle.CycleId,
                                 GameCycleStatus = gameCycle.GameStatus,
                                 StartDate = gameCycle.StartDate,
                                 HomeTeamId = homeTeam.TeamId,
                                 HomeTeam = homeTeamDetails != null ? homeTeamDetails.TeamName : homeTeam.Title,
                                 HomeTeamScore = gameCycle.HomeTeamScore,
                                 GuestTeam = guestTeamDetails != null ? guestTeamDetails.TeamName : guestTeam.Title,
                                 GuestTeamId = guestTeam.TeamId,
                                 GuestTeamScore = gameCycle.GuestTeamScore,
                                 Auditorium = gameCycleAuditorium != null ? gameCycleAuditorium.Name : null,
                                 HomeTeamLogo = homeTeam.Logo,
                                 GuestTeamLogo = guestTeam.Logo,
                                 CycleNumber = gameCycle.CycleNum
                             });

                var lastGame = query.Where(g => g.GameCycleStatus == GameStatus.Ended)
                    .OrderByDescending(g => g.StartDate)
                    .FirstOrDefault();

                if (lastGame == null)
                {
                    lastGame = query.Where(g => g.StartDate <= DateTime.Now)
                        .OrderByDescending(g => g.StartDate)
                        .FirstOrDefault();
                }

                resList[1] = lastGame;

                if (lastGame != null)
                {
                    var prevGame = query.Where(g => g.StartDate < lastGame.StartDate)
                        .OrderByDescending(g => g.StartDate)
                        .FirstOrDefault();

                    resList[0] = prevGame;
                }

                var nextGame = query.Where(g => g.StartDate >= DateTime.Now)
                    .OrderBy(g => g.StartDate)
                    .FirstOrDefault();

                resList[2] = nextGame;

                return resList;
            }
        }

        public static GameViewModel GetGameById(int gameId, int? seasonId = null)
        {
            using (var db = new DataEntities())
            {
                Func<GameDto, bool> predicate = gameCycle => gameCycle.GameId == gameId && gameCycle.IsPublished;
                GameViewModel game = GetGamesQuery(db)
                                        .Where(predicate)
                                        .Select(x => CycleToGame(x, seasonId))
                                        .ToList()
                                        .FirstOrDefault();

                return game;
            }
        }

        public static IQueryable<GameDto> GetGamesQuery(DataEntities db)
        {
            IQueryable<GameDto> gamesQuery = (from gameCycle in db.GamesCycles
                                              join homeTeam in db.Teams on gameCycle.HomeTeamId equals homeTeam.TeamId
                                              join guestTeam in db.Teams on gameCycle.GuestTeamId equals guestTeam.TeamId
                                              join auditorium in db.Auditoriums on gameCycle.AuditoriumId equals auditorium.AuditoriumId into aud
                                              join stage in db.Stages on gameCycle.StageId equals stage.StageId
                                              join league in db.Leagues on stage.LeagueId equals league.LeagueId

                                              from gameCycleAuditorion in aud.DefaultIfEmpty()

                                              select new GameDto()
                                              {
                                                  GameId = gameCycle.CycleId,
                                                  GameCycleStatus = gameCycle.GameStatus,
                                                  StartDate = gameCycle.StartDate,
                                                  HomeTeamId = homeTeam.TeamId,
                                                  HomeTeamTitle = homeTeam.Title,
                                                  HomeTeamScore = gameCycle.HomeTeamScore,
                                                  GuestTeamTitle = guestTeam.Title,
                                                  GuestTeamId = guestTeam.TeamId,
                                                  GuestTeamScore = gameCycle.GuestTeamScore,
                                                  Auditorium = gameCycleAuditorion != null ? gameCycleAuditorion.Name : string.Empty,
                                                  HomeTeamLogo = homeTeam.Logo,
                                                  GuestTeamLogo = guestTeam.Logo,
                                                  CycleNumber = gameCycle.CycleNum,
                                                  LeagueId = stage.LeagueId,
                                                  LeagueName = league.Name,
                                                  IsPublished = gameCycle.IsPublished,
                                                  HomeTeamDetails = homeTeam.TeamsDetails.Select(x => new TeamDetailsDto()
                                                  {
                                                      TeamId = x.TeamId,
                                                      TeamName = x.TeamName,
                                                      SeasonId = x.SeasonId
                                                  }),
                                                  GuestTeamDetails = guestTeam.TeamsDetails.Select(x => new TeamDetailsDto()
                                                  {
                                                      TeamId = x.TeamId,
                                                      TeamName = x.TeamName,
                                                      SeasonId = x.SeasonId
                                                  }),
                                              });

            return gamesQuery;
        }

        public static GameViewModel CycleToGame(GameDto gameDto, int? seasonId = null)
        {
            var vm = new GameViewModel()
            {
                GameId = gameDto.GameId,
                GameCycleStatus = gameDto.GameCycleStatus,
                StartDate = gameDto.StartDate,
                HomeTeamId = gameDto.HomeTeamId,
                HomeTeamScore = gameDto.HomeTeamScore,
                GuestTeamId = gameDto.GuestTeamId,
                GuestTeamScore = gameDto.GuestTeamScore,
                Auditorium = gameDto.Auditorium,
                HomeTeamLogo = gameDto.HomeTeamLogo,
                GuestTeamLogo = gameDto.GuestTeamLogo,
                CycleNumber = gameDto.CycleNumber,
                LeagueId = gameDto.LeagueId,
                LeagueName = gameDto.LeagueName,

            };

            if (seasonId.HasValue)
            {
                TeamDetailsDto homeTeamDetails = gameDto.HomeTeamDetails.FirstOrDefault(x => x.SeasonId == seasonId);
                vm.HomeTeam = homeTeamDetails != null ? homeTeamDetails.TeamName : gameDto.HomeTeamTitle;

                TeamDetailsDto guestTeamDetails = gameDto.GuestTeamDetails.FirstOrDefault(x => x.SeasonId == seasonId);
                vm.GuestTeam = guestTeamDetails != null ? guestTeamDetails.TeamName : gameDto.GuestTeamTitle;
            }
            else
            {
                vm.HomeTeam = gameDto.HomeTeamTitle;
                vm.GuestTeam = gameDto.GuestTeamTitle;
            }

            return vm;
        }

        public static IEnumerable<GameSetViewModel> GetGameSets(int gameId)
        {
            using (var db = new DataEntities())
            {
                return db.GameSets.Where(t => t.GameCycleId == gameId && t.GamesCycle.IsPublished).Select(s =>
                    new GameSetViewModel
                    {
                        GameSetId = s.GameSetId,
                        GameCycleId = s.GameCycleId,
                        HomeTeamScore = s.HomeTeamScore,
                        GuestTeamScore = s.GuestTeamScore,
                        SetNumber = s.SetNumber,
                        IsGoldenSet = s.IsGoldenSet
                    }).OrderBy(s => s.SetNumber).ToList();
            }
        }

        public static void UpdateGameSets(List<GamesCycle> gameCycles, string section)
        {
            switch (section)
            {
                case GamesAlias.WaterPolo:
                case GamesAlias.BasketBall:
                    foreach (var game in gameCycles)
                    {
                        game.HomeTeamScore = game.GameSets.Sum(x => x.HomeTeamScore);
                        game.GuestTeamScore = game.GameSets.Sum(x => x.GuestTeamScore);

                    }
                    break;
            }
        }

        public static void UpdateGameSet(GameViewModel game, string section)
        {
            switch (section)
            {
                case GamesAlias.WaterPolo:
                case GamesAlias.BasketBall:
                    using (var db = new DataEntities())
                    {
                        var searchedGameSet = db.GameSets.Where(x => x.GameCycleId == game.GameId).ToList();
                        if (searchedGameSet.Count > 0)
                        {
                            game.HomeTeamScore = searchedGameSet.Sum(x => x.HomeTeamScore);
                            game.GuestTeamScore = searchedGameSet.Sum(x => x.GuestTeamScore);
                        }
                        
                    }

                    break;
            }
        }

        public static void UpdateGameSets(IEnumerable<GameViewModel> games)
        {
            using (var db = new DataEntities())
            {
                foreach (var game in games)
                {
                    if (game != null)
                    {
                        var section = db.Leagues.FirstOrDefault(x => x.LeagueId == game.LeagueId)?.Union?.Section?.Alias;
                        switch (section)
                        {
                            case GamesAlias.WaterPolo:
                            case GamesAlias.BasketBall:
                                var searchedGameSet = db.GameSets.Where(x => x.GameCycleId == game.GameId).ToList();
                                if (searchedGameSet.Count > 0)
                                {
                                    game.HomeTeamScore = searchedGameSet.Sum(x => x.HomeTeamScore);
                                    game.GuestTeamScore = searchedGameSet.Sum(x => x.GuestTeamScore);
                                }

                                break;
                        }
                    }
                }
            }
        }

        public static IList<GameViewModel> GetPlayerGames(int playerId, int? seasonId)
        {
            using (var db = new DataEntities())
            {
                var playerGames = (from gameCycle in db.GamesCycles
                                   join homeTeam in db.Teams on gameCycle.HomeTeamId equals homeTeam.TeamId
                                   join stage in db.Stages on gameCycle.StageId equals stage.StageId
                                   join guestTeam in db.Teams on gameCycle.GuestTeamId equals guestTeam.TeamId
                                   join auditorium in db.Auditoriums on gameCycle.AuditoriumId equals auditorium.AuditoriumId into aud
                                   from gameCycleAuditorium in aud.DefaultIfEmpty()

                                   let homeTeamDetails = homeTeam.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)
                                   let guestTeamDetails = guestTeam.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)
                                   where gameCycle.IsPublished && (gameCycle.Users.Any(t => t.UserId == playerId))
                                   select new GameViewModel
                                   {
                                       GameId = gameCycle.CycleId,
                                       GameCycleStatus = gameCycle.GameStatus,
                                       StartDate = gameCycle.StartDate,
                                       HomeTeamId = homeTeam.TeamId,
                                       HomeTeam = homeTeamDetails != null ? homeTeamDetails.TeamName : homeTeam.Title,
                                       HomeTeamScore = gameCycle.HomeTeamScore,
                                       GuestTeam = guestTeamDetails != null ? guestTeamDetails.TeamName : guestTeam.Title,
                                       GuestTeamId = guestTeam.TeamId,
                                       GuestTeamScore = gameCycle.GuestTeamScore,
                                       Auditorium = gameCycleAuditorium != null ? gameCycleAuditorium.Name : null,
                                       HomeTeamLogo = homeTeam.Logo,
                                       GuestTeamLogo = guestTeam.Logo,
                                       CycleNumber = gameCycle.CycleNum,
                                       LeagueId = stage.LeagueId
                                   }).Take(4).ToList();
                return playerGames;
            }
        }
    }
}