using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using Resources;

namespace CmsApp.Models
{
    public class GameForm
    {
        public int GameId { get; set; }

        public int LeagueId { get; set; }

        public int StageId { get; set; }

        [Required]
        public int Rounds { get; set; }

        public int GroupsNum { get; set; }

        [Required]
        public int PlayoffTeamsNum { get; set; }

        [Required]
        [Range(0, Int32.MaxValue)]
        public int WeekRounds { get; set; }

        [Required]
        [Range(0, Int32.MaxValue)]
        public int NumberOfSequenceRounds { get; set; }

        public string GameDays { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required, RegularExpression(@"^(\d{2}:\d{2})$")]
        public string GamesInterval { get; set; }

        public int PointsWin { get; set; }

        public int PointsDraw { get; set; }

        public int PointsLoss { get; set; }

        public int PointsTechWin { get; set; }

        public int PointsTechLoss { get; set; }

        public string[] DaysList { get; set; }


        //public IEnumerable<SelectListItem> SortTypes { get; set; }

        //public string[] NewSortTypes { get; set; }

    }
}