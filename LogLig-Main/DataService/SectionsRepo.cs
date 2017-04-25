using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppModel;

namespace DataService
{
    public class SectionsRepo : BaseRepo
    {

        public SectionsRepo() : base() { }
        public SectionsRepo(DataEntities db) : base(db) { }
        public IQueryable<Section> GetQuery(bool isArchive)
        {
            return db.Sections.AsQueryable();
        }

        public Section GetById(int id)
        {
            return db.Sections.Find(id);
        }

        public IEnumerable<Section> GetSections(int? langId)
        {
            var query = GetQuery(false);

            if (langId.HasValue)
                query = query.Where(t => t.LangId == langId);

            return query.OrderBy(t => t.Name).ToList();
        }

        public void CreateSection(Section item)
        {
            db.Sections.Add(item);
        }

        public Section GetByUnionId(int unionId)
        {
            return db.Unions.Where(t => t.UnionId == unionId).Select(t => t.Section)
                .FirstOrDefault();
        }

        public Section GetByLeagueId(int leagueId)
        {
            return db.Leagues.Where(t => t.LeagueId == leagueId)
                .Select(t => t.Union.Section).FirstOrDefault();
        }

        public Section GetByClubId(int clubId)
        {
            var club = db.Clubs.Where(c => c.ClubId == clubId).First();
            if (club.IsSectionClub ?? true)
            {
                return club.Section;
            }
            else
            {
                return club.Union.Section;
            }
        }
    }
}
