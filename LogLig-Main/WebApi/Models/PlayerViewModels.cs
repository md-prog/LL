﻿using DataService;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApi.Models
{
    public class PlayerBaseViewModel
    {
        public int Id { get; set; }

        public string FullName { get; set; }

        public int? Height { get; set; }

        [JsonIgnore]
        public DateTime? BirthDay { get; set; }

        public int? Age
        {
            get
            {
                if (this.BirthDay.HasValue)
                {
                    DateTime zeroTime = new DateTime(1, 1, 1);
                    DateTime a = this.BirthDay.Value;
                    DateTime b = DateTime.Now;
                    if (a >= b)
                        return 0;
                    TimeSpan span = b - a;
                    int years = (zeroTime + span).Year - 1;
                    return years;
                }
                else
                {
                    return null;
                }
            }
        }

        public string FriendshipStatus { get; set; }

        public string Image { get; set; }
    }


    public class PlayerProfileViewModel : PlayerBaseViewModel
    {
        public IList<FanFriendViewModel> Friends { get; set; }
        public IList<PlayerTeam> Teams { get; set; }
        public IList<GameViewModel> Games { get; set; }

        public string UserRole { get; set; }
    }

    public class CompactPlayerViewModel : PlayerBaseViewModel
    {
        
        public string Image { get; set; }

        //TeamsPlayer

        public int ShirtNumber { get; set; }

        //Position

        public string PositionTitle { get; set; }
    }
}