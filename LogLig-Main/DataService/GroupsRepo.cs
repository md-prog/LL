using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Entity;
using AppModel;

namespace DataService
{
    public class GroupTeam
    {
        public int GroupId { get; set; }
        public int TeamId { get; set; }
        public int StageId { get; set; }
        public string Title { get; set; }

        public int? Pos { get; set; }
    }

    public class GroupsRepo : BaseRepo
    {
        public GroupsRepo() : base()
        {
        }


        public GroupsRepo(DataEntities db) : base(db)
        {
        }

        public Group GetById(int id)
        {
            return db.Groups.Find(id);
        }

        public IEnumerable<GamesType> GetGamesTypes()
        {
            return db.GamesTypes.Where(t => t.TypeId <= 3).ToList();
        }

        public void Create(Group item)
        {
            db.Groups.Add(item);
        }

        public IEnumerable<Group> GetAll(int leagueId)
        {
            return db.Groups.Include(t => t.GamesType).Where(t => t.IsArchive == false && t.Stage.LeagueId == leagueId).ToList();
        }

        //public IEnumerable<Team> GetGroupsTeams(int leagueId)
        //{
           
        //    return db.Teams.Include(t => t.GroupsTeams).Where(t => t.IsArchive == false).ToList();
        //}

        public IEnumerable<Group> GetLeagueGroups(int leagueId)
        {
            return (from g in db.Groups
                    from gt in g.GroupsTeams
                    let t = gt.Team
                    from l in t.LeagueTeams
                    where t.IsArchive == false && 
                    l.LeagueId == leagueId && 
                    g.IsArchive == false
                    select g).Distinct().ToList();
        }

        public Dictionary<string, int> UpdateTeams(Group group, int[] teams)
        {
            var listTeams = new Dictionary<string, int>();
            foreach (var t in group.GroupsTeams.ToList())
                group.GroupsTeams.Remove(t);

            base.Save();

            if (teams != null)
            {
                for (int i = 0; i < teams.Count(); i++)
                {
                    var gt = new GroupsTeam
                    {
                        Pos = i,
                        TeamId = teams[i],
                        GroupId = group.GroupId
                    };
                    var t = teams[i];
                    var team = db.Teams.FirstOrDefault(x => x.TeamId == t);
                    listTeams.Add(team.Title, 0);
                    db.GroupsTeams.Add(gt);
                    db.SaveChanges();
                }
            }
            return listTeams;
        }

        public IList<GroupTeam> GetTeamsGroups(int leagueId)
        {
            return (from g in db.Groups
                    from gt in g.GroupsTeams
                    let t = gt.Team
                    from l in t.LeagueTeams
                    where t.IsArchive == false && l.LeagueId == leagueId && g.IsArchive == false
                    select new GroupTeam
                    {
                        GroupId = g.GroupId,
                        TeamId = t.TeamId,
                        StageId = g.StageId,
                        Title = t.Title,
                        Pos = gt.Pos
                    }).OrderBy(gt => gt.Pos).ToList();
        }

        public IList<GroupTeam> GetTeamsByGroup(int groupId)
        {
            return (from g in db.Groups
                    from gt in g.GroupsTeams
                    let t = gt.Team
                    from l in t.LeagueTeams
                    where t.IsArchive == false && g.GroupId == groupId && g.IsArchive == false
                    select new GroupTeam
                    {
                        GroupId = g.GroupId,
                        TeamId = t.TeamId,
                        StageId = g.StageId,
                        Title = t.Title
                    }).ToList();
        }

        public int[] GetGroupsArr(int leagueId)
        {
            return db.Groups.Where(t => t.IsArchive == false && t.Stage.LeagueId == leagueId)
                .Select(t => t.GroupId).ToArray();
        }

        public IQueryable<GroupsTeam> GetGroupTeamsByGroupId(int groupId)
        {
            return db.GroupsTeams.Where(gt => gt.GroupId == groupId);
        }
    }
}
