using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using AppModel;

namespace DataService.LeagueRank
{

    public class RankLeague
    {
        public List<RankStage> Stages { get; set; }

        public int LeagueId { get; set; }
        public string AboutLeague { get; set; }
        public int SeasonId { get; set; }
        public string LeagueStructure { get; set; }

        public RankLeague()
        {
            Stages = new List<RankStage>();
            Teams = new List<Team>();
        }

        public List<Team> Teams { get; set; }

        public string Name { get; set; }

        public string Logo { get; set; }
        public bool IsEmptyRankTable { get; set; }
       
    }

    public class RankStage
    {
        public List<RankGroup> Groups { get; set; }

        public RankStage()
        {
            Groups = new List<RankGroup>();
        }
        public string Name { get; set; }
        public bool Playoff { get; set; }
        public int Number { get; set; }
    }

    public class RankGroup
    {
        public string GameType { get; set; }
        public List<RankTeam> Teams { get; set; }

        public int? PointsEditType { get; set; }

        public bool IsAdvanced { get; set; }

        public int? PlayoffBrackets { get; set; }

        public RankGroup()
        {
            Teams = new List<RankTeam>();
        }

        public string Title { get; set; }
        public List<ExtendedTable> ExtendedTables { get; set; } = new List<ExtendedTable>();
    }


    public class RankTeam
    {

        public int? Id { get; set; } = 0;

        public int Points { get; set; }
        public int Draw { get; set; } = 0;

        public string Title { get; set; }

        public string Address { get; set; } = "";

        public string Position { get; set; } = "";

        public int PositionNumber { get; set; }

        public int Games { get; set; }

        public int Wins { get; set; }

        public int Loses { get; set; }

        public int TechLosses { get; set; } = 0;

        public int SetsWon { get; set; }

        public int SetsLost { get; set; }

        public int HomeTeamFinalScore { get; set; }

        public int GuesTeamFinalScore { get; set; }

        public short TeamPosition { get; set; }

        public int PointsDifference
        {
            get { return HomeTeamFinalScore - GuesTeamFinalScore; }
        }

        public string SetsRatio
        {
            get
            {
                double scored = this.SetsWon;
                double lost = this.SetsLost;
                if (scored == 0 && lost == 0)
                {
                    return string.Format("{0:N2}", 0);
                }
                else if (lost == 0)
                {
                    return "MAX";
                }
                else
                {
                    double ratio = scored / lost;
                    return string.Format("{0:N2}", ratio);
                }
            }
        }

        public double SetsRatioNumiric
        {
            get
            {
                double scored = this.SetsWon;
                double lost = this.SetsLost;
                if (scored == 0 && lost == 0)
                {
                    return 0;
                }
                else if (lost == 0)
                {
                    return double.MaxValue;
                }
                else
                {
                    double ratio = scored / lost;
                    return ratio;
                }
            }
        }


        public int TotalPointsScored { get; set; }

        public int TotalHomeTeamPoints { get; set; }

        public int TotalGuesTeamPoints { get; set; }

        public int TotalPointsLost { get; set; }

        public int TotalPointsDiffs
        {
            get
            {
                return this.TotalPointsScored - this.TotalPointsLost;
            }
        }

        public int HomeTeamScore { get; set; }
        public int GuestTeamScore { get; set; }

        public string Logo { get; set; } = "";

    }

    public class ExtendedTable
    {
        public int TeamId { get; set; }
        public string TeamName { get; set; }
        public char Letter { get; set; }

        public List<ExtendedTableScore> Scores { get; set; } = new List<ExtendedTableScore>();
    }

    public class ExtendedTableScore
    {
        public int OpponentTeamId { get; set; }
        public int OpponentScore { get; set; }
        public int TeamScore { get; set; }
    }
}
