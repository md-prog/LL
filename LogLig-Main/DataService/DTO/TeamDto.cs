using System.Runtime.Serialization;

namespace DataService.DTO
{
    public class TeamDto
    {
        public int TeamId { get; set; }
        public string Title { get; set; }
        public int LeagueId { get; set; }
        public string Logo { get; set; }
        public string Address { get; set; }
        public int ClubId { get; set; }

        [IgnoreDataMember]
        public int? SeasonId { get; set; }
    }
}
