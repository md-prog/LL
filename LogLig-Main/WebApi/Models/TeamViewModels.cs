using AppModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class LeagueTeamsViewModel
    {
        public int LeagueId { get; set; }

        public string Name { get; set; }

        public IEnumerable<TeamCompactViewModel> Teams { get; set; }
    }


    public class TeamRegisterBindingModel
    {

        public int TeamId { get; set; }

        public string Title { get; set; }

        public int LeagueId { get; set; }

    }

    public class FanTeamsViewModel
    {

        public int TeamId { get; set; }

        public string Title { get; set; }

        public int LeagueId { get; set; }

    }

    public class TeamCompactViewModel
    {
        public TeamCompactViewModel(Team team, int leagueId, int? seasonId = null)
        {
            TeamId = team.TeamId;
            LeagueId = leagueId;
            Logo = team.Logo;

            if (seasonId.HasValue)
            {
                TeamsDetails teamsDetails = team.TeamsDetails.FirstOrDefault(x => x.SeasonId == seasonId);
                Title = teamsDetails != null ? teamsDetails.TeamName : team.Title;
            }
            else
            {
                Title = team.Title;
            }
        }

        public TeamCompactViewModel()
        {
            // TODO: Complete member initialization
        }

        public int TeamId { get; set; }

        public string Title { get; set; }

        public int LeagueId { get; set; }

        public string Logo { get; set; }

        public int FanNumber { get; set; }
    }


    public class TeamPageViewModel
    {

        public TeamInfoViewModel TeamInfo { get; set; }

        public NextGameViewModel NextGame { get; set; }

        public GameViewModel LastGame { get; set; }

        public IEnumerable<LeagueInfoVeiwModel> Leagues { get; set; }

        public List<UserBaseViewModel> Fans { get; set; }

        public List<int> GameCycles { get; set; }

        public List<CompactPlayerViewModel> Players { get; set; }

        public IEnumerable<GameViewModel> NextGames { get; set; }

        public IEnumerable<GameViewModel> LastGames { get; set; }

        public List<TeamJobsViewModel> Jobs { get; set; }
        public List<WallThreadViewModel> MessageThreads { get; internal set; }
    }


    public class TeamInfoViewModel
    {

        public int TeamId { get; set; }

        public int Place { get; set; }

        public string Ratio { get; set; }

        public int SuccsessLevel { get; set; }

        public string Logo { get; set; }

        public string Image { get; set; }

        public string Title { get; set; }


        public string League { get; set; }

        public int LeagueId { get; set; }
    }

    public class TeamJobsViewModel
    {

        public int Id { get; set; }

        public string JobName { get; set; }

        public int UserId { get; set; }

        public string FullName { get; set; }

        //[JsonIgnore]
        //public DateTime? BirthDay { get; set; }

        //public int? Age
        //{
        //    get
        //    {
        //        if (this.BirthDay.HasValue)
        //        {
        //            DateTime zeroTime = new DateTime(1, 1, 1);
        //            DateTime a = this.BirthDay.Value;
        //            DateTime b = DateTime.Now;
        //            TimeSpan span = b - a;
        //            int years = (zeroTime + span).Year - 1;
        //            return years;
        //        }
        //        else
        //        {
        //            return null;
        //        }
        //    }
        //}

        public bool IsFriend { get; set; }

        public string FriendshipStatus { get; set; }
    }

    //public class TeamProfileVeiwModel
    //{

    //    public TeamProfileVeiwModel()
    //    {
    //        Players = new List<CompactPlayerViewModel>();

    //        Fans = new List<FanCompactViewModel>();
    //    }

    //    public int TeamId { get; set; }

    //    public string Title { get; set; }

    //    public string Logo { get; set; }

    //    public string PersonnelPic { get; set; }

    //    public string Description { get; set; }

    //    public string OrgUrl { get; set; }

    //    public DateTime CreateDate { get; set; }

    //    public virtual List<CompactPlayerViewModel> Players { get; set; }

    //    public virtual List<FanCompactViewModel> Fans { get; set; }
    //}

}