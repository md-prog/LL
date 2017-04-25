using AppModel;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CmsApp.Models
{
    public class ClubsForm
    {
        public int? ClubId { get; set; }
        public int? SectionId { get; set; }
        public int? UnionId { get; set; }
        public int? SeasonId { get; set; }
        [Required]
        public string Name { get; set; }
        public IEnumerable<Club> Clubs { get; set; }
    }
}