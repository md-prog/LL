using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using Omu.ValueInjecter;
using Resources;

namespace CmsApp.Models
{
    public class LeagueDetailsForm
    {
        public int LeagueId { get; set; }
        public string Name { get; set; }
        [Range(0, 20)]
        public int MaximumHandicapScoreValue { get; set; }
        public bool IsHadicapEnabled { get; set; }
        public string Logo { get; set; }
        public int AgeId { get; set; }
        public int GenderId { get; set; }
        public string Image { get; set; }
        public short Gender { get; set; }
        public string Description { get; set; }
        public string Terms { get; set; }
        public IEnumerable<SelectListItem> Ages { get; set; }
        public IEnumerable<SelectListItem> Genders { get; set; }
        public int DocId { get; set; }
        public int PlayersCount { get; set; }
        public int OfficialsCount { get; set; }

        [MaxLength(2000, ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = "MaxLengthAboutLeague")]
        public string AboutLeague { get; set; }

        [MaxLength(3000, ErrorMessageResourceType = typeof(Messages), ErrorMessageResourceName = "MaxLengthLeagueStructure")]
        public string LeagueStructure { get; set; }
    }

    public class LeagueCreateForm
    {
        public TournamentsPDF.EditType Et { get; set; }
        public int? UnionId { get; set; }
        public int? ClubId { get; set; }

        public int SeasonId { get; set; }

        [Required]
        public string Name { get; set; }
        public int AgeId { get; set; }
        public int GenderId { get; set; }
        [Range(0, 20)]
        public int MaximumHandicapScoreValue { get; set; }
        public IEnumerable<SelectListItem> Ages { get; set; }
        public IEnumerable<SelectListItem> Genders { get; set; }
    }
}