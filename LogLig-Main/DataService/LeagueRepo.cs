using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using AppModel;
using DataService.DTO;

namespace DataService
{
    internal class UnionSeason : IEquatable<UnionSeason>
    {
        public int? UnionId { get; set; }
        public int? SeasonId { get; set; }

        public bool Equals(UnionSeason other)
        {
            return UnionId.HasValue && SeasonId.HasValue
                && other.UnionId.HasValue && other.SeasonId.HasValue
                && other.UnionId == UnionId && other.SeasonId == SeasonId;
        }
    }

    public class LeagueRepo : BaseRepo
    {
        private IEnumerable<League> Leagues { get; set; }

        public LeagueRepo() : base() { }

        public LeagueRepo(DataEntities db) : base(db)
        {
            Leagues = GetQuery(false).ToList();
        }

        public IEnumerable<LeagueShort> GetLeaguesFilterList(int unionId, int seasonId)
        {
            var leagueShortList = new List<LeagueShort>();
            //  Fill in leagues list
            leagueShortList.Add(new LeagueShort
            {
                Name = "All",
                Id = -1,
                Check = false
            });
            leagueShortList.AddRange(
                GetAll()
                .Where(x => x.UnionId == unionId && !x.IsArchive && x.SeasonId == seasonId)
                .Select(x => new LeagueShort
                {
                    Id = x.LeagueId,
                    Name = x.Name,
                    UnionId = x.UnionId,
                    Check = false
                }));
            return leagueShortList;
        }

        public IEnumerable<League> GetByTeamAndSeason(int teamId, int seasonId)
        {
            return db.Leagues.Where(l => l.LeagueTeams.Any(lt => lt.TeamId == teamId && lt.SeasonId == seasonId));
        }

        public IList<LeagueShort> GetByTeamAndSeasonShort(int teamId, int seasonId)
        {
            return GetByTeamAndSeason(teamId, seasonId).Select(l => new LeagueShort {
                Id = l.LeagueId,
                Name = l.Name,
                UnionId = l.UnionId,
                Check = true
            }).ToList();
        }

        public IQueryable<League> GetQuery(bool isArchive)
        {
            return db.Leagues.Where(t => t.IsArchive == isArchive);
        }

        public IQueryable<League> GetLastSeasonLeaguesBySection(int sectionId)
        {
            var allLeagues = db.Unions.Where(u => u.SectionId == sectionId && !u.IsArchive)
                    .Join(db.Seasons, u => u.UnionId, s => s.UnionId,
                        (u, s) => new { unionId = u.UnionId, seasonId = s.Id })
                    //  Get last season in each union 
                    .GroupBy(us => us.unionId, us => us.seasonId,
                        (key, g) => new UnionSeason { UnionId = key, SeasonId = g.Max() })
                    .Join(db.Leagues,
                        us => new UnionSeason { UnionId = us.UnionId, SeasonId = us.SeasonId },
                        l => new UnionSeason { UnionId = l.UnionId, SeasonId = l.SeasonId }, (us, l) => l);
            return allLeagues;
        }

        public IEnumerable<League> GetByUnion(int unionId, int seasonId)
        {
            return db.Leagues
                .Include(t => t.Gender)
                .Include(t => t.Age)
                .Where(t => t.IsArchive == false && t.UnionId == unionId && t.SeasonId == seasonId)
                .ToList();
        }

        public IEnumerable<League> GetByClub(int clubId, int seasonId)
        {
            return db.Leagues
                .Include(t => t.Gender)
                .Include(t => t.Age)
                .Where(t => t.IsArchive == false && t.ClubId == clubId && t.SeasonId == seasonId)
                .ToList();
        }

        public League GetById(int id)
        {
            return db.Leagues.Find(id);
        }

        public string GetGameAliasByLeague(League league)
        {
            return league?.Union?.Section?.Alias;
        }
        public string GetGameAliasByLeagueId(int leagueId)
        {
            var league = GetById(leagueId);
            return GetGameAliasByLeague(league);
        }
        public bool IsBasketBallOrWaterPoloLeague(int leagueId)
        {
            var alias = GetGameAliasByLeagueId(leagueId);
            return alias == GamesAlias.BasketBall || alias == GamesAlias.WaterPolo;
        }
        public League GetByIdForRanks(int id)
        {
            return
                db.Leagues.Include(x => x.Games)
                    .Include(x => x.Stages)
                    .Include(x => x.Stages.Select(t => t.Groups))

                    .FirstOrDefault(x => x.LeagueId == id);
        }

        public LeaguesDoc GetTermsDoc(int leagueId)
        {
            return db.LeaguesDocs.FirstOrDefault(t => t.LeagueId == leagueId);
        }

        public LeaguesDoc GetDocById(int id)
        {
            return db.LeaguesDocs.Find(id);
        }

        public void CreateDoc(LeaguesDoc doc)
        {
            db.LeaguesDocs.Add(doc);
        }

        public void Create(League item)
        {
            item.CreateDate = DateTime.Now;

            db.Leagues.Add(item);
            db.SaveChanges();

            var game = new Game
            {
                LeagueId = item.LeagueId,
                GameDays = "5,6",
                StartDate = DateTime.Now,
                GamesInterval = "01:00",
                NumberOfSequenceRounds = 1,
                PointsWin = 2,
                PointsDraw = 0,
                PointsLoss = 1,
                PointsTechWin = 2,
                PointsTechLoss = 0,
                SortDescriptors = "0,1,2"
            };

            db.Games.Add(game);
            db.SaveChanges();
        }

        public IEnumerable<ListItemDto> FindByName(string name, int num)
        {
            return GetQuery(false).ToList().Where(t => t.Name.Contains(name))
                  .Select(t => new ListItemDto { Id = t.LeagueId, Name = t.Name, SeasonId = t.SeasonId })
                  .OrderBy(t => t.Name)
                  .Take(num)
                  .ToList();
        }

        public IEnumerable<Job> GetLeagueJobs(int leagueId)
        {
            return (from l in db.Leagues
                    from j in l.Union.Section.Jobs
                    where l.LeagueId == leagueId && j.IsArchive == false
                    select j).ToList();
        }

        public List<League> GetByManagerId(int managerId, int? seasonId)
        {
            if (seasonId != null)
            {
                return db.UsersJobs
                    .Where(j => j.UserId == managerId)
                    .Select(j => j.League)
                    .Where(l => l != null && l.SeasonId == seasonId)
                    .Distinct()
                    .OrderBy(u => u.Name)
                    .ToList();
            }
            else
            {
                return db.UsersJobs
                    .Where(j => j.UserId == managerId)
                    .Select(j => j.League)
                    .Where(l => l != null)
                    .Distinct()
                    .OrderBy(u => u.Name)
                    .ToList();
            }
        }

        public List<League> GetAll()
        {
            return db.Leagues.ToList();
        }

        public List<League> GetLeaguesForMoveByUnionSeasonId(int unionId, int seasonId, int leagueId)
        {
            return db.Leagues.Where(x => x.UnionId == unionId && x.SeasonId == seasonId && x.LeagueId != leagueId).AsEnumerable()
                 .Select(x => new League
                 {
                     LeagueId = x.LeagueId,
                     Name = x.Name
                 }).ToList();
        }

        public List<League> GetLeaguesBySesonUnion(int unionId, int seasonId)
        {
            return db.Leagues.Where(x => x.UnionId == unionId && x.SeasonId == seasonId && x.IsArchive == false).AsEnumerable()
                .Select(x => new League
                {
                    LeagueId = x.LeagueId,
                    Name = x.Name,
                    SeasonId = x.SeasonId
                }).ToList();
        }

        public League GetByLeagueSeasonId(int leagueId, int seasonId)
        {
            return db.Leagues.FirstOrDefault(x => x.LeagueId == leagueId && x.SeasonId == seasonId);
        }

        /// <summary>
        /// Get all teams in league except current team
        /// </summary>
        /// <param name="managerId"></param>
        /// <param name="currentTeamId"></param>
        /// <param name="seasonId"></param>
        /// <returns></returns>
        public List<TeamDto> GetTeamsByManager(int managerId, int currentTeamId, int seasonId, int? unionId)
        {
            var teams = db.UsersJobs
                .Where(j => j.UserId == managerId)
                .Where(j => j.League.SeasonId == seasonId)
                .SelectMany(j => j.League.LeagueTeams)
                .Where(l => l != null && l.Leagues.IsArchive == false && l.TeamId != currentTeamId).AsQueryable();
            if (unionId.HasValue)
            {
                teams = teams.Where(x => x.Leagues.UnionId == unionId);
            }
            var teamDto =
             teams.OrderBy(l => l.TeamId)
                .Distinct()
                .Select(x => new TeamDto
                {
                    TeamId = x.TeamId,
                    Title = x.Teams.Title
                }).ToList();

            return teamDto;
        }

    }
}