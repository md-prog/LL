using System;
using System.Collections.Generic;
using System.Linq;
using AppModel;
using DataService.DTO;

namespace DataService
{
    public class ExcelGameService
    {
        private GamesRepo _gamesRepo;
        private LeagueRepo _leagueRepo;

        protected GamesRepo gamesRepo
        {
            get
            {
                if (_gamesRepo == null)
                {
                    _gamesRepo = new GamesRepo();
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
                    _leagueRepo = new LeagueRepo();
                }
                return _leagueRepo;
            }
        }

        protected IEnumerable<ExcelGameDto> MapResults(IEnumerable<GamesCycle> gamesList, int? seasonId = null)
        {
            var dtos = new List<ExcelGameDto>();
            foreach (var gc in gamesList)
            {
                var dto = new ExcelGameDto();

                dto.League = gc.Stage.League.Name;
                dto.LeagueId = gc.Stage.LeagueId;
                dto.Stage = gc.Stage.Number;
                dto.Groupe = (gc.Group != null) ? gc.Group.Name : "";
                dto.CycleNumber = (gc.CycleNum + 1);
                dto.Date = gc.StartDate;
                dto.HomeTeamId = gc.HomeTeam != null ? gc.HomeTeam.TeamId : 0;
                dto.GuestTeamId = gc.GuestTeam != null ? gc.GuestTeam.TeamId : 0;
                var section = gc.Stage.League.Season.Union.Section.Alias;
                switch (section)
                {
                    case GamesAlias.WaterPolo:
                    case GamesAlias.BasketBall:
                        dto.HomeTeamScore = gc.GameSets.Sum(x => x.HomeTeamScore);
                        dto.GuestTeamScore = gc.GameSets.Sum(x => x.GuestTeamScore);
                        break;
                    default:
                        dto.HomeTeamScore = gc.HomeTeamScore;
                        dto.GuestTeamScore = gc.GuestTeamScore;
                        break;
                }
                dto.GameId = gc.CycleId;
                dto.Time = gc.StartDate.ToString("HH:mm");

                if (seasonId.HasValue)
                {
                    var homeTeamDetails = gc.HomeTeam?.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    if (homeTeamDetails != null)
                    {
                        dto.HomeTeam = homeTeamDetails.TeamName;
                    }
                    else
                    {
                        dto.HomeTeam = gc.HomeTeam?.Title ?? "";
                    }

                    var guesTeamDetails = gc.GuestTeam?.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    if (guesTeamDetails != null)
                    {
                        dto.GuestTeam = guesTeamDetails.TeamName;
                    }
                    else
                    {
                        dto.GuestTeam = gc.GuestTeam?.Title ?? "";
                    }
                }
                else
                {
                    dto.HomeTeam = gc.HomeTeam != null ? gc.HomeTeam.Title : "";
                    dto.GuestTeam = gc.GuestTeam != null ? gc.GuestTeam.Title : "";
                }

                if (gc.GameSets.Any())
                {
                    var sets = gc.GameSets.ToArray();

                    dto.Set1 = sets.Length > 0 && sets[0].HomeTeamScore > 0 && sets[0].GuestTeamScore > 0 ? string.Format("{0} - {1}", sets[0].HomeTeamScore, sets[0].GuestTeamScore) : "";
                    dto.Set2 = sets.Length > 1 && sets[1].HomeTeamScore > 0 && sets[1].GuestTeamScore > 0 ? string.Format("{0} - {1}", sets[1].HomeTeamScore, sets[1].GuestTeamScore) : "";
                    dto.Set3 = sets.Length > 2 && sets[2].HomeTeamScore > 0 && sets[2].GuestTeamScore > 0 ? string.Format("{0} - {1}", sets[2].HomeTeamScore, sets[2].GuestTeamScore) : "";
                    dto.Set4 = sets.Length > 3 && sets[3].HomeTeamScore > 0 && sets[3].GuestTeamScore > 0 ? string.Format("{0} - {1}", sets[3].HomeTeamScore, sets[3].GuestTeamScore) : "";

                }

                if (gc.AuditoriumId.HasValue)
                {
                    dto.Auditorium = gc.Auditorium.Name;
                    dto.AuditoriumId = gc.AuditoriumId.Value;
                }
                if (gc.RefereeId.HasValue)
                {
                    dto.Referee = gc.User.FullName;
                    dto.RefereeId = gc.RefereeId.Value;
                }


                dtos.Add(dto);
            }

            return dtos;
        }

        public IEnumerable<ExcelGameDto> GetGameCyclesByIdSet(int[] gameIds)
        {
            var resList = gamesRepo.GetGroupsCyclesByGameIds(gameIds);

            return MapResults(resList);
        }


        public IEnumerable<ExcelGameDto> GetLeagueGames(int leagueId, int? seasonId)
        {
            return GetLeagueGames(leagueId, true, null, null);
        }
        public IEnumerable<ExcelGameDto> GetLeagueGames(int leagueId, bool userIsEditor, DateTime? dateFrom, DateTime? dateTo)
        {
            var league = leagueRepo.GetById(leagueId);
            var cond = new GamesRepo.GameFilterConditions
            {
                auditoriums = new List<AuditoriumShort>(),
                leagues = new List<LeagueShort>
                {
                    new LeagueShort
                    {
                        Check = true,
                        Id = leagueId,
                        UnionId = league.UnionId,
                        Name = league.Name
                    }
                },
                dateFrom = dateFrom,
                dateTo = dateTo,
                seasonId = league.SeasonId ?? 0
            };
            var resList = gamesRepo.GetCyclesByFilterConditions(cond, userIsEditor, false);

            return MapResults(resList, league.SeasonId);
        }

        public IEnumerable<ExcelGameDto> GetTeamGames(int teamId, int? seasonId)
        {
            var resList = gamesRepo.GetTeamCycles(teamId);
            return MapResults(resList, seasonId);
        }

        public IEnumerable<ExcelGameDto> GetTeamGames(int teamId, int leagueId, int? seasonId)
        {
            var resList = gamesRepo.GetTeamCycles(teamId, leagueId);
            return MapResults(resList, seasonId);
        }

        public IEnumerable<ExcelGameDto> GetLeagueGames(int leagueId, int[] gameIds)
        {
            var resList = gamesRepo.GetGroupsCycles(leagueId);

            if (gameIds.Length > 0)
                resList = resList.Where(w => gameIds.Any(a => a == w.CycleId));

            return MapResults(resList);
        }

        public IEnumerable<ExcelGameDto> GetTeamGames(int teamId, int[] gameIds, int? seasonId = null)
        {
            var resList = gamesRepo.GetTeamCycles(teamId);

            if (gameIds.Length > 0)
                resList = resList.Where(w => gameIds.Any(a => a == w.CycleId));

            return MapResults(resList, seasonId);
        }

        public IEnumerable<ExcelGameDto> GetTeamGames(int teamId, int leagueId, int[] gameIds, int? seasonId = null)
        {
            var resList = gamesRepo.GetTeamCycles(teamId, leagueId);

            if (gameIds.Length > 0)
                resList = resList.Where(w => gameIds.Any(a => a == w.CycleId));

            return MapResults(resList, seasonId);
        }
    }
}
