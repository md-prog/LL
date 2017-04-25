using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using AppModel;
using WebApi.Services;
using System.ComponentModel.DataAnnotations;

namespace WebApi.Models
{
    public class GameViewModel
    {

        public int GameId { get; set; }

        [JsonIgnore]
        public string GameCycleStatus { get; set; }

        public string HouseName { get; set; }

        public string GroupName { get; set; }

        public string Status
        {
            get
            {
                switch (this.GameCycleStatus)
                {
                    case GameStatus.Started:
                        return "live";
                    case GameStatus.Ended:
                        return "ended";
                    case GameStatus.Next:
                        return "waiting";
                    default:
                        int totalHours = (int)this.StartDate.Subtract(DateTime.Now).TotalHours;
                        if (totalHours <= 48)
                        {
                            return "closetodate";
                        }
                        else
                        {
                            return "next";
                        }
                }
            }
        }

        public DateTime StartDate { get; set; }

        public int? HomeTeamId { get; set; }

        public string HomeTeam { get; set; }

        public int HomeTeamScore { get; set; }

        public string HomeTeamLogo { get; set; }

        public int? GuestTeamId { get; set; }

        public string GuestTeam { get; set; }

        public int GuestTeamScore { get; set; }

        public string GuestTeamLogo { get; set; }

        public string Auditorium { get; set; }

        public int CycleNumber { get; set; }

        public string PlayOffType { get; set; }

        public int LeagueId { get; set; }

        public string LeagueName { get; set; }

        public Nullable<int> MaxPlayoffPos { get; set; }

        public Nullable<int> MinPlayoffPos { get; set; }
    }

    public class NextGameViewModel : GameViewModel
    {

        public int TimeLeft
        {
            get
            {
                return (int)StartDate.Subtract(DateTime.Now).TotalSeconds;
            }

        }

        public int IsGoing { get; set; }

        public int FriendsGoing { get; set; }

        public int FansGoing { get; set; }

        public IList<UserBaseViewModel> FansList { get; set; }
    }

    public class GoingDTO
    {
        public int Id { get; set; }
        public int IsGoing { get; set; }
    }

    public class GamePageViewModel
    {

        public GameViewModel GameInfo { get; set; }

        public IEnumerable<UserBaseViewModel> GoingFriends { get; set; }

        public IEnumerable<GameSetViewModel> Sets { get; set; }

        public IEnumerable<GameViewModel> History { get; set; }
    }

    public class CreateGameSetViewModel
    {
        public int GameSetId { get; set; }
        [Required]
        public int GameCycleId { get; set; }
        [Required]
        public int HomeTeamScore { get; set; }
        [Required]
        public int GuestTeamScore { get; set; }
        [Required]
        public bool IsGoldenSet { get; set; }
    }

    public class GameSetViewModel : CreateGameSetViewModel
    {
        public int SetNumber { get; set; }
    }
}