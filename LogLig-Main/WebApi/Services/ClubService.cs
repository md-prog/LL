using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using AppModel;
using DataService;
using WebApi.Models;

namespace WebApi.Services
{
    public static class ClubService
    {
        private static readonly JobsRepo JRepo = new JobsRepo();

        internal static ClubInfoViewModel GetClub(int clubId, int? seasonId)
        {
            using (var db = new DataEntities())
            {
                var club = db.Clubs.Include(x => x.ClubTeams)
                                   .Include(x => x.ClubTeams.Select(t => t.Team.TeamsDetails))
                                   .Include(x => x.ClubTeams.Select(f => f.Team.TeamsPlayers)).FirstOrDefault(x => x.ClubId == clubId);
                if (club == null) return new ClubInfoViewModel();

                var result = new ClubInfoViewModel
                {
                    Main = new Main
                    {
                        Players = club.ClubTeams.Sum(x => x.Team.TeamsPlayers.Count),
                        Officials = JRepo.CountOfficialsInClub(clubId)
                    },
                    Info = new Info
                    {
                        ClubName = club.Name,
                        Address = club.Address,
                        Description = club.Description,
                        Email = club.Email,
                        TermsCondition = club.TermsCondition,
                        Phone = club.ContactPhone,
                        AboutClub = club.IndexAbout,
                        Logo = club.Logo,
                        Image = club.PrimaryImage,
                        Index = club.IndexImage
                    },
                    Officials = JRepo.GetClubOfficials(clubId).Select(x => new Officials
                    {
                        JobName = x.JobName,
                        UserName = x.FullName
                    }).ToArray(),
                    Tournaments = db.Leagues.Include(x => x.Age).Include(x => x.Gender).Where(x => x.ClubId == clubId && x.IsArchive == false).Select(x => new Tournaments
                    {
                        Name = x.Name,
                        Ages = x.Age.Title,
                        Gender = x.Gender.Title
                    }).ToArray()
                };

                if (seasonId.HasValue)
                {
                    result.Teams = (from clubTeam in club.ClubTeams
                                    let teamDetails = clubTeam.Team.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)
                                    select new Teams()
                                    {
                                        Id = clubTeam.TeamId,
                                        Team = teamDetails != null ? teamDetails.TeamName : clubTeam.Team.Title
                                    }).ToArray();
                }
                else
                {
                    result.Teams = club.ClubTeams.Select(x => new Teams
                    {
                        Id = x.TeamId,
                        Team = x.Team.Title
                    }).ToArray();
                }

                return result;
            }

        }

        internal static List<ClubTeamInfoViewModel> GetAll(int sectionId, int seasonId)
        {
            using (var db = new DataEntities())
            {
                var clubs = (from club in db.Clubs
                             join clubTeam in db.ClubTeams on club.ClubId equals clubTeam.ClubId
                             join team in db.Teams on clubTeam.TeamId equals team.TeamId
                             where club.SectionId == sectionId && club.IsArchive == false &&
                                    team.IsArchive == false

                             let teamDetails = team.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId)
                             group new {team, teamDetails} by club into groupedClubsByTeam
                             select new ClubTeamInfoViewModel
                             {
                                 Id = groupedClubsByTeam.Key.ClubId,
                                 Title = groupedClubsByTeam.Key.Name,
                                 Logo = groupedClubsByTeam.Key.Logo,
                                 TotalTeams = groupedClubsByTeam.Count(),
                                 Teams = groupedClubsByTeam.Select(t => new ClubTeamViewModel
                                 {
                                     TeamId = t.team.TeamId,
                                     Title = t.teamDetails != null ? t.teamDetails.TeamName : t.team.Title,
                                     Logo = t.team.Logo,
                                     FanNumber = t.team.TeamsFans.Count(x => x.User.IsActive)
                                 }).ToList()
                             }).ToList();

                foreach (var club in clubs)
                {
                    club.TotalFans = club.Teams.Select(x => x.FanNumber).Sum();
                }

                return clubs;
            }
        }
    }
}