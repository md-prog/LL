using System.Collections.Generic;

namespace WebApi.Models
{
    public class ClubTeamInfoViewModel
    {
        public int Id { get; set; }
        public string Logo { get; set; }
        public string Title { get; set; }
        public int TotalTeams { get; set; }
        public int TotalFans { get; set; }

        public List<ClubTeamViewModel> Teams { get; set; }
    }
}