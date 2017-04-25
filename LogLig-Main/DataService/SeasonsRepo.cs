using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AppModel;
using System;

namespace DataService
{
    public class SeasonsRepo : BaseRepo
    {
        public SeasonsRepo() : base() { }

        public SeasonsRepo(DataEntities db) : base(db) { }

        public IEnumerable<Season> GetSeasons()
        {
            return db.Seasons;
        }

        public Season GetById(int Id)
        {
            return db.Seasons.Where(s => s.Id == Id).First();
        }

        public IEnumerable<Season> GetAllCurrent(DateTime date)
        {
            return db.Seasons.Where(s => s.StartDate <= date && s.EndDate >= date);
        }

        public IEnumerable<Season> GetAllCurrent()
        {
            return GetAllCurrent(DateTime.Now);
        }

        public IEnumerable<Season> GetSeasonsByUnion(int unionId)
        {
            return db.Seasons.Where(x => x.UnionId == unionId).ToList();
        }

        public IEnumerable<Season> GetSeasonsByClub(int clubId)
        {
            return db.Seasons.Where(x => x.ClubId == clubId).ToList();
        }

        public void Create(Season model)
        {
            db.Seasons.Add(model);
        }

        public void Duplicate(int[] leagueIds, int lastSeasonId, int newSeasonId)
        {
            var leagues = db.Leagues.Include(t => t.LeagueTeams).Where(x => leagueIds.Contains(x.LeagueId)).ToList();
            var auditoriums = db.Auditoriums.Where(x => x.SeasonId == lastSeasonId).ToList();
            var copiedLeagues = new List<League>();
            var copiedAuditoriums = new List<Auditorium>();
            var leagueTeams = new List<LeagueTeams>();
            var teamPlayers = new List<TeamsPlayer>();

            foreach (var league in leagues)
            {
                league.SeasonId = newSeasonId;
                league.UsersJobs = null;

                copiedLeagues.Add(league);
            }

            foreach (var audiotorium in auditoriums)
            {
                audiotorium.SeasonId = newSeasonId;
                copiedAuditoriums.Add(audiotorium);
            }
            var lt = copiedLeagues.SelectMany(t => t.LeagueTeams.Where(x=>x.Teams.IsArchive == false)).ToList();
            foreach (var cop in lt)
            {
                cop.SeasonId = newSeasonId;
                leagueTeams.Add(cop);
            }

            foreach (var tplayer in leagueTeams.SelectMany(t => t.Teams.TeamsPlayers))
            {
                tplayer.SeasonId = newSeasonId;
                teamPlayers.Add(tplayer);
            }
            
            db.Leagues.AddRange(copiedLeagues);
            db.Auditoriums.AddRange(copiedAuditoriums);
            db.LeagueTeams.AddRange(leagueTeams);
            db.TeamsPlayers.AddRange(teamPlayers);
            db.SaveChanges();

        }

        public Season Get(int seasonId, int? unionId = null)
        {
            return db.Seasons.FirstOrDefault(x => x.Id == seasonId && (!unionId.HasValue || x.UnionId == unionId));
        }

        public int GetLastSeasonByCurrentUnionId(int unionId)
        {
            int? seasonId = db.Seasons.Where(x => x.UnionId == unionId).OrderByDescending(x => x.Id).FirstOrDefault()?.Id;
            return seasonId ?? -1;
        }

        public int GetLastSeasonIdByCurrentClubId(int clubId)
        {
            var club = db.Clubs.Where(c => c.ClubId == clubId).First();
            return GetLastSeasonByCurrentClub(club).Id;
        }

        public Season GetLastSeasonByCurrentClub(Club club)
        {
            if (club.IsSectionClub ?? true)
            {
                return db.Seasons.Where(x => x.ClubId == club.ClubId).OrderByDescending(x => x.Id).First();
            }
            else
            {
                return db.Seasons.Where(s => s.UnionId == club.UnionId).OrderByDescending(s => s.Id).First();
            }
        }

        public List<Season> GetClubsSeasons(int clubId)
        {
            return db.Seasons.Where(x => x.ClubId == clubId).OrderBy(x => x.Name).ToList();
        }

        public int? GetLastSeasonByLeagueId(int leagueId)
        {
            int? unionId = db.Leagues.Where(x => x.LeagueId == leagueId)
                                   .Select(x => x.UnionId)
                                   .FirstOrDefault();

            if (unionId != null)
            {
                int? currentSeasonId = GetLasSeasonByUnionId(unionId.Value);
                return currentSeasonId;
            }

            return null;
        }

        public int? GetLasSeasonByUnionId(int unionId)
        {
            int? seasonId = db.Seasons.Where(x => x.UnionId == unionId)
                               .Select(x => x.Id)
                               .OrderByDescending(x => x)
                               .FirstOrDefault();
            return seasonId;
        }

        public Season GetLastSeason()
        {
            var lastSeason =  db.Seasons.Where(x => !x.ClubId.HasValue).OrderByDescending(x => x.Id).First();
            return lastSeason;
        }

        public Season GetLastClubSeason()
        {
            var lastSeason = db.Seasons.Where(x => x.ClubId.HasValue).OrderByDescending(x => x.Id).FirstOrDefault();
            return lastSeason;
        }


        public string GetGameAliasBySeasonId(int seasonId)
        {
            var season = db.Seasons.Where(s => s.Id == seasonId).FirstOrDefault();
            return GetGameAliasBySeason(season);
        }
        public string GetGameAliasBySeason(Season season)
        {
            return season?.Union?.Section?.Alias;
        }

        public bool IsBasketBallOrWaterPoloSeason(int seasonId)
        {
            var alias = GetGameAliasBySeasonId(seasonId);
            return alias == GamesAlias.BasketBall || alias == GamesAlias.WaterPolo;
        }
    }
}
