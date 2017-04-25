using System.Collections.Generic;
using System.Linq;
using System.Data.Entity;
using AppModel;

namespace DataService
{
    public class UserSearchItem
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string IdentNum { get; set; }
    }

    public class UsersRepo : BaseRepo
    {
        public User GetByUsername(string userName)
        {
            return db.Users.Where(t => t.UserName == userName && t.IsArchive == false).FirstOrDefault();
        }

        public User FindByName(string role, string name)
        {
            return db.Users.Where(t => t.FullName == name
                && t.UsersType.TypeRole == role
                && t.IsActive == true).FirstOrDefault();
        }

        public IQueryable<User> GetQuery()
        {
            return db.Users.AsQueryable();
        }

        public IEnumerable<User> GetAll()
        {
            return db.Users.Include(t => t.UsersType).OrderBy(t => t.UserName).ToList();
        }

        public User GetById(int id)
        {
            return db.Users.Find(id);
        }

        public List<PlayerHistory> GetPlayerHistory(int userId, int season)
        {
            var history = db.PlayerHistory.Where(x => x.UserId == userId && x.SeasonId == season).ToList();
            return history;
        }

        public User GetByIdentityNumber(string pId)
        {
            return db.Users.Where(u => u.IdentNum == pId).FirstOrDefault();
        }

        public User GetByEmail(string email)
        {
            return db.Users.Where(u => u.Email == email).FirstOrDefault();
        }

        public bool IsUsernameExists(string userName, int id)
        {
            return db.Users.Any(t => t.UserName == userName && t.UserId != id);
        }

        public IEnumerable<UsersType> GetTypes()
        {
            return db.UsersTypes.ToList();
        }

        public void Create(User item)
        {
            db.Users.Add(item);
            db.SaveChanges();
        }

        public IEnumerable<ListItemDto> SearchUser(string role, string name, int num)
        {
            return db.Users.Where(t => t.FullName.Contains(name) && t.UsersType.TypeRole == role && t.IsArchive == false)
                .OrderBy(t => t.FullName)
                .Select(t => new ListItemDto
                {
                    Id = t.UserId,
                    Name = t.FullName,
                }).Take(num).ToList();
        }

        public IEnumerable<ListItemDto> SearchSectionUser(int sectionId, string role, string name, int num)
        {
            return (from j in db.Jobs
                    from uj in j.UsersJobs
                    let u = uj.User
                    where j.SectionId == sectionId
                        && u.FullName.Contains(name)
                        && u.UsersType.TypeRole == role
                        && u.IsArchive == false
                    orderby u.FullName
                    select new ListItemDto { Id = u.UserId, Name = u.FullName }).Distinct().Take(num).ToList();
        }

        public IEnumerable<UserSearchItem> SearchUserByIdent(int sectionId, string role, string identNum, int num)
        {
            return (from t in db.Teams
                    from l in t.LeagueTeams
                    from tp in t.TeamsPlayers
                    let p = tp.User
                    let u = l.Leagues.Union
                    where u.SectionId == sectionId && p.IdentNum.Contains(identNum) && p.UsersType.TypeRole == role
                        && p.IsArchive == false
                        && p.IsActive == true
                    orderby p.FullName
                    select new UserSearchItem { UserId = p.UserId, IdentNum = p.IdentNum, FullName = p.FullName })
                    .Distinct().Take(num).ToList();
        }

        public string GetTopLevelJob(int userId)
        {
            return (from u in db.Users
                    from j in u.UsersJobs
                    let r = j.Job.JobsRole
                    where u.UserId == userId
                    orderby r.Priority descending
                    select r.RoleName).FirstOrDefault();
        }

        public IEnumerable<User> GetLeagueWorkers(int leagueId, string roleName)
        {
            return (from u in db.Users
                    from uj in u.UsersJobs
                    let r = uj.Job.JobsRole
                    where uj.LeagueId == leagueId && u.IsArchive == false && r.RoleName == roleName
                    select u).ToList();
        }

        public IEnumerable<User> GetUnionWorkers(int unionId, string roleName)
        {
            return (from u in db.Users
                    from uj in u.UsersJobs
                    let r = uj.Job.JobsRole
                    where uj.UnionId == unionId && u.IsArchive == false && r.RoleName == roleName
                    select u).ToList();
        }

        public IEnumerable<User> GetUnionAndLeageReferees(int unionId, int leagueId)
        {
            return (from u in db.Users
                    from uj in u.UsersJobs
                    let r = uj.Job.JobsRole
                    where
                    (uj.UnionId == unionId || uj.LeagueId == leagueId) &&
                    u.IsArchive == false &&
                    r.RoleName == JobRole.Referee
                    select u)
                    .Distinct().OrderBy(r => r.FullName)
                    .ToList();
        }
    }
}
