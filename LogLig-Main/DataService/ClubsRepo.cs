using AppModel;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using DataService.DTO;

namespace DataService
{
    public class ClubsRepo : BaseRepo
    {
        public ClubsRepo() : base() { }
        public ClubsRepo(DataEntities db) : base(db) { }
        public Club GetById(int id)
        {
            return db.Clubs.Find(id);
        }

        public IEnumerable<Club> GetBySection(int sectionId)
        {
            return db.Clubs.Where(c => c.SectionId == sectionId && !c.IsArchive).Distinct().ToList();
        }

        public IEnumerable<Club> GetByUnion(int unionId, int? seasonId)
        {
            return db.Clubs.Where(c => c.UnionId == unionId && (!seasonId.HasValue || c.SeasonId == seasonId) && !c.IsArchive)
                .Distinct().ToList();
        }

        public void Create(Club club)
        {
            if (!club.UnionId.HasValue)
            {
                var season = new Season();
                season.StartDate = DateTime.UtcNow;
                season.EndDate = DateTime.UtcNow.AddYears(1);
                season.ClubId = club.ClubId;
                season.Name = $"{season.StartDate.Year.ToString()}-{season.EndDate.Year.ToString()}";
                season.Description = $"Season {season.Name} of club {club.Name}";
                club.Seasons.Add(season);
            }
            club.CreateDate = DateTime.UtcNow;
            db.Clubs.Add(club);
        }

        public void CreateTeamClub(ClubTeam clubTeam)
        {
            db.ClubTeams.Add(clubTeam);
        }

        public ClubTeam GetTeamClub(int clubId, int teamId, int seasonId)
        {
            return db.ClubTeams.FirstOrDefault(x => x.ClubId == clubId &&
                                                    x.TeamId == teamId &&
                                                    x.SeasonId == seasonId);
        }

        public int GetNumberOfClubTeams(int clubId, int teamId)
        {
            return db.ClubTeams.Count(x => x.ClubId == clubId && x.TeamId == teamId);
        }

        public bool IsExistClubTeamForCurrentSeason(int clubId, int teamId, int? seasonId)
        {
            return db.ClubTeams.Any(t => t.ClubId == clubId && t.TeamId == teamId && t.SeasonId == seasonId);
        }

        public void RemoveTemClub(ClubTeam item)
        {
            db.ClubTeams.Remove(item);
        }

        public List<string> GetClubTeamGamesUrl()
        {
            var gamesUrl = db.Teams.Where(x => !string.IsNullOrEmpty(x.GamesUrl) && x.ClubTeams.Any()).Select(x => x.GamesUrl).ToList();
            return gamesUrl;
        }

        public IEnumerable<Club> GetByTeamAnsSeason(int teamId, int seasonId)
        {
            return db.Clubs.Where(c => c.ClubTeams.Any(ct => ct.TeamId == teamId && ct.SeasonId == seasonId));
        }

        public IList<ClubShort> GetByTeamAndSeasonShort(int teamId, int seasonId)
        {
            return GetByTeamAnsSeason(teamId, seasonId).Select(c => new ClubShort { Id = c.ClubId, Name = c.Name}).ToList();
        }
    }
}
