using AppModel;
using DataService.LeagueRank;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using WebApi.Models;

namespace WebApi.Services
{
    public class TeamsService : IDisposable
    {
        #region Constructor & fields
        private readonly DataEntities _db;


        public TeamsService(DataEntities db)
        {
            _db = db;
        }
        public TeamsService()
        {
            _db = new DataEntities();
        }
        #endregion

        internal static TeamInfoViewModel GetTeamInfo(Team team, int leagueId, int? seasonId = null)
        {
            if (!team.LeagueTeams.Any(l => l.LeagueId == leagueId))
            {
                return null;
            }

            var vm = new TeamInfoViewModel();
            //teamId
            vm.TeamId = team.TeamId;

            //"place": 7,
            //"ratio": "2:3",
            //"succsessLevel": 59,
            LeagueRankService leagueRankService = new LeagueRankService(leagueId);
            RankLeague rLeague = leagueRankService.CreateLeagueRankTable(seasonId);
            if (rLeague != null)
            {
                var stage = rLeague.Stages.OrderByDescending(t => t.Number).FirstOrDefault();
                RankGroup group;
                if (stage != null)
                {
                    group = stage.Groups.Where(gr => gr.Teams.Any(t => t.Id == team.TeamId)).FirstOrDefault();
                    RankTeam rTeam = null;
                    if (group != null)
                    {
                        rTeam = group.Teams.Where(t => t.Id == team.TeamId).FirstOrDefault();
                    }
                    if (rTeam != null)
                    {
                        vm.Place = int.Parse(rTeam.Position);
                        vm.Ratio = rTeam.SetsLost.ToString() + ":" + rTeam.SetsWon.ToString();
                        if (rTeam.Games == 0)
                        {
                            vm.SuccsessLevel = 0;
                        }
                        else
                        {
                            double wins = rTeam.Wins;
                            double games = rTeam.Games;
                            double ratio = (wins / games) * 100;
                            vm.SuccsessLevel = Convert.ToInt32(ratio);
                        }
                    }
                }
            }
            vm.Logo = team.Logo;
            vm.Image = team.PersonnelPic;
            vm.League = team.LeagueTeams.Where(tl => tl.LeagueId == leagueId).FirstOrDefault().Leagues.Name;
            vm.LeagueId = leagueId;

            if (seasonId.HasValue)
            {
                TeamsDetails teamsDetails = team.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId);
                vm.Title = teamsDetails != null ? teamsDetails.TeamName : team.Title;
            }
            else
            {
                vm.Title = team.Title;
            }

            return vm;
        }

        internal static List<TeamJobsViewModel> GetTeamJobsByTeamId(int teamId, int currentUserId)
        {
            using (DataEntities db = new DataEntities())
            {
                var jobs = (from u in db.Users
                            from j in u.UsersJobs
                            where j.TeamId == teamId && u.IsArchive == false && u.IsActive == true
                            select new TeamJobsViewModel
                            {
                                Id = j.Id,
                                JobName = j.Job.JobName,
                                UserId = u.UserId,
                                FullName = u.FullName.Trim(),
                                //BirthDay = u.BirthDay
                            }).ToList();

                if (currentUserId != 0)
                {
                    foreach (var job in jobs)
                    {
                        job.FriendshipStatus = FriendsService.AreFriends(job.UserId, currentUserId);
                    }
                }
                return jobs;
            }
        }

        public static List<RankTeam> GetRankedTeams(int leagueId, int teamId, int? seasonId)
        {
            var resList = new List<RankTeam>();

            var leagueRankService = new LeagueRankService(leagueId);
            RankLeague rLeague = leagueRankService.CreateLeagueRankTable(seasonId);

            if (rLeague != null)
            {
                var stage = rLeague.Stages.OrderByDescending(t => t.Number).FirstOrDefault();
                if (stage == null)
                {
                    return null;
                }

                var group = stage.Groups.FirstOrDefault(gr => gr.Teams.Any(t => t.Id == teamId));
                if (group == null)
                {
                    return null;
                }

                var teams = group.Teams.OrderBy(t => t.Position).ToList();
                for (int i = 0; i < teams.Count; i++)
                {
                    if (teams[i].Id == teamId)
                    {
                        if (i > 0)
                            resList.Add(teams[i - 1]);

                        resList.Add(teams[i]);

                        if (i < teams.Count - 1)
                            resList.Add(teams[i + 1]);
                    }
                }
            }

            return resList;
        }

        internal static List<UserBaseViewModel> GetTeamFans(int teamId, int leagueId, int currentUserId)
        {
            using (DataEntities db = new DataEntities())
            {
                return (from tf in db.TeamsFans
                        let u = tf.User
                        from us in u.Friends.Where(t => t.FriendId == currentUserId && t.UserId == tf.UserId).DefaultIfEmpty()
                        from usf in u.UsersFriends.Where(t => t.UserId == currentUserId && t.FriendId == tf.UserId).DefaultIfEmpty()
                        where tf.TeamId == teamId && tf.LeageId == leagueId
                        select new UserBaseViewModel
                        {
                            Id = tf.UserId,
                            UserName = u.UserName,
                            FullName = u.FullName,
                            UserRole = u.UsersType.TypeRole,
                            Image = u.Image,
                            CanRcvMsg = true,
                            FriendshipStatus = (us == null && usf == null) ? FriendshipStatus.No :
                             ((us != null && us.IsConfirmed) || (usf != null && usf.IsConfirmed)) ? FriendshipStatus.Yes :
                             FriendshipStatus.Pending
                        }).ToList();
            }
        }

        //internal static List<TeamFanViewModel> GetTeamFans(int leagueId, int teamId)
        //{
        //    using (DataEntities db = new DataEntities())
        //    {
        //        var teamFans = (from teamFan in db.TeamsFans
        //                        join user in db.Users on teamFan.UserId equals user.UserId
        //                        where teamFan.TeamId == teamId && user.IsActive// && teamFan.LeageId == leagueId
        //                        select new TeamFanViewModel
        //                        {
        //                            Id = teamFan.UserId,
        //                            UserName = user.UserName,
        //                            FullName = user.FullName,
        //                            Image = user.Image
        //                        }).ToList();

        //        teamFans = teamFans.GroupBy(x => x.Id).Select(g => g.First()).ToList();

        //        return teamFans;
        //    }
        //}

        //internal static List<UserBaseViewModel> GetTeamFans(Team team, int leagueId, int currentUserId)
        //{

        //    var fans = team.TeamsFans
        //        .Where(tf => tf.LeageId == leagueId && tf.User.IsActive == true && tf.User.IsArchive == false)
        //        .Select(tf => new UserBaseViewModel
        //            {
        //                Id = tf.UserId,
        //                UserName = tf.User.UserName,
        //                Image = tf.User.Image,
        //            }).ToList();
        //    FriendsService.AreFansFriends(fans, currentUserId);
        //    return fans;
        //}

        internal static List<FanTeamsViewModel> GetFanTeams(User user)
        {

            return user.TeamsFans
                .Where(tf => tf.Team.IsArchive == false)
                .Select(tf =>
                new FanTeamsViewModel
                {
                    TeamId = tf.TeamId,
                    Title = tf.Team.Title,
                    LeagueId = tf.LeageId
                }).ToList();
        }

        internal static List<TeamInfoViewModel> GetFanTeamsWithStatistics(User user, int? seasonId)
        {
            Func<TeamsFan, bool> predicate;
            if (seasonId.HasValue)
            {
                predicate = tf => tf.Team.IsArchive == false &&
                                  tf.Team.LeagueTeams.Where(x => x.SeasonId == seasonId)
                                                     .Any(l => l.LeagueId == tf.LeageId);
            }
            else
            {
                predicate = tf => tf.Team.IsArchive == false &&
                                  tf.Team.LeagueTeams.Any(l => l.LeagueId == tf.LeageId);
            }

            return user.TeamsFans
                       .Where(predicate)
                       .Select(tf => GetTeamInfo(tf.Team, tf.LeageId, seasonId)).ToList();
        }

        public Team GetTeamById(int teamId)
        {
            return _db.Teams.Include(x=>x.LeagueTeams)
                    .Include(x=>x.HomeTeamGamesCycles)
                    .Include(x=>x.GuestTeamGamesCycles)
                    .Include(x=>x.HomeTeamGamesCycles.Select(f=>f.Stage))
                    .Include(x=>x.GuestTeamGamesCycles.Select(f=>f.Stage))
                    .Include(x=>x.HomeTeamGamesCycles.Select(f=>f.GameSets))
                    .Include(x=>x.GuestTeamGamesCycles.Select(f=>f.GameSets))
                    .FirstOrDefault(x=>x.TeamId == teamId);
        }

        public void Dispose()
        {
            _db.Dispose();
        }
    }
}