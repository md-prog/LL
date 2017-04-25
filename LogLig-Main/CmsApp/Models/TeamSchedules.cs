using AppModel;
using System.Collections.Generic;
using System.Linq;

namespace CmsApp.Models
{
    public class TeamSchedules
    {
        public int TeamId { get; set; }
        public int SeasonId { get; set; }
        public IList<IGrouping<League, IGrouping<Stage, GamesCycle>>> LeaguesWithCycles { get; set; }
        public IList<Club> Clubs { get; set; }
    }
}