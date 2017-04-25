﻿using AppModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataService
{
    public class AuthorizationEntitiesService : BaseRepo
    {


        public bool AuthorizeUnionByIdAndManagerId(int unionId, int managerId)
        {
            return FindUnionByIdAndManagerId(unionId, managerId).Count > 0;
        }

        public List<Union> FindUnionByIdAndManagerId(int unionId, int managerId)
        {
            var unionsQuery = (from u in db.Unions
                               from uj in u.UsersJobs
                               let j = uj.Job.JobsRole
                               where u.IsArchive == false
                                   && u.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.UnionManager
                                   && u.UnionId == unionId
                               select u);

            return unionsQuery.ToList();
        }

        public bool AuthorizeLeagueByIdAndManagerId(int leageId, int managerId)
        {
            return FindLeagueByIdAndManagerId(leageId, managerId).Count > 0;
        }

        public IEnumerable<Team> FindTeamsByManagerId(int managerId)
        {
            var unionsQuery = (from u in db.Unions
                               from le in u.Leagues
                               from t in le.LeagueTeams
                               from uj in u.UsersJobs
                               let j = uj.Job.JobsRole
                               where u.IsArchive == false
                                   && le.IsArchive == false
                                   && t.Teams.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.UnionManager
                               select t.Teams);

            var leagueQuery = (from le in db.Leagues
                               from t in le.LeagueTeams
                               from uj in le.UsersJobs
                               let j = uj.Job.JobsRole
                               where le.IsArchive == false
                                   && t.Teams.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.LeagueManager
                               select t.Teams);

            var teamsQuery = (from t in db.Teams
                              from uj in t.UsersJobs
                              let j = uj.Job.JobsRole
                              where t.IsArchive == false
                                  && uj.UserId == managerId
                                  && j.RoleName == JobRole.TeamManager
                              select t);

            return unionsQuery.Union(leagueQuery).Union(teamsQuery).ToList();
        }

        public IList<League> FindLeaguesByTeamAndManagerId(int teamId, int managerId)
        {
            var unionsQuery = (from u in db.Unions
                               from le in u.Leagues
                               from uj in u.UsersJobs
                               from t in le.LeagueTeams
                               let j = uj.Job.JobsRole
                               where u.IsArchive == false
                                   && le.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.UnionManager
                                   && t.TeamId == teamId
                               select le);

            var leagueQuery = (from le in db.Leagues
                               from t in le.LeagueTeams
                               from uj in le.UsersJobs
                               let j = uj.Job.JobsRole
                               where le.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.LeagueManager
                                   && t.TeamId == teamId
                               select le);

            return unionsQuery.Union(leagueQuery).ToList();
        }

        public List<League> FindLeagueByIdAndManagerId(int leageId, int managerId)
        {
            var unionsQuery = (from u in db.Unions
                               from le in u.Leagues
                               from uj in u.UsersJobs
                               let j = uj.Job.JobsRole
                               where u.IsArchive == false
                                   && le.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.UnionManager
                                   && le.LeagueId == leageId
                               select le);

            var leagueQuery = (from le in db.Leagues
                               from uj in le.UsersJobs
                               let j = uj.Job.JobsRole
                               where le.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.LeagueManager
                                   && le.LeagueId == leageId
                               select le);

            return unionsQuery.Union(leagueQuery).ToList();
        }

        public bool AuthorizeTeamByIdAndManagerId(int teamId, int managerId)
        {
            return FindTeamByIdAndManagerId(teamId, managerId).Count > 0;
        }

        public List<Team> FindTeamByIdAndManagerId(int teamId, int managerId)
        {
            var unionsQuery = (from u in db.Unions
                               from le in u.Leagues
                               from t in le.LeagueTeams
                               from uj in u.UsersJobs
                               let j = uj.Job.JobsRole
                               where u.IsArchive == false
                                   && le.IsArchive == false
                                   && t.Teams.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.UnionManager
                                   && t.TeamId == teamId
                               select t.Teams);

            var leagueQuery = (from le in db.Leagues
                               from t in le.LeagueTeams
                               from uj in le.UsersJobs
                               let j = uj.Job.JobsRole
                               where le.IsArchive == false
                                   && t.Teams.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.LeagueManager
                                   && t.TeamId == teamId
                               select t.Teams);

            var teamsQuery = (from t in db.Teams
                              from uj in t.UsersJobs
                              let j = uj.Job.JobsRole
                              where t.IsArchive == false
                                  && uj.UserId == managerId
                                  && j.RoleName == JobRole.TeamManager
                                  && t.TeamId == teamId
                              select t);

            return unionsQuery.Union(leagueQuery).Union(teamsQuery).ToList();
        }

        public bool AuthorizePlayerByUserIdAndManagerId(int userId, int managerId)
        {
            return FindPlayerByUserIdAndManagerId(userId, managerId).Count > 0;
        }

        public List<User> FindPlayerByUserIdAndManagerId(int userId, int managerId)
        {
            var unionsQuery = (from u in db.Unions
                               from le in u.Leagues
                               from t in le.LeagueTeams
                               from p in t.Teams.TeamsPlayers
                               from uj in u.UsersJobs
                               let j = uj.Job.JobsRole
                               let usr = p.User
                               let ut = usr.UsersType
                               where u.IsArchive == false
                                   && le.IsArchive == false
                                   && t.Teams.IsArchive == false
                                   && usr.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.UnionManager
                                   && ut.TypeRole == AppRole.Players
                                   && usr.UserId == userId
                               select usr);

            var leagueQuery = (from le in db.Leagues
                               from t in le.LeagueTeams
                               from p in t.Teams.TeamsPlayers
                               from uj in le.UsersJobs
                               let j = uj.Job.JobsRole
                               let usr = p.User
                               let ut = usr.UsersType
                               where le.IsArchive == false
                                   && t.Teams.IsArchive == false
                                   && usr.IsArchive == false
                                   && uj.UserId == managerId
                                   && j.RoleName == JobRole.LeagueManager
                                   && ut.TypeRole == AppRole.Players
                                   && usr.UserId == userId
                               select usr);

            var teamsQuery = (from t in db.Teams
                              from p in t.TeamsPlayers
                              from uj in t.UsersJobs
                              let j = uj.Job.JobsRole
                              let usr = p.User
                              let ut = usr.UsersType
                              where t.IsArchive == false
                                  && usr.IsArchive == false
                                  && uj.UserId == managerId
                                  && j.RoleName == JobRole.TeamManager
                                  && ut.TypeRole == AppRole.Players
                                  && usr.UserId == userId
                              select usr);

            return unionsQuery.Union(leagueQuery).Union(teamsQuery).ToList();
        }

    }
}
