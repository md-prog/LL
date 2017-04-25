using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using DataService;

namespace CmsApp.Models
{
    public class Workers
    {

        public int RelevantEntityId { get; set; }

        public LogicaName RelevantEntityLogicalName { get; set; }

        public int JobId { get; set; }
        public int SeasonId { get; set; }
        public int LeagueId { get; set; }

        [Required]
        public string FullName { get; set; }

        public int UserId { get; set; }

        public IEnumerable<SelectListItem> JobsList { get; set; }

        public IEnumerable<UserJobDto> UsersList { get; set; }

    }

    public class CreateWorkerForm
    {

        public int UserId { get; set; }

        public int SeasonId { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string IdentNum { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        public bool IsActive { get; set; }

        public int RelevantEntityId { get; set; }

        public LogicaName RelevantEntityLogicalName { get; set; }

        [Required]
        public int JobId { get; set; }

        public IEnumerable<SelectListItem> JobsList { get; set; }


        public int UserJobId { get; set; }
    }

    public class WorkerForm
    {
        public int UserId { get; set; }
        [Required]
        public string FullName { get; set; }
        [Required]
        public string IdentNum { get; set; }
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        public bool IsActive { get; set; }
    }
}