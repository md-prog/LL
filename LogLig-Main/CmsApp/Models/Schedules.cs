using AppModel;
using DataService.DTO;
using System;
using System.Collections.Generic;

namespace CmsApp.Models
{
    public class Schedules
    {
        public class DateFilterPeriod
        {
            public const int BeginningOfMonth = 0;
            public const int Ranged = 1;
            public const int All = 2;
        }

        public int? UnionId { get; set; }
        public List<GamesInLeague> Games { get; set; }
        public AuditoriumShort[] Auditoriums { get; set; }
        public LeagueShort[] Leagues { get; set; }
        public int dateFilterType { get; set; }
        public DateTime? dateFrom { get; set; }
        public DateTime? dateTo { get; set; }
        public User[] Referees { get; set; }
        public IDictionary<int, IList<TeamShortDTO>> teamsByGroups { get; set; }
        public int Sort { get; set; }
        public int SeasonId { get; set; }
        public bool? IsPublished { get; set; }
        public static DateTime FirstDayOfMonth
        {
            get { return new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1); }
        }
        public static DateTime Tomorrow
        {
            get
            {
                return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1);
            }
        }
    }

    public class GamesInLeague : GamesCycle
    {
        public GamesInLeague(GamesCycle model)
        {
            CycleId = model.CycleId;
            StageId = model.StageId;
            CycleNum = model.CycleNum;
            StartDate = model.StartDate;
            AuditoriumId = model.AuditoriumId;
            GuestTeamId = model.GuestTeamId;
            GuestTeamScore = model.GuestTeamScore;
            HomeTeamId = model.HomeTeamId;
            HomeTeamScore = model.HomeTeamScore;
            RefereeId = model.RefereeId;
            GroupeId = model.GroupeId;
            GameStatus = model.GameStatus;
            TechnicalWinnnerId = model.TechnicalWinnnerId;
            MaxPlayoffPos = model.MaxPlayoffPos;
            MinPlayoffPos = model.MinPlayoffPos;
            BracketId = model.BracketId;
            Auditorium = model.Auditorium;
            Group = model.Group;
            GuestTeam = model.GuestTeam;
            HomeTeam = model.HomeTeam;
            Stage = model.Stage;
            User = model.User;
            GameSets = model.GameSets;
            PlayoffBracket = model.PlayoffBracket;
            NotesGames = model.NotesGames;
            WallThreads = model.WallThreads;
            Users = model.Users;
            IsPublished = model.IsPublished;
        }

        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
    }
}