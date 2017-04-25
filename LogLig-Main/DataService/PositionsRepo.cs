﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppModel;

namespace DataService
{
    public class PositionsRepo : BaseRepo
    {
        public PositionsRepo() : base() { }
        public PositionsRepo(DataEntities db) : base(db) { }

        public Position GetById(int id)
        {
            return db.Positions.Find(id);
        }

        public IEnumerable<Position> GetBySection(int sectionId)
        {
            return db.Positions.Where(t => t.SectionId == sectionId && t.IsArchive == false).Distinct().ToList();
        }

        public IEnumerable<Position> GetByTeam(int teamId)
        {
            return (from t in db.Teams
                    from l in t.LeagueTeams
                    from p in l.Leagues.Union.Section.Positions
                    where p.IsArchive == false && t.TeamId == teamId
                    select p).Distinct().ToList();
        }

        public void Create(Position item)
        {
            db.Positions.Add(item);
        }
    }
}
