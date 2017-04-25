using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AppModel;
using DataService.DTO;

namespace CmsApp.Models
{
    public class TeamForm
    {
        public int LeagueId { get; set; }
        public int SeasonId { get; set; }
        public int? UnionId { get; set; }
        public int SectionId { get; set; }

        [Required]
        public string Title { get; set; }

        public IEnumerable<Team> TeamsList { get; set; }
        public int TeamId { get; set; }
        public bool IsNew { get; set; }
        public string Address { get; set; }
        public TeamForm()
        {
            TeamsList = new List<Team>();
        }
    }

    public class TeamInfoForm
    {
        public int TeamId { get; set; }
        public IList<LeagueShort> leagues { get; set; }
        public IList<ClubShort> clubs { get; set; } 
        public int? LeagueId { get { return leagues.Count == 1 ? (int?)leagues[0].Id : null; } }
        [Required]
        public string Title { get; set; }
        public string Logo { get; set; }
        public string PersonnelPic { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public int SeasonId { get; set; }
        public bool? IsReserved { get; set; }
        public bool? IsUnderAdult { get; set; }
    }
}