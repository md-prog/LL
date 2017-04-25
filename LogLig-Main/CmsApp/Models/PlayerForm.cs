using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.ComponentModel.DataAnnotations;
using DataService.DTO;

namespace CmsApp.Models
{
    public class PlayerBaseForm
    {
        public int UserId { get; set; }
        [Required]
        public string FullName { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        public string IdentNum { get; set; }
        public int GenderId { get; set; }

        [Required]
        public DateTime? BirthDay { get; set; }
        public string City { get; set; }
        public int Height { get; set; }
        public string Image { get; set; }
        public string IdentCard { get; set; }
        public string Telephone { get; set; }
        public bool MedicalCertificate { get; set; }
        public bool Insurance { get; set; }
        public IEnumerable<SelectListItem> Genders { get; set; }
        public int SeasonId { get; set; }
    }

    public class TeamPlayerForm : PlayerBaseForm
    {
        public int TeamId { get; set; }
        [Range(0, int.MaxValue)]
        public int ShirtNum { get; set; }
        [Range(0, 9999)]
        public int TestResults { get; set; }
        public bool IsHadicapEnabled { get; set; }
        public int? PosId { get; set; }
        public bool IsActive { get; set; }
        public IEnumerable<SelectListItem> Positions { get; set; }
    }

    public class PlayerFormView : PlayerBaseForm
    {
        [Required]
        public string Password { get; set; }
        public bool IsValidUser { get; set; }
        public IEnumerable<TeamDto> PlayerTeams { get; set; }
        public IEnumerable<TeamDto> ManagerTeams { get; set; }
        public int LeagueId { get; set; }
        public int ClubId { get; set; }
        public int CurrentTeamId { get; set; }
        [Range(0, 9999)]
        public int TestResults { get; set; }
        public bool IsHadicapEnabled { get; set; }
        public bool IsPlayereInTeamLessThan3year { get; set; }

        [Range(1, 9999)]
        public int HandicapLevel { get; set; } = 1;
        public int NumberOfTeamsUserPlays { get; set; }
        public List<PlayerHistoryFormView> PlayerHistories { get; set; }
    }
}