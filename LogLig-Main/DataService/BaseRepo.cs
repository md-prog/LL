using System;
using System.Collections.Generic;
using System.Linq;
using AppModel;

namespace DataService
{
    public class BaseRepo : IDisposable
    {
        internal DataEntities db;

        public BaseRepo()
        {
            db = new DataEntities();
            // add for production
            //db.Database.Connection.ConnectionString = ConnectionHelper.GetConnectionString();
            db.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public BaseRepo(DataEntities db)
        {
            this.db = db;
            // add for production
            //db.Database.Connection.ConnectionString = ConnectionHelper.GetConnectionString();
            db.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public void Save()
        {
            db.SaveChanges();
        }

        public void Dispose()
        {
            if (db != null)
            {
                db.Dispose();
                db = null;
            }
        }

        public Club GetClubById(int clubId)
        {
            return db.Clubs.Where(c => c.ClubId == clubId).First();
        }

        public Section GetSectionByTeamId(int teamId)
        {
            Section section;

            //  Try find section by league
            section = db.LeagueTeams.Where(lt => lt.TeamId == teamId).FirstOrDefault()?.Leagues?.Union?.Section;
            //  Try by section club
            if (section == null)
            {
                var club = db.ClubTeams.Where(ct => ct.TeamId == teamId).FirstOrDefault()?.Club;
                //  Try by union club
                if (club != null)
                {
                    section = club.IsSectionClub.Value ? club.Section : club.Union.Section;
                }
            }

            return section;
        }

        public IEnumerable<Language> GetLanguages()
        {
            return db.Languages.ToList();
        }

        public IEnumerable<Country> GetCountries()
        {
            return db.Countries.OrderBy(t => t.Name).ToList();
        }

        public IEnumerable<Age> GetAges()
        {
            return db.Ages.OrderBy(t => t.AgeId).ToList();
        }

        public IEnumerable<Gender> GetGenders()
        {
            return db.Genders.ToList();
        }
    }

    public class ListItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public int? SeasonId { get; set; }
    }
}
