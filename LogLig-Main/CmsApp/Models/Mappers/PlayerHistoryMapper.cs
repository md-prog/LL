using System;
using System.Collections.Generic;
using System.Linq;
using AppModel;

namespace CmsApp.Models.Mappers
{
    public static class PlayerHistoryMapper
    {
        public static PlayerHistoryFormView ToViewModel(this PlayerHistory model)
        {
            var vm = new PlayerHistoryFormView
            {
                Player = model.Users.FullName,
                Season = model.Seasons.Name,
                Team = model.Teams.Title,
                Date = new DateTime(model.TimeStamp)
            };

            return vm;
        }


        public static List<PlayerHistoryFormView> ToViewModel(this List<PlayerHistory> model)
        {
            return model.Select(x => x.ToViewModel()).ToList();
        }
    }
}