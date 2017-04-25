using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using AppModel;


namespace DataService
{
    public class StagesRepo : BaseRepo
    {

        public StagesRepo() : base()
        {

        }

        public StagesRepo(DataEntities db) : base(db)
        {
        }

        private IQueryable<Stage> GetQuery(int leagueId)
        {
            return db.Stages.Include(t => t.Groups).Where(t => t.IsArchive == false && t.LeagueId == leagueId);
        }

        public Stage Create(int leagueId)
        {
            int maxNum = GetQuery(leagueId).Max(t => (int?)t.Number) ?? 0;
            var s = new Stage();
            s.Number = maxNum + 1;
            s.LeagueId = leagueId;

            return db.Stages.Add(s);
        }

        public IEnumerable<Stage> GetAll(int leagueId, int?seasonId = null)
        {
            if (seasonId.HasValue)
            {

                var results = GetQuery(leagueId).Include(x => x.Groups).Include(f => f.Groups.Select(x => x.PlayoffBrackets)).Include(f => f.Groups.Select(x => x.PlayoffBrackets)).ToList();

                var teams = results.SelectMany(x => x.Groups).SelectMany(x => x.PlayoffBrackets).ToList();
                foreach (var team in teams)
                {
                    var firstTeamDetails = team.FirstTeam?.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    if (firstTeamDetails != null)
                    {
                        team.FirstTeam.Title = firstTeamDetails.TeamName;
                    }

                    var secondTeamDetails = team.SecondTeam?.TeamsDetails.FirstOrDefault(t => t.SeasonId == seasonId.Value);
                    if (secondTeamDetails != null)
                    {
                        team.SecondTeam.Title = secondTeamDetails.TeamName;
                    }
                }
                return results;
            }

            return GetQuery(leagueId).ToList();
        }

        public int CountStage(int idLeague)
        {
            return db.Stages.Count(x => x.LeagueId == idLeague && !x.IsArchive);
        }

        

        public Stage GetById(int id)
        {
            return db.Stages.Find(id);
        }

        public void DeleteAllGameCycles(int stageId)
        {
            var games = db.GamesCycles.Where(t => t.StageId == stageId).Include(f=>f.Users).ToList();
            foreach (var g in games)
            {
                //game fans
                g.Users.Clear();
                db.GamesCycles.Remove(g);


            }
            db.SaveChanges();
        }

        public void DeleteAllGroups(int stageId)
        {
            var groups = db.Groups.Where(t => t.StageId == stageId);
            foreach (var g in groups)
            {
                g.IsArchive = true;
            }
            Save();
        }

        internal List<Stage> GetStagesForLeague(int leagueId)
        {
            return db.Stages.Where(t => t.LeagueId == leagueId && t.IsArchive == false)
                   .OrderByDescending(t => t.StageId).ToList();
        }

        internal Stage GetLastStageForLeague(int leagueId)
        {
            return GetStagesForLeague(leagueId).FirstOrDefault();
        }

        internal DateTime? GetLastGameDateFromStageBeforeLast(int leagueId)
        {
            List<Stage> stages = GetStagesForLeague(leagueId);
            if (stages.Count() > 1)
            {
                var forLastStage = stages.ElementAt(1);
                var stageGames = forLastStage.GamesCycles.OrderBy(c => c.StartDate);
                var lastGame = stageGames.LastOrDefault();
                if (lastGame != null)
                {
                    return lastGame.StartDate.AddDays(1);
                }
            }
            return null;
        }
    }
}
