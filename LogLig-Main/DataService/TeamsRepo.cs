using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using AppModel;
using DataService.DTO;

namespace DataService
{
    public class PlayerTeam
    {
        public int TeamId { get; set; }
        public int LeagueId { get; set; }
        public string Title { get; set; }
        public string LeagueName { get; set; }
        public string Logo { get; set; }
        public string Position { get; set; }
        public int ShirtNum { get; set; }
    }

    internal class PlayerTeamComparer : IEqualityComparer<PlayerTeam>
    {
        public bool Equals(PlayerTeam x, PlayerTeam y)
        {
            return x.TeamId == y.TeamId && x.LeagueId == y.LeagueId;
        }

        public int GetHashCode(PlayerTeam obj)
        {
            return (int)((long)obj.TeamId * 17 + obj.TeamId);
        }
    }

    public class TeamsRepo : BaseRepo
    {
        private Func<DateTime, IEnumerable<Season>> currentSeasons;

        public TeamsRepo() : base()
        {
            currentSeasons = date => db.Seasons.Where(s => s.StartDate <= date && s.EndDate >= date);
        }
        public TeamsRepo(DataEntities db) : base(db)
        {
            currentSeasons = date => db.Seasons.Where(s => s.StartDate <= date && s.EndDate >= date);
        }

        public int? GetSeasonIdByTeamId(int id, DateTime? date = null)
        {
            DateTime rDate = date ?? DateTime.Now;
            var league = GetLeagueByTeamId(id, rDate);
            return league?.SeasonId != null ? league.SeasonId : GetClubByTeamId(id, rDate)?.SeasonId;
        }

        public League GetLeagueByTeamId(int id, DateTime? date = null)
        {
            DateTime rDate = date ?? DateTime.Now;
            var currSeasons = currentSeasons(rDate).Select(s => s.Id).ToList();
            int? leagueId = db.LeagueTeams
                .Where(lt => lt.TeamId == id && currSeasons.Contains(lt.SeasonId ?? 0))
                .FirstOrDefault()?.LeagueId;
            if (leagueId.HasValue)
            {
                return db.Leagues.Where(l => l.LeagueId == leagueId).First();
            }
            else
            {
                return null;
            }
        }

        public ClubTeam GetClubByTeamId(int id, DateTime? date = null)
        {
            DateTime rDate = date ?? DateTime.Now;
            var currSeasons = currentSeasons(rDate).Select(s => s.Id).ToList();
            return db.ClubTeams
                .Where(ct => ct.TeamId == id && currSeasons.Contains(ct.SeasonId ?? 0))
                .FirstOrDefault();
        }

        public IEnumerable<Team> GetTeams(int seasonId, int leagueId)
        {
            var teams =
                db.LeagueTeams.Include(x => x.Teams)
                    .Include(x => x.Teams.TeamsDetails)
                    .Where(t => t.SeasonId == seasonId && t.LeagueId == leagueId)
                    .Select(x => x.Teams)
                    .ToList();

            foreach (var team in teams)
            {
                var teamDetails = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId);
                if (teamDetails != null)
                {
                    team.Title = teamDetails.TeamName;
                }
            }
            return teams;
        }

        public List<Team> GetTeamsByLeague(int leagueId)
        {
            return (from l in db.Leagues
                    from t in l.LeagueTeams
                    where t.Teams.IsArchive == false && l.LeagueId == leagueId
                    orderby t.Teams.Title
                    select t.Teams).ToList();

        }

        /// <summary>
        /// Select GroupTeams/Teams belonging to certain set of Leagues during certain season
        /// </summary>
        /// <param name="seasonId">int id of Season</param>
        /// <param name="leagueIds">int[] set of LeagueId</param>
        /// <returns>IDictionary<int, IList<TeamShortDTO>> dictionary where list of teams is returned by </returns>
        public IDictionary<int, IList<TeamShortDTO>> GetGroupTeamsBySeasonAndLeagues(int seasonId, IEnumerable<int> leagueIds, bool allLeagues = false)
        {
            IEnumerable<int> groupsIds = db.GamesCycles
                .Where(gc => allLeagues || leagueIds.Contains(gc.Stage.LeagueId) && gc.Stage.League.SeasonId == seasonId)
                .Where(gc => gc.GroupeId != null && !gc.Group.IsArchive)
                .Select(gc => gc.GroupeId ?? 0).Distinct();
            Dictionary<int, IList<TeamShortDTO>> result = new Dictionary<int, IList<TeamShortDTO>>();
            foreach (var id in groupsIds)
            {
                result[id] = new List<TeamShortDTO>();
            }
            var groupTeams = db.Teams
                .Join(db.LeagueTeams.Where(lt => lt.SeasonId == seasonId && leagueIds.Contains(lt.LeagueId)),
                        t => t.TeamId, lt => lt.TeamId, (t, lt) => t)
                .Join(db.GroupsTeams, t => t.TeamId, gt => gt.TeamId, (t, gt) => gt).Include(gt => gt.Team)
                .Where(gt => !gt.Group.IsArchive).Distinct();
            foreach (var gt in groupTeams)
            {
                if (!result.Keys.Contains(gt.GroupId))
                {
                    result[gt.GroupId] = new List<TeamShortDTO>();
                }
                var teamDetails = gt.Team.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId);
                var teamName = teamDetails == null ? gt.Team.Title : teamDetails.TeamName;
                result[gt.GroupId].Add(new TeamShortDTO
                {
                    TeamId = gt.TeamId,
                    Title = teamName
                });
            }
            return result;
        }

        public GroupsTeam GetGroupTeam(int idGroup, int idTeam)
        {
            return db.GroupsTeams.FirstOrDefault(x => x.TeamId == idTeam && x.GroupId == idGroup);
        }

        public void Create(Team item)
        {
            item.CreateDate = DateTime.Now;
            db.Teams.Add(item);
            db.SaveChanges();
        }

        public void AddTeamDetailToSeason(Team team, int season)
        {
            var teamDetail = new TeamsDetails
            {
                SeasonId = season,
                TeamId = team.TeamId,
                TeamName = team.Title
            };

            db.TeamsDetails.Add(teamDetail);
            db.SaveChanges();
        }

        public Team GetByName(string name)
        {
            return db.Teams.FirstOrDefault(t => t.Title == name);
        }

        public IEnumerable<Team> GetByPlayer(int playerId)
        {
            return db.TeamsPlayers.Where(t => t.UserId == playerId).Select(t => t.Team).ToList();
        }

        public League GetLeague(int id)
        {
            return db.Leagues.Find(id);
        }

        public Team GetById(int? id, int? seasonId = null)
        {
            if (id.HasValue)
            {
                var team = db.Teams.Include(t => t.TeamsDetails).Include(x => x.LeagueTeams).FirstOrDefault(f => f.TeamId == id);
                if (team != null)
                {
                    var teamDetail = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId);
                    if (teamDetail != null)
                    {
                        team.Title = teamDetail.TeamName;
                    }
                    return team;
                }
            }
            return null;
        }

        public TeamScheduleScrapperGame GetScheduleScrapperById(int teamId, int clubId)
        {
            return db.TeamScheduleScrapperGames.FirstOrDefault(x => x.TeamId == teamId && x.ClubId == clubId);

        }

        public List<TeamScheduleScrapper> GetTeamGamesFromScrapper(int clubId, int teamId)
        {
            return
                db.TeamScheduleScrapperGames.Where(x => x.ClubId == clubId && x.TeamId == teamId)
                    .SelectMany(x => x.TeamScheduleScrappers)
                    .ToList();
        }

        public Team GetTeamByTeamSeasonId(int teamId, int season)
        {
            var team = db.Teams.Find(teamId);
            if (team != null)
            {
                var teamDetails = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == season);
                if (teamDetails != null)
                {
                    team.Title = teamDetails.TeamName;
                }
                return team;
            }
            return null;
        }

        public void RemoveTeamDetails(Team team, int seasonId)
        {
            var details = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId);
            if (details != null)
            {
                team.TeamsDetails.Remove(details);
            }
        }

        public Team GetLeagueTeam(int teamId, int? leagueId, int seasonId)
        {
            var leagueTeam =
                db.LeagueTeams.Include(x => x.Teams)
                    .Include(t => t.Teams.TeamsDetails)
                    .FirstOrDefault(x => x.TeamId == teamId && x.LeagueId == leagueId && seasonId == x.SeasonId);
            if (leagueTeam != null)
            {
                var teamDetails = leagueTeam.Teams.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId);
                if (teamDetails != null)
                {
                    leagueTeam.Teams.Title = teamDetails.TeamName;
                    return leagueTeam.Teams;
                }
                return leagueTeam.Teams;
            }
            return null;
        }

        public IEnumerable<ListItemDto> FindByNameAndSection(string name, int? sectionId, int num)
        {
            var teams = db.Teams.Include(t => t.TeamsDetails)
                .Where(t => t.IsArchive == false 
                && (t.Title.Contains(name) 
                    || t.TeamsDetails.Any(td => td.TeamName.Contains(name)))
                && (!sectionId.HasValue
                    || t.LeagueTeams.Any(lt => lt.Leagues.Union.SectionId == sectionId 
                    || t.ClubTeams.Any(ct => ct.Club.Union.SectionId == sectionId)
                    || t.ClubTeams.Any(ct => ct.Club.SectionId == sectionId)))
                )
                .OrderBy(t => t.Title)
                .Take(num).ToList();

            var dtos = new List<ListItemDto>();
            if (teams.Count > 0)
            {
                foreach (var team in teams)
                {
                    var dto = new ListItemDto();
                    dto.Id = team.TeamId;
                    dto.Name = CreateTeamTitle(team);
                    dtos.Add(dto);
                }
            }

            return dtos;
        }

        private string CreateTeamTitle(Team team)
        {
            var leagueTitles = team.LeagueTeams.Select(l => l.Leagues.Name).ToList();
            var clubTitles = team.ClubTeams.Select(c => c.Club.Name).ToArray();
            leagueTitles.AddRange(clubTitles);
            var title = team.TeamsDetails.Count() == 0 ? team.Title : 
                team.TeamsDetails.OrderByDescending(td => td.Id).Select(td => td.TeamName).First();
            return team.Title + " (" + string.Join(", ", leagueTitles) + ")";
        }

        public IEnumerable<ListItemDto> FindInUnionByLeague(int leagueId, string name, int num)
        {
            int? unionId = db.Leagues
                .Where(t => t.LeagueId == leagueId && t.IsArchive == false)
                .Select(t => t.UnionId).First();

            return (from u in db.Unions
                    from le in u.Leagues
                    from t in le.LeagueTeams
                    where le.IsArchive == false && t.Teams.IsArchive == false
                          && u.UnionId == unionId
                          && t.Teams.Title.Contains(name)
                    orderby t.Teams.Title
                    select new ListItemDto { Id = t.TeamId, Name = t.Teams.Title }).Distinct().ToList();
        }

        public List<Team> GetByManagerId(int managerId, int? seasonId = null)
        {
            var teams = db.UsersJobs
                .Include(t => t.Team.TeamsDetails)
                .Where(j => j.UserId == managerId)
                .Select(j => j.Team)
                .Where(l => l != null && l.IsArchive == false)
                .Distinct()
                .OrderBy(u => u.Title)
                .ToList();

            foreach (var team in teams)
            {
                var teamDetail = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId);
                if (teamDetail != null)
                {
                    team.Title = teamDetail.TeamName;
                }
            }

            return teams;
        }

        public List<TeamManagerTeamInformationDto> GetByTeamManagerId(int managerId)
        {
            var teamManagerTeamsInformation = (from userJob in db.UsersJobs
                                               join team in db.Teams on userJob.TeamId equals team.TeamId
                                               where userJob.UserId == managerId && team.IsArchive == false

                                               let teamsDetails = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == userJob.SeasonId.Value)
                                               let league = team.LeagueTeams.FirstOrDefault(x => x.TeamId == team.TeamId)
                                               let club = team.ClubTeams.FirstOrDefault(x => x.TeamId == team.TeamId)
                                               select new TeamManagerTeamInformationDto
                                               {
                                                   LeagueId = league.LeagueId,
                                                   LeagueName = league != null ? league.Leagues.Name : string.Empty,
                                                   ClubId = userJob.ClubId ?? club.ClubId,
                                                   SeasonId = userJob.SeasonId,
                                                   TeamId = userJob.TeamId,
                                                   Title = teamsDetails != null ?
                                                               teamsDetails.TeamName :
                                                               team.Title,
                                                   UnionId = userJob.UnionId
                                               }).ToList();

            return teamManagerTeamsInformation;
        }

        public int? GetMainOrFirstAuditoriumForTeam(int? teamId)
        {
            if (teamId.HasValue)
            {
                var team = GetById(teamId);
                var auditorium = team.TeamsAuditoriums.Where(a => a.IsMain == true).FirstOrDefault();

                if (auditorium == null)
                {
                    auditorium = team.TeamsAuditoriums.FirstOrDefault();
                }

                if (auditorium != null)
                {
                    return auditorium.AuditoriumId;
                }
            }
            return null;
        }

        public int GetTeamsUnion(int teamId)
        {
            return (from u in db.Unions
                    from le in u.Leagues
                    from t in le.LeagueTeams
                    where t.TeamId == teamId
                    select u.UnionId).FirstOrDefault();
        }

        public List<Team> GetTeamsByIds(int[] ids)
        {
            List<Team> list = new List<Team>();
            foreach (int id in ids)
            {
                list.Add(db.Teams.Find(id));
            }
            return list;
        }

        public IQueryable<IGrouping<League, IGrouping<Stage, GamesCycle>>> GetTeamGames(int id, int seasonId)
        {
            IQueryable<IGrouping<League, IGrouping<Stage, GamesCycle>>> leagues = db.GamesCycles
                .Where(game => (game.HomeTeamId == id || game.GuestTeamId == id) &&
                            game.IsPublished &&
                            (game.HomeTeam.LeagueTeams.Any(t => t.SeasonId == seasonId) &&
                                game.GuestTeam.LeagueTeams.Any(t => t.SeasonId == seasonId)
                    )
                ).GroupBy(game => game.Stage)
                .Where(g => g.Key.IsArchive == false)
                .GroupBy(s => s.Key.League)
                .Where(l => l.Key.IsArchive == false);
            return leagues;
        }

        public Tuple<int, int> GetByMostFans(int leagueId)
        {

            var bestTeam = db.LeagueTeams.Where(lt => lt.LeagueId == leagueId)
                .Join(db.TeamsFans, lt => lt.TeamId, tf => tf.TeamId, (lt, tf) => tf)
                .Select(tf => new { tf.TeamId, tf.UserId }).Distinct()
                .GroupBy(tf => tf.TeamId).Select(g => new { TeamId = g.Key, FansCount = g.Count() })
                .OrderByDescending(g => g.FansCount).FirstOrDefault();

            return bestTeam == null ? null : Tuple.Create(bestTeam.TeamId, bestTeam.FansCount);
        }

        public IList<TeamDto> GetFanTeams(int fanId, int? seasonId)
        {
            List<TeamDto> fanTeams = new List<TeamDto>();
            var fanTeamsInformation = (from l in db.Leagues
                                       from t in l.LeagueTeams
                                       from tp in t.Teams.TeamsFans
                                       where l.IsArchive == false && t.Teams.IsArchive == false &&
                                             tp.UserId == fanId

                                       let teamDetails = t.Teams.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)
                                       select new TeamInformationDto()
                                       {
                                           Team = new TeamDto()
                                           {
                                               LeagueId = l.LeagueId,
                                               TeamId = t.TeamId,
                                               Logo = t.Teams.Logo,
                                               Title = t.Teams.Title,
                                               SeasonId = t.SeasonId
                                           },
                                           TeamInformation = teamDetails != null ?
                                                         new TeamDetailsDto()
                                                         {
                                                             TeamId = teamDetails.TeamId,
                                                             SeasonId = teamDetails.SeasonId,
                                                             TeamName = teamDetails.TeamName
                                                         } : null
                                       })
                            .ToList();

            if (seasonId.HasValue)
            {
                fanTeams = fanTeamsInformation.Where(x => x.Team.SeasonId != null && x.Team.SeasonId.Value == seasonId.Value)
                    .Select(t => new TeamDto()
                    {
                        TeamId = t.Team.TeamId,
                        Title = t.TeamInformation != null && t.TeamInformation.SeasonId == seasonId ?
                                t.TeamInformation.TeamName : t.Team.Title,
                        LeagueId = t.Team.LeagueId,
                        Logo = t.Team.Logo
                    })
                    .ToList();

                fanTeams = fanTeams.GroupBy(x => x.TeamId).Select(g => g.First()).ToList();
            }
            else
            {
                fanTeams = fanTeamsInformation
                    .Select(t => new TeamDto()
                    {
                        TeamId = t.Team.TeamId,
                        Title = t.TeamInformation != null && t.TeamInformation.SeasonId == seasonId ?
                                t.TeamInformation.TeamName : t.Team.Title,
                        LeagueId = t.Team.LeagueId,
                        Logo = t.Team.Logo
                    })
                    .GroupBy(x => x.TeamId).Select(g => g.First()).ToList();
            }

            return fanTeams;
        }

        public IList<TeamDto> GetPlayerTeams(int playerId, int? seasonId = null)
        {
            List<TeamDto> playerTeams;
            var playerTeamsInformation = (from l in db.Leagues
                                          from t in l.LeagueTeams
                                          from tp in t.Teams.TeamsPlayers
                                          where l.IsArchive == false && t.Teams.IsArchive == false &&
                                                tp.UserId == playerId

                                          let teamDetails = t.Teams.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId.Value)
                                          select new TeamInformationDto()
                                          {
                                              Team = new TeamDto()
                                              {
                                                  LeagueId = l.LeagueId,
                                                  TeamId = t.TeamId,
                                                  Logo = t.Teams.Logo,
                                                  Title = t.Teams.Title,
                                                  SeasonId = t.SeasonId
                                              },
                                              TeamInformation = teamDetails != null ?
                                                                new TeamDetailsDto()
                                                                {
                                                                    TeamId = teamDetails.TeamId,
                                                                    SeasonId = teamDetails.SeasonId,
                                                                    TeamName = teamDetails.TeamName
                                                                } : null
                                          }).ToList();

            if (seasonId.HasValue)
            {
                playerTeams = playerTeamsInformation.Where(x => x.Team.SeasonId != null && x.Team.SeasonId.Value == seasonId.Value)
                    .Select(t => new TeamDto()
                    {
                        TeamId = t.Team.TeamId,
                        Title = t.TeamInformation != null && t.TeamInformation.SeasonId == seasonId ?
                                t.TeamInformation.TeamName : t.Team.Title,
                        LeagueId = t.Team.LeagueId,
                        Logo = t.Team.Logo
                    })
                    .ToList();
            }
            else
            {
                playerTeams = playerTeamsInformation.Select(x => x.Team).GroupBy(x => x.TeamId).Select(g => g.First()).ToList();
            }

            return playerTeams;
        }

        public IList<TeamStanding> GetTeamStandings(int clubId, int teamId)
        {
            var result = (from tsg in db.TeamStandingGames
                          join ts in db.TeamStandings on tsg.Id equals ts.TeamStandingGamesId
                          where teamId == tsg.TeamId && clubId == tsg.ClubId
                          orderby ts.Rank
                          select ts).ToList();

            return result;
        }

        public TeamStandingGame GetTeamStandingGame(int teamId, int clubId)
        {
            var result = db.TeamStandingGames.FirstOrDefault(x => x.TeamId == teamId && x.ClubId == clubId);
            return result;
        }

        public IList<PlayerTeam> GetPlayerPositions(int playerId, int? seasonId)
        {
            var playerTeamsDtos = (from l in db.Leagues
                                   from t in l.LeagueTeams
                                   from tp in t.Teams.TeamsPlayers
                                   let teamDetails = t.Teams.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)
                                   where t.Teams.IsArchive == false && tp.UserId == playerId
                                   select new PlayerTeamDto()
                                   {
                                       LeagueId = l.LeagueId,
                                       LeagueName = l.Name,
                                       TeamDetails = new TeamInformationDto()
                                       {
                                           Team = new TeamDto()
                                           {
                                               TeamId = t.TeamId,
                                               Logo = t.Teams.Logo,
                                               Title = t.Teams.Title,
                                           },
                                           TeamInformation = teamDetails != null ? new TeamDetailsDto()
                                           {
                                               TeamId = teamDetails.TeamId,
                                               TeamName = teamDetails.TeamName,
                                               SeasonId = teamDetails.SeasonId
                                           } : null
                                       },
                                       ShirtNum = tp.ShirtNum,
                                       Position = tp.Position.Title
                                   }).ToList();

            List<PlayerTeam> playerTeams = (from playerTeamsDto in playerTeamsDtos
                                            let teamInformation = playerTeamsDto.TeamDetails.TeamInformation
                                            select new PlayerTeam()
                                            {
                                                LeagueId = playerTeamsDto.LeagueId,
                                                LeagueName = playerTeamsDto.LeagueName,
                                                TeamId = playerTeamsDto.TeamDetails.Team.TeamId,
                                                Logo = playerTeamsDto.TeamDetails.Team.Logo,
                                                Title = teamInformation != null ? teamInformation.TeamName : playerTeamsDto.TeamDetails.Team.Title,
                                                ShirtNum = playerTeamsDto.ShirtNum,
                                                Position = playerTeamsDto.Position
                                            }).Distinct(new PlayerTeamComparer()).ToList();

            return playerTeams;
        }

        public Team UpdateTeamNameInSeason(Team team, int seasonId, string teamName)
        {
            var existedDetails = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId);
            if (existedDetails != null)
            {
                existedDetails.TeamName = teamName;
            }
            else
            {
                db.TeamsDetails.Add(new TeamsDetails
                {
                    TeamId = team.TeamId,
                    SeasonId = seasonId,
                    TeamName = teamName
                });
            }
            return team;
        }

        public void MoveTeams(int leagueId, int[] teams, int currentLeagueId, int seasonId)
        {
            //find teams to move
            var teamsToMove = db.Teams.Include(x => x.LeagueTeams).Where(x => teams.Contains(x.TeamId)).ToList();
            //find teams in league that will that will be moved
            var previousLeagueTeams =
                db.LeagueTeams.Where(
                    x => x.LeagueId == currentLeagueId && seasonId == x.SeasonId && teams.Contains(x.TeamId));
            foreach (var team in teamsToMove)
            {
                team.LeagueTeams.Add(new LeagueTeams
                {
                    LeagueId = leagueId,
                    TeamId = team.TeamId,
                    SeasonId = seasonId
                });

            }
            db.LeagueTeams.RemoveRange(previousLeagueTeams);
            db.SaveChanges();
        }

        /// <summary>
        /// Get all teams except current team
        /// </summary>
        /// <returns></returns>
        public List<TeamDto> GetAllExceptCurrent(int currentTeamId, int season, int? unionId)
        {
            var teams =
                db.Teams.Include(t => t.TeamsDetails)
                    .Include(t => t.LeagueTeams)
                     .Where(t => t.IsArchive == false && t.TeamId != currentTeamId).AsQueryable();
            if (unionId.HasValue)
            {
                teams = teams.Where(t => t.LeagueTeams.Any(f => f.SeasonId == season && f.Leagues.UnionId == unionId));
            }
            else
            {
                teams = teams.Where(t => t.LeagueTeams.Any(f => f.SeasonId == season));
            }
            // .Where(t => t.IsArchive == false && t.TeamId != currentTeamId && t.LeagueTeams.Any(f => f.SeasonId == season && f.Leagues.UnionId == unionId)).ToList();

            var teamsDto = new List<TeamDto>();
            var teamsList = teams.ToList();
            foreach (var t in teamsList)
            {
                var teamDto = new TeamDto();
                var teamDetail = t.TeamsDetails.FirstOrDefault(f => f.SeasonId == season);
                if (teamDetail != null)
                {
                    teamDto.Title = teamDetail.TeamName;
                }

                teamDto.TeamId = t.TeamId;
                teamDto.Title = t.Title;
                teamsDto.Add(teamDto);
            }
            return teamsDto;

        }

        public List<Team> GetTeamsByClubSeasonId(int clubId, int seasonId, int? unionId = null)
        {
            var teams = db.ClubTeams.Where(x => x.ClubId == clubId && x.SeasonId == seasonId).Include(x => x.Team).Include(x => x.Team.TeamsDetails).Select(t => t.Team).OrderBy(x => x.Title).AsQueryable();
            if (unionId.HasValue)
            {
                teams = teams.Where(x => x.LeagueTeams.Any(f => f.Leagues.UnionId == unionId));
            }
            var teamList = teams.ToList();
            foreach (var team in teamList)
            {
                var teamDetail = team.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId);
                if (teamDetail != null)
                {
                    team.Title = teamDetail.TeamName;
                }
            }
            return teamList;
        }

        public int GetNumberOfLeaguesAndClubs(int teamId)
        {
            return db.LeagueTeams.Count(lt => lt.TeamId == teamId) + db.ClubTeams.Count(ct => ct.TeamId == teamId);
        }

        public string GetGamesUrl(int teamId)
        {
            var team = db.Teams.FirstOrDefault(x => x.TeamId == teamId);
            if (team != null)
            {
                return team.GamesUrl;
            }

            return string.Empty;
        }

        public int SaveTeamStandingUrl(int teamId, int clubId, string url, string externalTeamName)
        {
            try
            {
                var teamStanding = db.TeamStandingGames.FirstOrDefault(x => x.TeamId == teamId && x.ClubId == clubId);
                if (teamStanding != null)
                {
                    teamStanding.GamesUrl = url;
                    teamStanding.ExternalTeamName = externalTeamName;

                    db.SaveChanges();
                    return teamStanding.Id;
                }

                var newTeamStanding = new TeamStandingGame();
                newTeamStanding.GamesUrl = url;
                newTeamStanding.TeamId = teamId;
                newTeamStanding.ClubId = clubId;
                newTeamStanding.ExternalTeamName = externalTeamName;
                db.TeamStandingGames.Add(newTeamStanding);
                db.SaveChanges();
                return newTeamStanding.Id;



            }
            catch (Exception)
            {

                return -1;
            }

        }

        public IList<string> GetTeamStandingsUrl()
        {
            return db.TeamStandingGames.Where(x => !string.IsNullOrEmpty(x.GamesUrl)).Select(x => x.GamesUrl).ToList();
        }
    }
}
