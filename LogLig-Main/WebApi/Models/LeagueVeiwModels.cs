using AppModel;
using DataService.LeagueRank;
using System;
using System.Collections.Generic;

namespace WebApi.Models
{

    public class LeagueInfoVeiwModel
    {

        public LeagueInfoVeiwModel() {}
        public LeagueInfoVeiwModel(League league)
        {
            Id = league.LeagueId;
            Logo = league.Logo;
            Image = league.Image;
            Title = league.Name;
        }

        public int Id { get; set; }

        public string Logo { get; set; }

        public string Image { get; set; }

        public string Title { get; set; }

    }

    public class LeaguePageVeiwModel
    {

        public LeagueInfoVeiwModel LeagueInfo { get; set; }

        public TeamCompactViewModel TeamWithMostFans { get; set; }

        public NextGameViewModel NextGame { get; set; }

        public IEnumerable<GameViewModel> NextGames { get; set; }

        public IEnumerable<GameViewModel> LastGames { get; set; }

        public List<RankStage> LeagueTableStages { get; set; }


        public List<int> GameCycles { get; set; }
    }

    //public class LeagueTableVeiwModel
    //{

    //    public LeagueInfoVeiwModel LeagueInfo { get; set; }


    //    public List<DataService.LeagueRank.RankStage> Stages { get; set; }
    //}

    public class LeaguesListItemViewModel
    {

        public int Id { get; set; }

        public string Title { get; set; }

        public int TotalTeams { get; set; }

        public int TotalFans { get; set; }

        public string Logo { get; set; }
    }

}