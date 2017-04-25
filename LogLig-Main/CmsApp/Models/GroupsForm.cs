using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using DataService;
using Resources;

namespace CmsApp.Models
{
    public class PointEdit
    {
        public string Name { get; set; }
        public int? Points { get; set; }
    }

    public class GroupsForm
    {
        public GroupsForm()
        {
            GamesTypes = new List<SelectListItem>();
            TeamsList = new List<SelectListItem>(); 
            SelectedTeamsList = new List<SelectListItem>();
            GroupsTeams = new List<GroupTeam>();
            PossibleNumberOfCycles = new SelectList(new List<int>(new int[] { 1, 2, 3, 4, 5, 6, 7 }));
            //PointsTypes = new SelectList(new List<string>() { "With their records", "Reset scores", "Set the scores manually" });
            PointsTypes = new Dictionary<int, string>() { { 1, Messages.WithTheirRecords }, { 2, Messages.ResetScores }, { 3, Messages.SetTheScoresManualy } };
        }

        public int GroupId { get; set; }
        public int StageId { get; set; }
        public int TypeId { get; set; }
        public int PointId { get; set; }
        public int LeagueId { get; set; }
        public bool FirstStage { get; set; }
        public int? SeasonId { get; set; }
        [Required]
        public string Name { get; set; }

        [Required]
        [Range(1,7, ErrorMessage="נא להכניס מספר בין 1 ל 7")]
        public int NumberOfCycles { get; set; }
        public IEnumerable<SelectListItem> PossibleNumberOfCycles { get; set; }
        public int[] Points { get; set; }
        public int?[] IdTeams { get; set; }
        public string[] Names { get; set; }
        public int[] TeamsArr { get; set; }
        public int[] SelectedTeamsArr { get; set; }
        public IEnumerable<SelectListItem> GamesTypes { get; set; }
        public Dictionary<int, string> PointsTypes { get; set; }
        public IEnumerable<SelectListItem> TeamsList { get; set; }
        public IEnumerable<SelectListItem> SelectedTeamsList { get; set; }
        public IEnumerable<GroupTeam> GroupsTeams { get; set; }
    }
}