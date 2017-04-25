using System;
using System.Collections.Generic;

namespace DataService
{
    public class GamesCycleDto
    {
        public int CycleId { get; set; }
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public string LeagueLogo { get; set; }
        public string SectionAlias { get; set; }
        public DateTime StartDate { get; set; }
        public string GameStatus { get; set; }
        public int AuditoriumId { get; set; }
        public string Auditorium { get; set; }
        public string AuditoriumAddress { get; set; }
        public string HomeTeam { get; set; }
        public string GuestTeam { get; set; }
        public string HomeLogo { get; set; }
        public string GuesLogo { get; set; }
        public int? HomeTeamId { get; set; }
        public int? GuesTeamId { get; set; }
        public int HomeTeamScore { get; set; }
        public int GuestTeamScore { get; set; }
        public BracketDto Bracket { get; set; }
        public EventDto EventRef { get; set; }
        public string StageName { get; internal set; }
        public bool IsAdvanced { get; internal set; }
        public bool IsRoot { get; internal set; }
        public bool IsHomeTeamKnown { get; internal set; }
        public bool IsGuestTeamKnown { get; internal set; }
        public int IndexInBracket { get; internal set; }
        public int? MaxPlayoffPos { get; internal set; }
        public int? MinPlayoffPos { get; internal set; }

        public int BasketBallWaterpoloHomeTeamScore { get; set; }
        public int BasketBallWaterpoloGuestTeamScore { get; set; }

    }

    public class BracketDto
    {
        public int Id { get; internal set; }
        public int Type { get; internal set; }
    }

    public class EventDto
    {
        public EventDto()
        {
            IsUsed = false;
        }
        public int EventId { get; set; }
        public int? LeagueId { get; set; }
        public int? ClubId { get; set; }
        public DateTime EventTime { get; set; }

        public string Title { get; set; }
        public string Place { get; set; }
        public bool IsUsed { get; set; }
    }

    public class SchedulesDto
    {
        public IEnumerable<GamesCycleDto> GameCycles { get; set; }
        public IEnumerable<EventDto> Events { get; set; }
    }
}
