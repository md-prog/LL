

using System.ComponentModel.DataAnnotations;

namespace CmsApp.Models
{
    public class NotificationsForm
    {
        public int? SeasonId { get; set; }
        public int EntityId { get; set; }

        public LogicaName RelevantEntityLogicalName { get; set; }

        [Required]
        [MaxLength(500)]
        public string Message { get; set; }

        public bool  NeedHideTextField { get; set; }
    }
}