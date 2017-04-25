using System;
using System.Collections.Generic;
using System.Linq;
using AppModel;

namespace DataService
{
    public class UserJobDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string JobName { get; set; }
        public string FullName { get; set; }
    }

    public class JobsRepo : BaseRepo
    {
        public JobsRepo() : base() { }
        public JobsRepo(DataEntities db) : base(db) { }

        public Job GetById(object id)
        {
            return db.Jobs.Find(id);
        }

        public IQueryable<Job> GetQuery(bool isArchive)
        {
            return db.Jobs.Where(t => t.IsArchive == isArchive);
        }

        public IEnumerable<Job> GetAll()
        {
            return db.Jobs;
        }

        public void Delete(Job entity)
        {
            db.Jobs.Remove(entity);
        }

        public void Add(Job entity)
        {
            db.Jobs.Add(entity);
        }

        public IEnumerable<JobsRole> GetRoles()
        {
            return db.JobsRoles.ToList();
        }

        public IEnumerable<Job> GetBySection(int sectionId)
        {
            return GetQuery(false).Where(t => t.SectionId == sectionId).ToList();
        }

        public IEnumerable<Job> GetByUnion(int unionId)
        {
            return (from j in GetQuery(false)
                    from u in j.Section.Unions
                    where u.UnionId == unionId &&
                    (j.JobsRole.RoleName == JobRole.UnionManager || j.JobsRole.RoleName == JobRole.Referee)
                    select j).ToList();
        }

        public IEnumerable<Job> GetByLeague(int leagueId)
        {
            return (from j in GetQuery(false)
                    from u in j.Section.Unions
                    from l in u.Leagues
                    where l.LeagueId == leagueId &&
                    (j.JobsRole.RoleName == JobRole.LeagueManager || j.JobsRole.RoleName == JobRole.Referee)
                    select j).ToList();
        }

        public IEnumerable<Job> GetByTeam(int teamId)
        {
            var sectionId = GetSectionByTeamId(teamId).SectionId;

            return db.Jobs.Where(jb => !jb.IsArchive && jb.SectionId == sectionId)
                .Join(db.JobsRoles.Where(jr => jr.RoleName == JobRole.TeamManager),
                        jb => jb.RoleId, jr => jr.RoleId, (jb, jr) => jb).ToList();
        }

        public void AddUsersJob(UsersJob job)
        {
            db.UsersJobs.Add(job);
        }

        public IEnumerable<UserJobDto> GetTeamUsersJobs(int teamId)
        {
            return (from u in db.Users
                    from j in u.UsersJobs
                    where j.TeamId == teamId && u.IsArchive == false
                    select new UserJobDto
                    {
                        Id = j.Id,
                        JobName = j.Job.JobName,
                        UserId = u.UserId,
                        FullName = u.FullName
                    }).ToList();
        }

        public IEnumerable<UserJobDto> GetUnionUsersJobs(int unionId)
        {
            return (from u in db.Users
                    from j in u.UsersJobs
                    where j.UnionId == unionId && u.IsArchive == false
                    select new UserJobDto
                    {
                        Id = j.Id,
                        JobName = j.Job.JobName,
                        UserId = u.UserId,
                        FullName = u.FullName
                    }).ToList();
        }

        public IEnumerable<UserJobDto> GetLeagueUsersJobs(int leagueId)
        {
            return (from u in db.Users
                    from j in u.UsersJobs
                    where j.LeagueId == leagueId && j.TeamId == null && u.IsArchive == false
                    select new UserJobDto
                    {
                        Id = j.Id,
                        JobName = j.Job.JobName,
                        UserId = u.UserId,
                        FullName = u.FullName
                    }).ToList();
        }

        public IEnumerable<UserJobDto> GetClubUsersJobs(int clubId)
        {
            return (from u in db.Users
                    from j in u.UsersJobs
                    from tc in j.Team.ClubTeams
                    where tc.ClubId == clubId
                    select new UserJobDto
                    {
                        Id = j.Id,
                        JobName = j.Job.JobName,
                        UserId = u.UserId,
                        FullName = u.FullName
                    }).ToList();
        }

        public int CountOfficialsInClub(int clubId)
        {
            return (from userJob in db.UsersJobs
                    join user in db.Users on userJob.UserId equals user.UserId
                    join job in db.Jobs on userJob.JobId equals job.JobId
                    where userJob.ClubId.HasValue && userJob.ClubId == clubId &&
                          user.IsArchive == false
                    select userJob).Count();
        }

        public int CountOfficialsInLeague(int LeagueId)
        {
            return (from userJob in db.UsersJobs
                    join user in db.Users on userJob.UserId equals user.UserId
                    join job in db.Jobs on userJob.JobId equals job.JobId
                    where userJob.LeagueId.HasValue && userJob.LeagueId == LeagueId &&
                          user.IsArchive == false
                    select userJob).Count();
        }

        public UsersJob GetUsersJobById(int id)
        {
            return db.UsersJobs.Find(id);
        }

        public void RemoveUsersJob(UsersJob job)
        {
            db.UsersJobs.Remove(job);
        }


        public void RemoveUsersJob(int id)
        {
            var userJob = db.UsersJobs.FirstOrDefault(x => x.Id == id);
            db.UsersJobs.Remove(userJob);
            db.SaveChanges();
        }

        public bool IsUserInJob(LogicaName logicaName, int relevantEntityId, int jobId, int userId)
        {
            switch (logicaName)
            {
                case LogicaName.Union:
                    return db.UsersJobs.Any(uj => uj.JobId == jobId && uj.UserId == userId && uj.UnionId == relevantEntityId);
                case LogicaName.League:
                    return db.UsersJobs.Any(uj => uj.JobId == jobId && uj.UserId == userId && uj.LeagueId == relevantEntityId);
                case LogicaName.Team:
                    return db.UsersJobs.Any(uj => uj.JobId == jobId && uj.UserId == userId && uj.TeamId == relevantEntityId);
            }
            return false;
        }

        public List<Job> GetClubJobs(int clubId)
        {
            Club club = db.Clubs.FirstOrDefault(x => x.ClubId == clubId);
            if (club == null) return new List<Job>();

            int? sectionId = null;
            if (club.IsSectionClub ?? true)
            {
                sectionId = club.SectionId;
            }
            else
            {
                sectionId = club.Union.SectionId;
            }
            List<Job> jobs = db.Jobs.Join(db.JobsRoles.Where(jr => jr.RoleName == JobRole.ClubManager), j => j.RoleId, jr => jr.RoleId, (j, jr) => j)
                .Where(j => j.SectionId == sectionId).ToList();

            return jobs;
        }

        public IEnumerable<UserJobDto> GetClubOfficials(int clubId)
        {
            List<UserJobDto> officials = (from userJob in db.UsersJobs
                                          join user in db.Users on userJob.UserId equals user.UserId
                                          join job in db.Jobs on userJob.JobId equals job.JobId
                                          where userJob.ClubId.HasValue && userJob.ClubId == clubId &&
                                                user.IsArchive == false
                                          orderby user.FullName
                                          select new UserJobDto()
                                          {
                                              Id = userJob.Id,
                                              JobName = job.JobName,
                                              UserId = user.UserId,
                                              FullName = user.FullName
                                          }).ToList();

            return officials;
        }
    }
}
