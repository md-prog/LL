using AppModel;
using System.Linq;
using WebApi.Models;

namespace WebApi.Services
{
    public static class FansService
    {

        internal static FanPrfileViewModel GetFanProfileAsAnonymousUser(User fan, int? seasonId)
        {
            var fpvm = new FanPrfileViewModel
            {
                Id = fan.UserId,
                UserName = fan.UserName,
                Image = fan.Image,
                FriendshipStatus = FriendshipStatus.No,
                Teams = TeamsService.GetFanTeamsWithStatistics(fan, seasonId),
                Friends = null
            };
            return fpvm;
        }

        internal static FanPrfileViewModel GetFanProfileAsLoggedInUser(User user, User fan, int? seasonId)
        {
            var Friends = FriendsService.GetAllFanFriends(fan.UserId, user.UserId);
            var fpvm = new FanPrfileViewModel
            {
                Id = fan.UserId,
                UserName = fan.UserName,
                Image = fan.Image,
                FriendshipStatus = FriendsService.AreFriends(user.UserId, fan.UserId),
                Teams = TeamsService.GetFanTeamsWithStatistics(fan, seasonId),
                Friends = Friends,
                NumberOfFriends = Friends.Count,
                NumberOfCommonFriends = Friends.Count(f => f.FriendshipStatus == FriendshipStatus.Yes)
            };
            return fpvm;
        }
    }
}