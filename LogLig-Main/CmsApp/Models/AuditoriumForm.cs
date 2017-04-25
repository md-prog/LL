using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using AppModel;

namespace CmsApp.Models
{
    public class AuditoriumForm
    {
        public int AuditoriumId { get; set; }

        public int? UnionId { get; set; }
        public int? SeasonId { get; set; }
        public int? ClubId { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; }

        [Required, StringLength(250)]
        public string Address { get; set; }

        public IEnumerable<Auditorium> Auditoriums { get; set; }
    }
}