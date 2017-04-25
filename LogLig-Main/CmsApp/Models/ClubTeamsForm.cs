using System.Collections.Generic;
using AppModel;

namespace CmsApp.Models
{
    public class ClubTeamsForm
    {
        public int ClubId { get; set; }
        public int TeamId { get; set; }
        public int SeasonId { get; set; }
        public int CurrentSeasonId { get; set; }
        public string TeamName { get; set; }
        public bool IsNew { get; set; }
        public int SectionId { get; set; }
        public IEnumerable<Team> Teams { get; set; }
    }
}