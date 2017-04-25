using AppModel;
using System.Collections.Generic;
using System.Linq;
using WebApi.Models;

namespace WebApi.Services
{
    public static class PlayerService
    {

        internal static List<CompactPlayerViewModel> GetActivePlayersByTeamId(int teamId, int seasonId)
        {
            using (DataEntities db = new DataEntities())
            {
                return (from tp in db.TeamsPlayers
                        join user in db.Users on tp.UserId equals user.UserId
                        where tp.TeamId == teamId && tp.SeasonId == seasonId && 
                              tp.IsActive && user.IsActive
                        select new CompactPlayerViewModel
                        {
                            Id = tp.UserId,
                            FullName = tp.User.FullName,
                            Height = tp.User.Height,
                            BirthDay = tp.User.BirthDay,
                            Image = tp.User.Image,
                            ShirtNumber = tp.ShirtNum,
                            PositionTitle = tp.Position != null ? tp.Position.Title : null
                        }).ToList();
            }
        }

        internal static PlayerProfileViewModel GetPlayerProfile(User player)
        {
            var vm = new PlayerProfileViewModel();
            vm.Id = player.UserId;
            vm.FullName = player.FullName;
            vm.Height = player.Height;
            vm.BirthDay = player.BirthDay;
            vm.Image = player.Image;
            vm.UserRole = player.UsersType?.TypeRole;
            return vm;
        }
    }
}