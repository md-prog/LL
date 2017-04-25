using System.Collections.Generic;
using DataService;
using AppModel;

namespace CmsApp.Models
{
    public class NotificationsViewModel
    {
        public NotificationsViewModel()
        {
            Notifications = new List<NotesMessage>();
        }

        public int EntityId { get; set; }

        public LogicaName RelevantEntityLogicalName { get; set; }

        public List<NotesMessage> Notifications { get; set; }
    }
}