using DataService.DTO;
using System.Collections.Generic;

namespace CmsApp.Models
{
    public class TeamNavView
    {
        public int TeamId { get; set; }
        public int SeasonId { get; set; }
        public string TeamName { get; set; }
        public string Address { get; set; }
        public IList<ClubShort> clubs { get; set; }
        public IList<LeagueShort> leagues { get; set; }
        public IList<LeagueShort> UserLeagues { get; set; }
        public bool IsValidUser { get; set; }

        public int? CurrentLeagueId { get { return leagues.Count == 1 ? (int?)leagues[0].Id : null; } }
        public int? ClubId { get { return clubs.Count == 1 ? (int?)clubs[0].Id : null; } }
        public int SectionId { get; set; }
        public int? UnionId { get; set; }
        public string JobRole { get; set; }
        public TeamInfoForm Details { get; set; }
    }
}