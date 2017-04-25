using System;
using System.Collections.Generic;
using AppModel;

namespace DataService.DTO
{
    public class GameDto
    {
        public int GameId { get; set; }
        public string GameCycleStatus { get; set; }
        public DateTime StartDate { get; set; }
        public int? HomeTeamId { get; set; }
        public string HomeTeamTitle { get; set; }
        public int HomeTeamScore { get; set; }
        public string HomeTeamLogo { get; set; }
        public int? GuestTeamId { get; set; }
        public string GuestTeamTitle { get; set; }
        public int GuestTeamScore { get; set; }
        public string GuestTeamLogo { get; set; }
        public string Auditorium { get; set; }
        public int CycleNumber { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public bool IsPublished { get; set; }
        public IEnumerable<TeamDetailsDto> HomeTeamDetails { get; set; }
        public IEnumerable<TeamDetailsDto> GuestTeamDetails { get; set; }
    }
}
