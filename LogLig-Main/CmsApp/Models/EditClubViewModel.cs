using System;
using System.Collections.Generic;
using AppModel;

namespace CmsApp.Models
{
    public class EditClubViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int? SectionId { get; set; }
        public string SectionName { get; set; }
        public int? UnionId { get; set; }
        public string UnionName { get; set; }
        public int? SeasonId { get; set; }
        public int CurrentSeasonId { get; set; }
        public string CurrentSeasonName { get; set; }
        public IEnumerable<Season> Seasons { get; set; }
    }
}