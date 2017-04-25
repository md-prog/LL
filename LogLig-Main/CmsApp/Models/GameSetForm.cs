using System;
using System.ComponentModel.DataAnnotations;

namespace CmsApp.Models
{
    public class GameSetForm
    {

        public int GameCycleId { get; set; }

        public string warnOnReset { get; set; }

        [Range(0, Int32.MaxValue)]
        public int HomeTeamScore { get; set; }

        [Range(0, Int32.MaxValue)]
        public int GuestTeamScore { get; set; }

        public bool IsGoldenSet { get; set; }
    }
}