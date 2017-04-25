using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{

    public class FanFriendViewModel : UserBaseViewModel
    {
        public List<FanTeamsViewModel> Teams { get; set; }
    }


    public class FanPrfileViewModel : UserBaseViewModel
    {

        public List<TeamInfoViewModel> Teams { get; set; }

        public List<FanFriendViewModel> Friends { get; set; }

        public int NumberOfFriends { get; set; }

        public int NumberOfCommonFriends { get; set; }
    }

    public class FanOwnPrfileViewModel
    {

        public TeamInfoViewModel TeamInfo { get; set; }

        public NextGameViewModel NextGame { get; set; }

        public GameViewModel LastGame { get; set; }

        public List<FanFriendViewModel> Friends { get; set; }

        public List<UserBaseViewModel> TeamFans { get; set; }
    }
}