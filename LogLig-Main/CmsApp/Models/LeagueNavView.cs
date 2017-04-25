namespace CmsApp.Models
{
    public class LeagueNavView
    {
        public int LeagueId { get; set; }
        public string LeagueName { get; set; }
        public int? UnionId { get; set; }
        public int? ClubId { get; set; }
        public int? SectionId { get; set; }
        public string SectionAlias { get; set; }
        public int SeasonId { get; set; }
        public string UnionName { get; set; }
        public string ClubName { get; set; }
        public bool IsUnionValid { get; set; }
    }
}