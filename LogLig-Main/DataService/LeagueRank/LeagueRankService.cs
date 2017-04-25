using System;
using AppModel;
using System.Collections.Generic;
using System.Linq;

namespace DataService.LeagueRank
{
    public class LeagueRankService : IDisposable
    {
        private int _leagueId;
        DataEntities db = new DataEntities();
        private LeagueRepo leagueRepo;
        private TeamsRepo teamsRepo;
        private List<Game> settings;



        public LeagueRankService(int leagueId)
        {
            _leagueId = leagueId;
            leagueRepo = new LeagueRepo(db);
            teamsRepo = new TeamsRepo(db);
        }

        public RankLeague CreateLeagueRankTable(int? seasonId = null)
        {
            //Get League
            var league = leagueRepo.GetByIdForRanks(_leagueId);
            if (league == null)
            {
                return null;
            }

            //Get game settings
            this.settings = league.Games.ToList();
            if (this.settings == null)
            {
                return null;
            }

            RankLeague rLeague = new RankLeague();
            rLeague.LeagueId = league.LeagueId;
            rLeague.AboutLeague = league.AboutLeague;
            rLeague.LeagueStructure = league.LeagueStructure;
            rLeague.Name = league.Name;
            rLeague.Logo = league.Logo;
            var stages = league.Stages;
            var index = 0;
            var stagesNotArchive = stages.Where(e => !e.IsArchive).ToList();
            foreach (var stage in stagesNotArchive)
            {
                RankStage rStage = new RankStage();
                rStage.Number = stage.Number;
                foreach (var group in stage.Groups.Where(e => !e.IsArchive))
                {
                    //LLCMS-191 don't show playoff or knockout type
                    if (group.GamesType.Name.Equals(GameType.Knockout) || group.GamesType.Name.Equals(GameType.Playoff))
                        continue;

                    RankGroup rGroup = CreateGroupRank(group, stagesNotArchive, index, seasonId);
                    rStage.Groups.Add(rGroup);
                    rGroup.GameType = group.GamesType.Name;
                    var result = CreateExtendedTable(group, rGroup.Teams);

                    rGroup.ExtendedTables.AddRange(result);
                }
                if (rStage.Groups.Any())
                    rLeague.Stages.Add(rStage);
                index++;
            }
            //  Update extended tables for all groups having "With their records" Point edit type
            //  in Netball (Catchball) leagues
            if (league.Union.Section.Alias == GamesAlias.NetBall)
            {
                foreach (var stage in rLeague.Stages.OrderByDescending(s => s.Number))
                {
                    if (stage.Groups.Any(g => g.PointsEditType == PointEditType.WithTheirRecords))
                    {
                        var extTables = rLeague.Stages.Where(s => s.Number < stage.Number)
                            .SelectMany(s => s.Groups, (s, g) => g)
                            .SelectMany(g => g.ExtendedTables, (g, et) => et);
                        foreach (var group in stage.Groups.Where(gr => gr.PointsEditType == PointEditType.WithTheirRecords))
                        {
                            foreach (var extTab in group.ExtendedTables)
                            {
                                var prevStagesScores = extTables.Where(et => et.TeamId == extTab.TeamId)
                                        .SelectMany(et => et.Scores, (et, s) => s)
                                        .Where(s => group.Teams.Any(gt => gt.Id == s.OpponentTeamId)).ToList();
                                prevStagesScores.AddRange(extTab.Scores);
                                extTab.Scores = prevStagesScores;
                            }
                        }
                    }
                }
            }

            SetTeamOrderByPosition(rLeague.Stages);

            return rLeague;
        }

        private void SetTeamOrderByPosition(List<RankTeam> teams)
        {
            short teamPosition = 1;
            for (int i = 0; i < teams.Count; i++)
            {
                teams[i].TeamPosition = teamPosition++;
            }
        }

        private void SetTeamOrderByPosition(List<RankStage> stages)
        {
            foreach (var rankStage in stages)
            {
                var notAdvancedGroups = rankStage.Groups.Where(x => !x.IsAdvanced);
                foreach (var group in notAdvancedGroups)
                {
                    var teams = group.Teams.OrderByDescending(x => x.Points).ThenByDescending(x => x.PointsDifference).ToList();
                    SetTeamOrderByPosition(teams);
                }
            }
        }

        public RankLeague CreateEmptyRankTable(int? seasonId = null)
        {
            var league = leagueRepo.GetById(this._leagueId);
            if (league == null)
            {
                return null;
            }

            //Get game settings
            this.settings = league.Games.ToList();
            if (this.settings == null)
            {
                return null;
            }
            RankLeague rLeague = new RankLeague();
            rLeague.LeagueId = league.LeagueId;
            rLeague.Name = league.Name;
            rLeague.Logo = league.Logo;
            rLeague.AboutLeague = league.AboutLeague;
            rLeague.LeagueStructure = league.LeagueStructure;
            var stages = league.Stages;
            var index = 0;
            var stagesNotArchive = stages.Where(e => e.IsArchive == false).ToList();
            foreach (var stage in stages.Where(e => e.IsArchive == false))
            {
                RankStage rStage = new RankStage();
                rStage.Number = stage.Number;
                foreach (var group in stage.Groups.Where(e => e.IsArchive == false))
                {
                    //LLCMS-191 don't show playoff or knockout type
                    if (group.GamesType.Name.Equals(GameType.Knockout) || group.GamesType.Name.Equals(GameType.Playoff))
                        continue;

                    RankGroup rGroup = CreateGroupRank(group, stagesNotArchive, index, seasonId);

                    SetTeamOrderByPosition(rGroup.Teams);
                    rStage.Groups.Add(rGroup);

                }

                rLeague.Stages.Add(rStage);
                index++;
            }
            return rLeague;
        }

        public List<RankTeam> GetRankedTeams(int leagueId, int teamId)
        {
            var resList = new List<RankTeam>();

            RankLeague rLeague = CreateLeagueRankTable();

            if (rLeague != null)
            {
                var stages = rLeague.Stages.OrderByDescending(t => t.Number);
                var stage = stages.Count() > 1 ? stages.ToArray()[1] : null;
                if (stage == null)
                {
                    return null;
                }

                var group = stage.Groups.Where(gr => gr.Teams.Any(t => t.Id == teamId)).FirstOrDefault();
                if (group == null)
                {
                    return null;
                }

                var teams = group.Teams.OrderBy(t => t.Position).ToList();
                for (int i = 0; i < teams.Count; i++)
                {
                    if (teams[i].Id == teamId)
                    {
                        resList.Add(teams[i]);
                    }
                }
            }

            return resList;
        }

        private RankGroup CreateGroupRank(Group group, List<Stage> stages, int index, int? seasonId)
        {
            RankGroup rGroup = new RankGroup();
            rGroup.Title = group.Name;
            rGroup.PointsEditType = group.PointEditType;
            var listGroup = db.GroupsTeams.Where(x => x.GroupId == group.GroupId).ToList();
            var teams = new List<RankTeam>();
            foreach (var team in listGroup)
            {
                if (seasonId.HasValue)
                {
                    var details = team.Team?.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    if (details != null)
                    {
                        team.Team.Title = details.TeamName;
                    }
                }
                var points = group.PointEditType == 2 && team.Points != null ? (int)team.Points : 0;
                var res = AddTeamIfNotExist(team.TeamId, teams, points, seasonId);
            }
            rGroup.Teams = teams;
            var rGroupsList = rGroup.Teams.ToList();
            GetPoints(rGroupsList, group, stages, index);

            rGroup.Teams = @group.TypeId == 3 ? SetTeamsOrderPlayOff(rGroupsList, @group) : SetTeamsOrder(rGroupsList, @group);
            rGroup.IsAdvanced = group.IsAdvanced;

            if (group.PlayoffBrackets.Any())
                rGroup.PlayoffBrackets = group.PlayoffBrackets.OrderBy(x => x.MaxPos).First().MinPos / 2;
            else
                rGroup.PlayoffBrackets = 0;

            return rGroup;
        }

        //List<Stage> stages
        private void CalculatePointsForWaterPolo(Group group, Game setting, bool flag, List<RankTeam> rGroupsList)
        {
            foreach (var game in group.GamesCycles.Where(g => !string.IsNullOrEmpty(g.GameStatus) && g.GameStatus.Trim() == GameStatus.Ended && setting != null))
            {

                RankTeam homeTeam = flag ? rGroupsList.FirstOrDefault(x => x.Id == game.HomeTeamId) : AddTeamIfNotExist(game.HomeTeamId, rGroupsList);
                RankTeam guestTeam = flag ? rGroupsList.FirstOrDefault(x => x.Id == game.GuestTeamId) : AddTeamIfNotExist(game.GuestTeamId, rGroupsList);

                if (homeTeam == null || guestTeam == null)
                    continue;

                guestTeam.HomeTeamFinalScore += game.GameSets.Sum(x => x.GuestTeamScore);
                guestTeam.GuesTeamFinalScore += game.GameSets.Sum(x => x.HomeTeamScore);

                homeTeam.HomeTeamFinalScore += game.GameSets.Sum(x => x.HomeTeamScore);
                homeTeam.GuesTeamFinalScore += game.GameSets.Sum(x => x.GuestTeamScore);


                homeTeam.SetsWon += game.GameSets.Sum(x => x.HomeTeamScore);
                homeTeam.SetsLost += game.GameSets.Sum(x => x.GuestTeamScore);

                guestTeam.SetsWon += game.GameSets.Sum(x => x.GuestTeamScore);
                guestTeam.SetsLost += game.GameSets.Sum(x => x.HomeTeamScore);

                homeTeam.Games++;
                guestTeam.Games++;

                //Technical Win/Lost
                if (game.TechnicalWinnnerId.HasValue)
                {
                    if (game.HomeTeamId == game.TechnicalWinnnerId.Value)
                    {
                        homeTeam.Points += setting.PointsTechWin;
                        guestTeam.Points += setting.PointsTechLoss;
                        homeTeam.Wins++;
                        guestTeam.Loses++;
                    }
                    else
                    {
                        guestTeam.Points += setting.PointsTechWin;
                        homeTeam.Points += setting.PointsTechLoss;
                        homeTeam.Loses++;
                        guestTeam.Wins++;
                    }
                }
                else
                {
                    //Normal Win/Lost

                    if (game.HomeTeamScore > game.GuestTeamScore)
                    {
                        //Home Team wins
                        homeTeam.Wins++;
                        guestTeam.Loses++;

                        homeTeam.Points += setting.PointsWin;
                        guestTeam.Points += setting.PointsLoss;
                    }
                    else if (game.HomeTeamScore < game.GuestTeamScore)
                    {
                        //Guest Team Wins
                        homeTeam.Loses++;
                        guestTeam.Wins++;

                        homeTeam.Points += setting.PointsLoss;
                        guestTeam.Points += setting.PointsWin;
                    }
                    else
                    {
                        //Draw
                        homeTeam.Draw++;
                        guestTeam.Draw++;
                        homeTeam.Points += setting.PointsDraw;
                        guestTeam.Points += setting.PointsDraw;
                    }

                }
            }
        }

        public void GetPoints(List<RankTeam> rGroupsList, Group group, List<Stage> stages, int index, bool flag = false)
        {
            var league = leagueRepo.GetById(_leagueId);
            if (group.PointEditType == 0)
            {
                if ((index - 1) >= 0)
                    foreach (var groupPre in stages[index - 1].Groups)
                    {
                        GetPoints(rGroupsList, groupPre, stages, index - 1, true);
                    }
            }
            if (group.TypeId == 3 || group.TypeId == 2)
            {
                if ((index - 1) >= 0 && stages[index - 1].Groups.All(x => x.TypeId == 3 || x.TypeId == 2))
                    foreach (var groupPre in stages[index - 1].Groups)
                    {
                        GetPoints(rGroupsList, groupPre, stages, index - 1, false);
                    }
            }
            Game setting = settings.FirstOrDefault(x => x.StageId == @group.StageId) ?? settings.FirstOrDefault();

            if (group.TypeId == 3 || group.TypeId == 2)
            {
                var positions = group.PlayoffBrackets.OrderBy(x => x.MaxPos);
                if (positions.Any())
                {
                    setting.PointsWin = positions.First().MinPos / 2;
                    setting.PointsTechWin = positions.First().MinPos / 2;
                }
                setting.PointsWin = 1;
                setting.PointsTechWin = 1;
                setting.PointsTechLoss = 0;
                setting.PointsDraw = 0;
                setting.PointsLoss = 0;
            }

            string sectionAlias = string.Empty;
            if (league != null)
            {
                var alias = league.Union?.Section?.Alias;
                if (alias != null)
                {
                    sectionAlias = alias;
                }

            }

            switch (sectionAlias)
            {
                case GamesAlias.WaterPolo:
                    CalculatePointsForWaterPolo(group, setting, flag, rGroupsList);
                    break;
                case GamesAlias.BasketBall:
                    CalculatePointsForBasketBall(group, setting, flag, rGroupsList);
                    break;

                default:
                    CalculatePoints(group, flag, rGroupsList, setting);
                    break;
            }

        }

        private List<ExtendedTable> CreateExtendedTable(Group group, List<RankTeam> rGroupsList)
        {
            var results = new List<ExtendedTable>();
            char[] alpha = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToLower().ToCharArray();
            int charIndex = 0;

            foreach (var game in group.GamesCycles)
            {

                RankTeam homeTeam = rGroupsList.FirstOrDefault(x => x.Id == game.HomeTeamId);
                RankTeam guestTeam = rGroupsList.FirstOrDefault(x => x.Id == game.GuestTeamId);

                if (guestTeam == null || homeTeam == null)
                    continue;

                SetExtendedTableScoresForHomeTeam(results, homeTeam, guestTeam, alpha, ref charIndex, game);
                SetExtendedTableScoresForGuestTeam(results, guestTeam, homeTeam, alpha, ref charIndex, game);
            }

            results = results.OrderBy(x => x.TeamName).ToList();
            return results;
        }

        private void SetExtendedTableScoresForHomeTeam(List<ExtendedTable> results, RankTeam homeTeam, RankTeam guestTeam, char[] alpha, ref int charIndex, GamesCycle game)
        {
            var homeTeamForExtended = results.FirstOrDefault(t => t.TeamId == homeTeam.Id.Value);
            //scores 
            if (homeTeamForExtended == null)
            {
                homeTeamForExtended = new ExtendedTable
                {
                    TeamId = homeTeam.Id.Value,
                    TeamName = homeTeam.Title,
                    Letter = alpha[charIndex],
                };

                results.Add(homeTeamForExtended);

                charIndex++;
            }

            if (!string.IsNullOrEmpty(game.GameStatus) && game.GameStatus.Trim() == GameStatus.Ended)
            {
                homeTeamForExtended.Scores.Add(new ExtendedTableScore
                {
                    OpponentTeamId = guestTeam.Id.Value,
                    OpponentScore = game.GuestTeamScore,
                    TeamScore = game.HomeTeamScore

                });
            }
        }

        private void SetExtendedTableScoresForGuestTeam(List<ExtendedTable> results, RankTeam guestTeam, RankTeam homeTeam, char[] alpha, ref int guestCharIndex, GamesCycle game)
        {
            var guestTeamForExtended = results.FirstOrDefault(t => t.TeamId == guestTeam.Id.Value);
            if (guestTeamForExtended == null)
            {
                guestTeamForExtended = new ExtendedTable
                {
                    TeamId = guestTeam.Id.Value,
                    TeamName = guestTeam.Title,
                    Letter = alpha[guestCharIndex],
                };

                results.Add(guestTeamForExtended);

                guestCharIndex++;
            }

            if (!string.IsNullOrEmpty(game.GameStatus) && game.GameStatus.Trim() == GameStatus.Ended)
            {
                guestTeamForExtended.Scores.Add(new ExtendedTableScore
                {
                    OpponentTeamId = homeTeam.Id.Value,
                    OpponentScore = game.HomeTeamScore,
                    TeamScore = game.GuestTeamScore

                });
            }

        }

        private void CalculatePointsForBasketBall(Group group, Game setting, bool flag, List<RankTeam> rGroupList)
        {
            foreach (var game in group.GamesCycles.Where(g => !string.IsNullOrEmpty(g.GameStatus) && g.GameStatus.Trim() == GameStatus.Ended && setting != null))
            {

                RankTeam homeTeam = flag ? rGroupList.FirstOrDefault(x => x.Id == game.HomeTeamId) : AddTeamIfNotExist(game.HomeTeamId, rGroupList);
                RankTeam guestTeam = flag ? rGroupList.FirstOrDefault(x => x.Id == game.GuestTeamId) : AddTeamIfNotExist(game.GuestTeamId, rGroupList);

                if (homeTeam == null || guestTeam == null)
                    continue;

                ////count of games
                homeTeam.Games++;
                guestTeam.Games++;

                guestTeam.HomeTeamFinalScore += game.GameSets.Sum(x => x.GuestTeamScore);
                guestTeam.GuesTeamFinalScore += game.GameSets.Sum(x => x.HomeTeamScore);

                homeTeam.HomeTeamFinalScore += game.GameSets.Sum(x => x.HomeTeamScore);
                homeTeam.GuesTeamFinalScore += game.GameSets.Sum(x => x.GuestTeamScore);

                homeTeam.SetsWon += game.GameSets.Sum(x => x.HomeTeamScore);
                homeTeam.SetsLost += game.GameSets.Sum(x => x.GuestTeamScore);

                guestTeam.SetsWon += game.GameSets.Sum(x => x.GuestTeamScore);
                guestTeam.SetsLost += game.GameSets.Sum(x => x.HomeTeamScore);

                //Technical Win/Lost
                if (game.TechnicalWinnnerId.HasValue)
                {
                    if (game.HomeTeamId == game.TechnicalWinnnerId.Value)
                    {
                        homeTeam.Points += setting.PointsTechWin;
                        guestTeam.Points += setting.PointsTechLoss;
                        homeTeam.Wins++;
                        guestTeam.TechLosses++;
                    }
                    else
                    {
                        guestTeam.Points += setting.PointsTechWin;
                        homeTeam.Points += setting.PointsTechLoss;
                        homeTeam.TechLosses++;
                        guestTeam.Wins++;
                    }
                }
                else
                {
                    //Normal Win/Lost
                    var homeTeamScore = game.GameSets.Sum(g => g.HomeTeamScore);
                    var guestTeamScore = game.GameSets.Sum(g => g.GuestTeamScore);
                    if (homeTeamScore > guestTeamScore)
                    {
                        //Home Team wins
                        homeTeam.Wins++;
                        guestTeam.Loses++;

                        homeTeam.Points += setting.PointsWin;
                        guestTeam.Points += setting.PointsLoss;
                    }
                    else if (homeTeamScore < guestTeamScore)
                    {
                        //Guest Team Wins
                        homeTeam.Loses++;
                        guestTeam.Wins++;

                        homeTeam.Points += setting.PointsLoss;
                        guestTeam.Points += setting.PointsWin;
                    }
                    else
                    {
                        //Draw
                        homeTeam.Draw++;
                        guestTeam.Draw++;
                        homeTeam.Points += setting.PointsDraw;
                        guestTeam.Points += setting.PointsDraw;
                    }


                }
            }
        }

        private void CalculatePoints(Group group, bool flag, List<RankTeam> rGroupsList, Game setting)
        {
            foreach (var game in group.GamesCycles)
            {
                RankTeam homeTeam = flag ? rGroupsList.FirstOrDefault(x => x.Id == game.HomeTeamId) : AddTeamIfNotExist(game.HomeTeamId, rGroupsList);
                RankTeam guestTeam = flag ? rGroupsList.FirstOrDefault(x => x.Id == game.GuestTeamId) : AddTeamIfNotExist(game.GuestTeamId, rGroupsList);
                if (guestTeam == null || homeTeam == null)
                    continue;
                if (!string.IsNullOrEmpty(game.GameStatus) && game.GameStatus.Trim() == GameStatus.Ended &&
                    setting != null)
                {

                    homeTeam.Games++;
                    guestTeam.Games++;

                    //Technical Win/Lost
                    if (game.TechnicalWinnnerId != null)
                    {

                        if (game.HomeTeamId == game.TechnicalWinnnerId)
                        {
                            homeTeam.Points += setting.PointsTechWin;
                            guestTeam.Points += setting.PointsTechLoss;
                            homeTeam.Wins++;
                            guestTeam.Loses++;
                        }
                        else
                        {
                            guestTeam.Points += setting.PointsTechWin;
                            homeTeam.Points += setting.PointsTechLoss;
                            homeTeam.Loses++;
                            guestTeam.Wins++;
                        }
                    }
                    else
                    {
                        //Normal Win/Lost
                        if (game.HomeTeamScore > game.GuestTeamScore)
                        {
                            //Home Team wins
                            homeTeam.Points += setting.PointsWin;
                            guestTeam.Points += setting.PointsLoss;
                            homeTeam.Wins++;
                            guestTeam.Loses++;
                        }
                        else if (game.HomeTeamScore < game.GuestTeamScore)
                        {
                            //Guest Team Wins
                            homeTeam.Points += setting.PointsLoss;
                            guestTeam.Points += setting.PointsWin;
                            homeTeam.Loses++;
                            guestTeam.Wins++;
                        }
                        else
                        {
                            //Drow
                            homeTeam.Points += setting.PointsDraw;
                            guestTeam.Points += setting.PointsDraw;

                        }
                    }



                    homeTeam.SetsWon += game.HomeTeamScore;
                    homeTeam.SetsLost += game.GuestTeamScore;
                    guestTeam.SetsWon += game.GuestTeamScore;
                    guestTeam.SetsLost += game.HomeTeamScore;

                    foreach (GameSet set in game.GameSets)
                    {
                        homeTeam.TotalPointsScored += set.HomeTeamScore;
                        guestTeam.TotalPointsScored += set.GuestTeamScore;
                        homeTeam.TotalPointsLost += set.GuestTeamScore;
                        guestTeam.TotalPointsLost += set.HomeTeamScore;
                        homeTeam.HomeTeamScore += set.HomeTeamScore;
                        guestTeam.GuestTeamScore += set.GuestTeamScore;

                        guestTeam.TotalGuesTeamPoints += set.GuestTeamScore;
                        guestTeam.TotalHomeTeamPoints += set.HomeTeamScore;

                        homeTeam.TotalHomeTeamPoints += set.HomeTeamScore;
                        homeTeam.TotalGuesTeamPoints += set.GuestTeamScore;

                        if (set.HomeTeamScore == set.GuestTeamScore)
                        {
                            homeTeam.Draw++;
                            guestTeam.Draw++;
                        }
                    }


                }
            }
        }

        private List<RankTeam> SetTeamsOrderPlayOff(List<RankTeam> teams, Group group)
        {
            IOrderedEnumerable<RankTeam> ordTeams = null;


            ordTeams = teams.OrderByDescending(t => t.Points);
            var teamGroups = ordTeams.GroupBy(t => t.Points);
            int pos = 1;
            List<RankTeam> result = new List<RankTeam>();
            foreach (var tg in teamGroups)
            {
                List<RankTeam> groupList = tg.ToList();
                if (groupList.Count() == 1)
                {
                    var t = groupList.ElementAt(0);
                    t.Position = pos.ToString();
                    result.Add(t);
                    pos++;
                }
                else if (groupList.Count() > 1)
                {
                    groupList = SortTeams(groupList, group);
                    for (var i = 0; i < groupList.Count(); i++)
                    {
                        groupList[i].Position = pos.ToString();
                    }
                    result.AddRange(groupList);
                }
                pos++;
            }
            return result;
        }

        private List<RankTeam> SetTeamsOrder(List<RankTeam> teams, Group group)
        {
            IOrderedEnumerable<RankTeam> ordTeams = teams.OrderByDescending(t => t.Points)
                                                         .ThenByDescending(t => t.SetsRatioNumiric);
            var teamGroups = ordTeams.GroupBy(t => new { t.Points, t.SetsRatioNumiric }).ToList();

            List<RankTeam> result = new List<RankTeam>();
            int position = 1;
            if (teamGroups.Any())
            {
                List<RankTeam> teamsInGroup = SortTeams(teamGroups[0].ToList(), group);

                foreach (var team in teamsInGroup)
                {
                    team.Position = position.ToString();
                    team.PositionNumber = position;
                }

                result.AddRange(teamsInGroup);
            }

            for (int i = 1; i < teamGroups.Count; i++)
            {
                List<RankTeam> teamsInGroup = SortTeams(teamGroups[i].ToList(), group);

                position = position + teamGroups[i - 1].Count();
                foreach (var team in teamsInGroup)
                {
                    team.Position = position.ToString();
                    team.PositionNumber = position;
                }

                result.AddRange(teamsInGroup);
            }
            return result;
        }

        public List<RankTeam> SortTeams(List<RankTeam> groupList, Group group)
        {
            for (var i = 0; i < groupList.Count() - 1; i++)
                for (var j = 0; j < groupList.Count() - 1; j++)
                    if (GetTheBestOutOfTwoTeams(groupList[j], groupList[j + 1], group) == -1)
                    {
                        var t = groupList[j];
                        groupList[j] = groupList[j + 1];
                        groupList[j + 1] = t;
                    }
            return groupList;
        }

        private int GetTheBestOutOfTwoTeams(RankTeam team1, RankTeam team2, Group group)
        {
            int team1Points = 0;
            int team2Points = 0;

            // Team1 = Guest Team & Team2 = Home Team
            var games = group.GamesCycles.Where(gc => gc.GuestTeamId == team1.Id && gc.HomeTeamId == team2.Id);
            foreach (var game in games)
            {
                if (game.HomeTeamScore > game.GuestTeamScore)
                {
                    team2Points++;
                }
                else if (game.HomeTeamScore < game.GuestTeamScore)
                {
                    team1Points++;
                }
            }

            // Team1 = Home Team & Team2 = Guest Team
            games = group.GamesCycles.Where(gc => gc.GuestTeamId == team2.Id && gc.HomeTeamId == team1.Id);

            foreach (var game in games)
            {
                if (game.HomeTeamScore > game.GuestTeamScore)
                {
                    team1Points++;
                }
                else if (game.HomeTeamScore < game.GuestTeamScore)
                {
                    team2Points++;
                }
            }

            List<RankTeam> result = new List<RankTeam>();
            if (team1Points > team2Points)
            {
                return 1;
            }
            else if (team1Points < team2Points)
            {
                return -1;
            }
            else
            {
                //If the two Teams did not play together or have same score
                return 0;
            }
        }

        public RankTeam AddTeamIfNotExist(int? id, List<RankTeam> teams, int points = 0, int? seasonId = null)
        {
            RankTeam t = teams.FirstOrDefault(tm => tm.Id == id);
            if (t == null)
            {
                Team team = teamsRepo.GetById(id);
                if (team != null)
                {
                    t = new RankTeam
                    {
                        Id = id,
                        Points = points,
                        Title = team.Title,
                        Games = 0,
                        Wins = 0,
                        Loses = 0,
                        SetsWon = 0,
                        SetsLost = 0,
                        Logo = team.Logo
                    };

                    if (seasonId.HasValue)
                    {
                        var teamDetails = team.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId.Value);
                        if (teamDetails != null)
                        {
                            t.Title = teamDetails.TeamName;
                        }
                    }

                    teams.Add(t);
                }
            }
            return t;
        }

        public void Dispose()
        {
            db.Dispose();
        }
    }
}


/*
private List<RankTeam> SetTeamsOrder(List<RankTeam> teams, Group group)
        {
            IOrderedEnumerable<RankTeam> ordTeams = null;

            int[] descriptorsArr = this.settings.SortDescriptors.Split(',').Select(d => Convert.ToInt32(d)).ToArray();
            for (int i = 0; i < descriptorsArr.Length; i++)
            {
                if (i == 0)
                {
                    switch (descriptorsArr[i])
                    {
                        //Points
                        case 0:
                            ordTeams = teams.OrderByDescending(t => t.Points);
                            break;
                        //Wins
                        case 1:
                            ordTeams = teams.OrderByDescending(t => t.Wins);
                            break;
                        //SetDiffs
                        case 2:
                            ordTeams = teams.OrderByDescending(t => t.SetsRatioNumiric);
                            break;
                    }
                }
                else
                {
                    switch (descriptorsArr[i])
                    {
                        //Points
                        case 0:
                            ordTeams = ordTeams.ThenByDescending(t => t.Points);
                            break;
                        //Wins
                        case 1:
                            ordTeams = ordTeams.ThenByDescending(t => t.Wins);
                            break;
                        //SetDiffs
                        case 2:
                            ordTeams = ordTeams.ThenByDescending(t => t.SetsRatioNumiric);
                            break;
                    }
                }
            }

            var teamGroups = ordTeams.GroupBy(t => new { t.Points, t.Wins, t.SetsRatioNumiric });
            int pos = 1;
            List<RankTeam> result = new List<RankTeam>();

            foreach (var tg in teamGroups)
            {

                List<RankTeam> groupList = tg.ToList();
                if (groupList.Count() == 1)
                {
                    var t = groupList.ElementAt(0);
                    t.Position = pos;
                    result.Add(t);
                    pos++;
                }
                else if (groupList.Count() == 2)
                {
                    groupList = GetTheBestOutOfTwoTeams(groupList.ElementAt(0), groupList.ElementAt(1), group);
                    foreach (var t in groupList)
                    {
                        t.Position = pos;
                        pos++;
                    }
                    result.AddRange(groupList);
                }
                else if (groupList.Count() > 2)
                {
                    
                    groupList = groupList.OrderByDescending(t => t.TotalPointsDiffs).ToList();
                    foreach (var t in groupList)
                    {
                        t.Position = pos;
                        pos++;
                    }
                    result.AddRange(groupList);
                }
                 
            }
            return result;
        }

*/
