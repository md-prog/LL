using AppModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataService
{
    public class SchedulingService
    {

        #region Ctor

        private LeagueRepo leagueRepo;
        private StagesRepo stagesRepo;
        private GroupsRepo groupesRepo;
        private BracketsRepo bracketsRepo;
        private GamesRepo gamesRepo;
        private TeamsRepo teamsRepo;

        public SchedulingService()
        {
            DataEntities db = new DataEntities();
            leagueRepo = new LeagueRepo(db);
            stagesRepo = new StagesRepo(db);
            groupesRepo = new GroupsRepo(db);
            gamesRepo = new GamesRepo(db);
            bracketsRepo = new BracketsRepo(db, gamesRepo);
            teamsRepo = new TeamsRepo(db);
        }

        #endregion

        #region Early Groups

        public void ScheduleGames(int leagueId)
        {
            //Find League
            var league = leagueRepo.GetById(leagueId);
            if (league == null)
            {
                return;
            }

            //Find Game Settings
            var games = league.Games.ToList();
            if (games == null)
            {
                return;
            }

            //Get Stages
            var lastStage = stagesRepo.GetLastStageForLeague(league.LeagueId);
            if (lastStage == null)
            {
                return;
            }
            if (lastStage.Groups.Where(t => t.IsArchive == false).Count() == 0)
            {
                return;
            }


            //Delete all games from last stage
            stagesRepo.DeleteAllGameCycles(lastStage.StageId);

            List<GamesCycle> gamesList = new List<GamesCycle>();
            foreach (var group in lastStage.Groups.Where(t => t.IsArchive == false))
            {
                Game game = games.FirstOrDefault(x => x.StageId == group.StageId);
                if (game == null)
                    game = games.FirstOrDefault();
                List<GamesCycle> groupGames = new List<GamesCycle>();
                if (group.TypeId == 1)
                {
                    //Get Settings
                    GameSettings settings = GetGameSettings(league, game);
                    groupGames = ScheduleLeagueGroup(group, settings);
                }
                else if (group.TypeId == 2 || group.TypeId == 3)
                {
                    groupGames = new List<GamesCycle>();
                    SchedulePlayoffGroup(groupGames, group, league, game);
                    SetWinnersAndLosers(group.PlayoffBrackets.ToList());
                }
                ValidateTimePlaceContradictions(groupGames, TimeSpan.Parse(game.GamesInterval));
                gamesList.AddRange(groupGames);
            }

            //UpdateReferee(gamesList, league.LeagueId, league.UnionId);
            UpdateAuditorium(gamesList);
            gamesRepo.Save();
        }



        #endregion

        #region League

        private List<GamesCycle> ScheduleLeagueGroup(Group group, GameSettings settings)
        {
            List<int> groupTeamsIds = group.GroupsTeams.Where(gt => gt.Team.IsArchive == false).OrderBy(gt => gt.Pos).Select(gt => gt.TeamId).ToList();
            int numberOfCycles = group.NumberOfCycles.HasValue ? group.NumberOfCycles.Value : 1;
            List<Tuple<int, int>> combinations = RoundRobin.GetMatches(groupTeamsIds);
            combinations = CreateCombinationCycles(numberOfCycles, combinations);
            List<GamesCycle> groupGames = CreateGamesFromCombinations(combinations);
            groupGames.ForEach(g =>
            {
                g.GroupeId = group.GroupId;
                g.StageId = group.StageId;
            });
            int numberOfTeams = groupTeamsIds.Count;
            int numberOfGamesInRound = numberOfTeams / 2;
            int numberOfRoundsInOneCycle = GetNumberOfRoundsInOneCycle(group.TypeId, numberOfTeams);
            var totalNumberOfRoundes = numberOfRoundsInOneCycle * numberOfCycles;
            SchedualGames(settings.ActualStartDate, settings.SettingsDays, settings.SequenceRounds, numberOfGamesInRound, totalNumberOfRoundes, groupGames);
            SaveGamesList(groupGames);
            return groupGames;
        }

        #endregion

        #region Playoff

        private void SchedulePlayoffGroup(List<GamesCycle> games, Group group, League league, Game game, int stagesLeft = 0)
        {

            int numberOfCycles = group.NumberOfCycles.HasValue ? group.NumberOfCycles.Value : 1;

            if ((group.TypeId == 2 || group.TypeId == 3) && !group.IsAdvanced)
            {

                bracketsRepo.DeleteAllBracketsAndChildrenBracketsForGroup(group);
                int ligitNumberOfTeams = CreateBracketsForEarlyStagePlayoffGroup(group);
                stagesLeft = Convert.ToInt32(Math.Log(ligitNumberOfTeams, 2)) - 1;
            }
            else if ((group.TypeId == 2 || group.TypeId == 3) && group.IsAdvanced)
            {
                group = CreateNextPlayoffStep(group);
            }
            //SetWinnersAndLosers(group.PlayoffBrackets.ToList());
            List<GamesCycle> groupGames = CreateGamesForPlayoffGroup(group);
            GameSettings settings = GetGameSettings(league, game);
            int numberOfTeams = group.PlayoffBrackets.Count * 2;
            int numberOfGamesInRound = numberOfTeams / 2;
            int numberOfRoundsInOneCycle = GetNumberOfRoundsInOneCycle(group.TypeId, numberOfTeams);
            var totalNumberOfRoundes = numberOfRoundsInOneCycle * numberOfCycles;
            SchedualGames(settings.ActualStartDate, settings.SettingsDays, settings.SequenceRounds, numberOfGamesInRound, totalNumberOfRoundes, groupGames);
            games.AddRange(groupGames);

            if (stagesLeft > 0)
            {
                SchedulePlayoffGroup(games, group, league, game, stagesLeft - 1);
            }
        }

        private Group CreateNextPlayoffStep(Group group)
        {
            //Create New Group
            Stage stage = stagesRepo.Create(group.Stage.LeagueId);
            stagesRepo.Save();
            Group newGroup = new Group
            {
                StageId = stage.StageId,
                Name = group.Name,
                IsArchive = false,
                NumberOfCycles = group.NumberOfCycles,
                TypeId = group.TypeId,
                IsAdvanced = true
            };
            groupesRepo.Create(newGroup);
            groupesRepo.Save();

            //Add Brackets to group
            CreateNextStepBrackets(group, newGroup);
            return newGroup;
        }

        private int CreateBracketsForEarlyStagePlayoffGroup(Group group)
        {
            group.IsAdvanced = true;
            List<int> groupTeamsIds = group.GroupsTeams.Where(gt => gt.Team.IsArchive == false).OrderBy(gt => gt.Pos).Select(gt => gt.TeamId).ToList();
            List<int> ligitTeamCounts = new List<int> { 2, 4, 8, 16, 32, 64 };
            int numberOfTeams = groupTeamsIds.Count;
            int ligitNumber = 0;
            foreach (var num in ligitTeamCounts)
            {
                if (numberOfTeams <= num)
                {
                    ligitNumber = num;
                    break;
                }
            }

            for (int i = numberOfTeams; i < ligitNumber; i++)
            {
                groupTeamsIds.Add(0);
            }

            List<Tuple<int, int>> combinations = CreatePlayoffCombinations(groupTeamsIds);
            List<PlayoffBracket> brackets = new List<PlayoffBracket>();
            foreach (var comb in combinations)
            {
                PlayoffBracket b = new PlayoffBracket
                {
                    Team1Id = comb.Item1,
                    Team2Id = comb.Item2,
                    MaxPos = 1,
                    MinPos = combinations.Count * 2,
                    Stage = 1,
                    Type = (int)PlayoffBracketType.Root
                };

                if (comb.Item2 == 0)
                {
                    b.Team2Id = null;
                    b.WinnerId = comb.Item1;
                }

                brackets.Add(b);
            }
            group.PlayoffBrackets = brackets;
            groupesRepo.Save();
            return ligitNumber;
        }

        private List<GamesCycle> CreateGamesForPlayoffGroup(Group group)
        {
            List<GamesCycle> games = new List<GamesCycle>();
            for (int i = 1; i <= group.NumberOfCycles; i++)
            {
                foreach (var bracket in group.PlayoffBrackets)
                {
                    GamesCycle g = new GamesCycle
                    {
                        MinPlayoffPos = bracket.MinPos,
                        MaxPlayoffPos = bracket.MaxPos,
                        GroupeId = group.GroupId,
                        StageId = group.StageId
                    };

                    if (bracket.Type == (int)PlayoffBracketType.Root && (bracket.Team1Id == null || bracket.Team2Id == null))
                    {
                        g.GameStatus = GameStatus.Ended;
                    }

                    if (i % 2 != 0)
                    {
                        g.HomeTeamId = bracket.Team1Id;
                        g.GuestTeamId = bracket.Team2Id;
                    }
                    else
                    {
                        g.HomeTeamId = bracket.Team2Id;
                        g.GuestTeamId = bracket.Team1Id;
                    }
                    bracket.GamesCycles.Add(g);
                    games.Add(g);
                }
            }
            return games;
        }

        private void CreateNextStepBrackets(Group parentGroup, Group childGroup)
        {
            var parentBracketGroups = parentGroup.PlayoffBrackets.GroupBy(b => new { b.Stage, b.MinPos, b.MaxPos });

            List<PlayoffBracket> nextBrackets = new List<PlayoffBracket>();

            foreach (var brackets in parentBracketGroups)
            {
                int minPos = brackets.Key.MinPos;
                int maxPos = brackets.Key.MaxPos;
                int middelPos = ((brackets.Key.MinPos - brackets.Key.MaxPos) / 2) + brackets.Key.MaxPos;
                int stage = brackets.Key.Stage + 1;

                int startIndex = 0;
                int endIndex = brackets.Count() - 1;
                for (int i = 0; i < brackets.Count() / 2; i++)
                {
                    var parent1 = brackets.ElementAt(startIndex);
                    startIndex++;
                    var parent2 = brackets.ElementAt(endIndex);
                    endIndex--;
                    PlayoffBracket winnerBracket = new PlayoffBracket();
                    winnerBracket.Team1Id = parent1.WinnerId;
                    winnerBracket.Team2Id = parent2.WinnerId;
                    winnerBracket.Stage = stage;
                    winnerBracket.MaxPos = maxPos;
                    winnerBracket.MinPos = middelPos;
                    winnerBracket.Type = (int)PlayoffBracketType.Winner;
                    winnerBracket.ParentBracket1Id = parent1.Id;
                    winnerBracket.ParentBracket2Id = parent2.Id;
                    winnerBracket.GroupId = childGroup.GroupId;
                    nextBrackets.Add(winnerBracket);


                    if (childGroup.TypeId == 2)
                    {
                        PlayoffBracket loserBracket = new PlayoffBracket();
                        loserBracket.Team1Id = parent1.LoserId;
                        loserBracket.Team2Id = parent2.LoserId;
                        loserBracket.Stage = brackets.Key.Stage + 1;
                        loserBracket.MaxPos = middelPos + 1;
                        loserBracket.MinPos = minPos;
                        loserBracket.Type = (int)PlayoffBracketType.Loseer;
                        loserBracket.ParentBracket1Id = parent1.Id;
                        loserBracket.ParentBracket2Id = parent2.Id;
                        loserBracket.GroupId = childGroup.GroupId;
                        nextBrackets.Add(loserBracket);
                    }
                }
            }
            var list = nextBrackets.OrderBy(b => b.Type).ToList();
            bracketsRepo.SaveBrackets(list);
        }

        private void SetWinnersAndLosers(List<PlayoffBracket> brackets)
        {
            foreach (var bracket in brackets)
            {
                if (bracket.FirstTeam != null && bracket.SecondTeam == null)
                {
                    bracket.WinnerId = bracket.FirstTeam.TeamId;
                }
                else if (bracket.FirstTeam == null && bracket.SecondTeam != null)
                {
                    bracket.WinnerId = bracket.SecondTeam.TeamId;
                }
                //else if (bracket.FirstTeam != null && bracket.SecondTeam != null)
                //{
                //    int t1score = bracket.GamesCycles.Where(g => g.HomeTeamId == bracket.FirstTeam.TeamId && g.HomeTeamScore > g.GuestTeamScore)
                //    .Concat(bracket.GamesCycles.Where(g => g.GuestTeamId == bracket.FirstTeam.TeamId && g.HomeTeamScore < g.GuestTeamScore)).Count();

                //    int t2score = bracket.GamesCycles.Where(g => g.HomeTeamId == bracket.SecondTeam.TeamId && g.HomeTeamScore > g.GuestTeamScore)
                //        .Concat(bracket.GamesCycles.Where(g => g.GuestTeamId == bracket.SecondTeam.TeamId && g.HomeTeamScore < g.GuestTeamScore)).Count();

                //    if (t1score > t2score)
                //    {
                //        bracket.WinnerId = bracket.FirstTeam.TeamId;
                //        bracket.LoserId = bracket.SecondTeam.TeamId;
                //    }
                //    else if (t1score < t2score)
                //    {
                //        bracket.WinnerId = bracket.SecondTeam.TeamId;
                //        bracket.LoserId = bracket.FirstTeam.TeamId;
                //    }
                //}
            }
            bracketsRepo.Save();
        }

        #endregion

        #region Sechedual

        private void SchedualGames(DateTime actualStartDate, int[] settingsDays, int sequenceRounds, int numberOfGamesInRound, int totalNumberOfRoundes, List<GamesCycle> groupGames)
        {
            int gameIndex = 0;
            DateTime roundDate = actualStartDate;
            for (int r = 0; r < totalNumberOfRoundes; r++)
            {
                if (gameIndex >= groupGames.Count)
                {
                    break;
                }
                DateTime dayDate = roundDate;

                int numberOfDayLeftInWeek = settingsDays.Count() - Array.IndexOf(settingsDays, (int)dayDate.DayOfWeek);

                for (int g = 0; g < numberOfDayLeftInWeek; g++)
                {
                    if (gameIndex >= groupGames.Count)
                    {
                        break;
                    }

                    int numberOfGamesInDay = numberOfGamesInRound / numberOfDayLeftInWeek;
                    if (g < numberOfGamesInRound % numberOfDayLeftInWeek)
                    {
                        numberOfGamesInDay++;
                    }
                    DateTime gameDate = dayDate;
                    for (int d = 0; d < numberOfGamesInDay; d++)
                    {

                        var gameC = groupGames.ElementAt(gameIndex);
                        gameC.CycleNum = r;
                        gameC.StartDate = gameDate;
                        gameIndex++;
                    }
                    int currentDayIndex = Array.IndexOf(settingsDays, (int)dayDate.DayOfWeek);
                    int nextDayIndex = (currentDayIndex + 1) % settingsDays.Count();
                    dayDate = GetNextWeekday(dayDate, settingsDays[nextDayIndex]);
                }


                roundDate = GetNextWeekday(roundDate.AddDays(1), settingsDays[0]);

                if (r % sequenceRounds == (sequenceRounds - 1))
                {
                    roundDate = GetNextWeekday(roundDate.AddDays(1), 0);
                    roundDate = GetNextWeekday(roundDate, settingsDays[0]);
                }
            }
        }

        #endregion

        #region Private

        public List<Tuple<int, int>> CreatePlayoffCombinations(List<int> listTeam)
        {
            int numTeams = listTeam.Count;
            var resList = new List<Tuple<int, int>>();
            if (numTeams % 2 != 0)
            {
                return resList;
            }

            for (int i = 0; i < numTeams / 2; i++)
            {
                Tuple<int, int> t = Tuple.Create(listTeam[i], listTeam[numTeams - 1 - i]);
                resList.Add(t);
            }

            return resList;
        }

        private List<Tuple<int, int>> CreateCombinationCycles(int numberOfCycles, List<Tuple<int, int>> combinations)
        {
            List<Tuple<int, int>> result = new List<Tuple<int, int>>();
            for (int i = 1; i <= numberOfCycles; i++)
            {
                foreach (var comb in combinations)
                {
                    if (i % 2 == 0)
                    {
                        result.Add(Tuple.Create(comb.Item2, comb.Item1));
                    }
                    else
                    {
                        result.Add(comb);
                    }
                }
            }
            return result;
        }

        private List<GamesCycle> CreateGamesFromCombinations(List<Tuple<int, int>> combinations)
        {
            List<GamesCycle> groupGames = new List<GamesCycle>();
            foreach (var comb in combinations)
            {
                var item = new GamesCycle
                {
                    HomeTeamId = comb.Item1,
                    GuestTeamId = comb.Item2
                };
                groupGames.Add(item);
            }
            return groupGames;
        }

        private GameSettings GetGameSettings(League league, Game game)
        {
            //Set Real Start Day for this Stage
            DateTime actualStartDate = game.StartDate;

            // If current stage is not the last one, start date should be set to the date of the last game in the last stage Played plus one day
            DateTime? lastGameDateFromStageBeforeLast = stagesRepo.GetLastGameDateFromStageBeforeLast(league.LeagueId);
            if (lastGameDateFromStageBeforeLast.HasValue)
            {
                actualStartDate = lastGameDateFromStageBeforeLast.Value;
            }


            // days list 0,1,2
            int[] settingsDays = (game.GameDays).Split(',').Select(x => int.Parse(x)).ToArray();

            while (!settingsDays.Contains((int)actualStartDate.DayOfWeek))
            {
                actualStartDate = actualStartDate.AddDays(1);
            }

            //Find the rate of sequence weeks
            int sequenceRounds = game.NumberOfSequenceRounds.HasValue ? game.NumberOfSequenceRounds.Value : int.MaxValue;
            if (sequenceRounds == 0)
            {
                sequenceRounds = int.MaxValue;
            }

            GameSettings settings = new GameSettings
            {
                ActualStartDate = actualStartDate,
                SettingsDays = settingsDays,
                SequenceRounds = sequenceRounds
            };

            return settings;
        }

        private int GetNumberOfRoundsInOneCycle(int gamesType, int numberOfTeams)
        {
            int numberOfRoundsInOneCycle = 0;
            if (gamesType == 1)
            {
                //League
                numberOfRoundsInOneCycle = numberOfTeams - 1;
                if (numberOfTeams % 2 != 0)
                {
                    numberOfRoundsInOneCycle++;
                }

            }
            else if (gamesType == 2 || gamesType == 3 || gamesType == 4 || gamesType == 5)
            {
                //Playoffs OR Knockout
                numberOfRoundsInOneCycle = numberOfTeams / 2;
            }
            return numberOfRoundsInOneCycle;
        }

        private void UpdateReferee(List<GamesCycle> gamesList, int leagueId, int unionId)
        {

            var uRepo = new UsersRepo();
            List<User> referees = uRepo.GetUnionAndLeageReferees(unionId, leagueId).OrderBy(t => Guid.NewGuid()).ToList();
            if (referees.Count() > 0)
            {
                int refereeIndex = 0;
                foreach (var gc in gamesList.OrderBy(g => g.StartDate))
                {
                    if (refereeIndex < 0 || refereeIndex >= referees.Count())
                    {
                        refereeIndex = 0;
                    }
                    gc.RefereeId = referees.ElementAt(refereeIndex).UserId;
                    refereeIndex++;
                }
            }
        }

        private void UpdateAuditorium(List<GamesCycle> gamesList)
        {

            foreach (var gc in gamesList)
            {
                var auditoriumId = teamsRepo.GetMainOrFirstAuditoriumForTeam(gc.HomeTeamId);
                gc.AuditoriumId = auditoriumId;
            }
        }

        private void SaveGamesList(List<GamesCycle> gamesList)
        {
            gamesRepo.SaveGames(gamesList);
        }

        public static DateTime GetNextWeekday(DateTime start, int day)
        {
            int daysToAdd = (day - (int)start.DayOfWeek + 7) % 7;
            return start.AddDays(daysToAdd);
        }

        public void ValidateTimePlaceContradictions(List<GamesCycle> gamesList, TimeSpan gamesInterval)
        {
            gamesRepo.ValidateTimePlaceContradictions(gamesList, gamesInterval);
        }

        private int GetWeekOfYear(DateTime date)
        {
            var cal = System.Globalization.DateTimeFormatInfo.CurrentInfo.Calendar;
            return cal.GetWeekOfYear(date, System.Globalization.CalendarWeekRule.FirstDay, System.DayOfWeek.Sunday);
        }

        #endregion

        #region Actions

        public void MoveCycles(int stageId, int cycleNum, DateTime newDate, bool allAfter)
        {
            var stage = stagesRepo.GetById(stageId);
            var game = stage.League.Games.FirstOrDefault();

            var allGameCycles = stage.GamesCycles.OrderBy(g => g.StartDate).ToList();
            var currentCycles = allGameCycles.Where(g => g.CycleNum == cycleNum).ToList();

            var firstGameOfCurrentCycle = currentCycles.FirstOrDefault();
            if (firstGameOfCurrentCycle == null)
            {
                return;
            }

            var startWeek = GetWeekOfYear(firstGameOfCurrentCycle.StartDate);
            var newWeek = GetWeekOfYear(newDate);
            var weeksDiff = newWeek - startWeek;

            foreach (var gameC in currentCycles)
            {
                gameC.StartDate = newDate;
            }

            if (allAfter && weeksDiff != 0)
            {
                var nextCycles = allGameCycles.Where(g => g.CycleNum > cycleNum).ToList();
                if (nextCycles.Count > 0)
                {
                    foreach (var gameC in nextCycles)
                    {
                        gameC.StartDate = gameC.StartDate.AddDays(weeksDiff * 7);
                    }
                }
            }
            stagesRepo.Save();
        }

        public void AddGame(GamesCycle item)
        {
            gamesRepo.AddGame(item);
        }

        #endregion

        #region Helper classes
        private class GameSettings
        {
            public DateTime ActualStartDate { get; set; }

            public int[] SettingsDays { get; set; }

            public int SequenceRounds { get; set; }
        }
        #endregion
    }
}
