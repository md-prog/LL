namespace CmsApp.Models
{
    public class ManagedItemViewModel
    {

        public int Id { get; set; }

        public string Name { get; set; }

        public string LeagueName { get; set; }

        public string Controller { get; set; }

        public int? SeasonId { get; set; }

        public int? LeagueId { get; set; }
        public int? UnionId { get; set; }

        public int? ClubId { get; set; }
    }
}