using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AppModel;
using DataService;
using DataService.DTO;

namespace CmsApp.Models
{
    public class MovePlayerForm
    {
        #region Constructor

        public MovePlayerForm()
        {
            Teams = new List<TeamDto>();
        }
        #endregion
        public List<TeamDto> Teams { get; set; }
        public int TeamId { get; set; }
        public int CurrentTeamId { get; set; }
        public int? CurrentLeagueId { get; set; }
        public int[] Players { get; set; }
        public int SeasonId { get; set; }
        public int? ClubId { get; set; }
    }
}