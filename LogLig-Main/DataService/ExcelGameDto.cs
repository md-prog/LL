using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DataService
{
    public class ExcelGameDto
    {
        public int GameId { get; set; }
        public string League { get; set; }
        public int LeagueId { get; set; }
        public int Stage { get; set; }
        public DateTime Date { get; set; }
        public string Time { get; set; }
        public string HomeTeam { get; set; }
        public int HomeTeamId { get; set; }
        public int HomeTeamScore { get; set; }
        public string GuestTeam { get; set; }
        public int GuestTeamScore { get; set; }
        public int GuestTeamId { get; set; }
        public string Auditorium { get; set; }
        public int AuditoriumId { get; set; }
        public string Referee { get; set; }
        public int RefereeId { get; set; }
        public int CycleNumber { get; set; }
        public string Groupe { get; set; }
        public string Set1 { get; set; }
        public string Set2 { get; set; }
        public string Set3 { get; set; }
        public string Set4 { get; set; }
    }

    public class ExcelRefereeDto
    {
        public string League { get; set; }
        public DateTime StartDate { get; set; }
        public string HomeTeam { get; set; }
        public string GuestTeam { get; set; }
        public string Auditorium { get; set; }
        public string Referee { get; set; }
    }
}
