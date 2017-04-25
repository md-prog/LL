using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using AppModel;

namespace DataService
{
    public class UnionsRepo : BaseRepo
    {
        public UnionsRepo() : base() { }
        public UnionsRepo(DataEntities db) : base(db) { }

        public IEnumerable<Union> GetBySection(int sectionId)
        {
            return db.Unions.Where(t => t.SectionId == sectionId && t.IsArchive == false)
                .OrderBy(t => t.Name)
                .ToList();
        }

        public Union GetById(int id)
        {
            return db.Unions.Find(id);
        }

        public void Create(Union item)
        {
            db.Unions.Add(item);
        }

        public Section GetSectionByUnionId(int unionId)
        {
            var section = db.Unions.Include(x => x.Section).FirstOrDefault(x => x.UnionId == unionId);
            if (section != null)
                return section.Section;
            return new Section();
        }

        public UnionsDoc GetTermsDoc(int unionId)
        {
            return db.UnionsDocs.FirstOrDefault(t => t.UnionId == unionId);
        }

        public UnionsDoc GetDocById(int id)
        {
            return db.UnionsDocs.Find(id);
        }

        public void CreateDoc(UnionsDoc doc)
        {
            db.UnionsDocs.Add(doc);
        }


        public List<Union> GetByManagerId(int managerId)
        {
            return db.UsersJobs
                .Where(j => j.UserId == managerId)
                .Select(j => j.Union)
                .Where(u => u != null)
                .Distinct()
                .OrderBy(u => u.Name)
                .ToList();
        }
    }
}
