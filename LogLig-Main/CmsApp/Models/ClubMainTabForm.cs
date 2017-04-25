using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CmsApp.Models
{
    public class ClubMainTabForm
    {
        public int ClubId { get; set; }
        public int SectionId { get; set; }
        public int PlayersCount { get; set; }
        public int OfficialsCount { get; set; }
    }
}