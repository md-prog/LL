using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AppModel
{
    public partial class PlayoffBracket
    {

        public Team FirstTeam
        {
            get
            {
                return Team;
            }
            set
            {
                Team = value;
            }
        }

        public Team SecondTeam
        {
            get
            {
                return Team1;
            }
            set
            {
                Team1 = value;
            }
        }

        public Team WinnerTeam
        {
            get
            {
                return Team3;
            }
            set
            {
                Team3 = value;
            }
        }

        public Team LoserTeam
        {
            get
            {
                return Team2;
            }
            set
            {
                Team2 = value;
            }
        }

        public PlayoffBracket Parent1
        {
            get
            {
                return PlayoffBracket1;
            }
            set
            {
                PlayoffBracket1 = value;
            }
        }

        public PlayoffBracket Parent2
        {
            get
            {
                return PlayoffBracket2;
            }
            set
            {
                PlayoffBracket2 = value;
            }
        }

        public ICollection<PlayoffBracket> ChildrenSide1
        {
            get
            {
                return PlayoffBrackets1;
            }
            set
            {
                PlayoffBrackets1 = value;
            }
        }

        public ICollection<PlayoffBracket> ChildrenSide2
        {
            get
            {
                return PlayoffBrackets11;
            }
            set
            {
                PlayoffBrackets11 = value;
            }
        }


        public IEnumerable<PlayoffBracket> AllChildren
        {
            get
            {
                return PlayoffBrackets1.Concat(PlayoffBrackets11);
            }
        }


        //public override bool Equals(object obj)
        //{
        //    var other = obj as PlayoffBracket;
        //    if (this.Id == other.Id)
        //    {
        //        return true;
        //    }
        //    else
        //    {
        //        if (this.Team1Id != null && this.Team2Id != null && other.Team1Id != null && other.Team2Id != null)
        //        {
        //            return ((this.Team1Id == other.Team1Id && this.Team2Id == other.Team2Id) ||
        //            (this.Team1Id == other.Team2Id && this.Team2Id == other.Team1Id));
        //        }
        //    }
        //    return false;
        //}
    }

    public enum PlayoffBracketType
    {
        Root = 0,
        Loseer = 1,
        Winner = 2
    }


    // Indicates NotesMessage.TypeId 's value
    public class MessageTypeEnum
    {
        public const int Root = 0x0;
        public const int Reply = 0x1;
        public const int PushNotifyOnly = 0x10;
        public const int NoInAppMessage = 0x10;
        public const int MessagingOnly = 0x20;
        public const int NoPushNotify = 0x20;

        public static bool IsPushNotification(int TypeId)
        {
            return ((TypeId & NoPushNotify) == 0);
        }

        public static bool IsInAppMessage(int TypeId)
        {
            return ((TypeId & NoInAppMessage) == 0);
        }

        public static bool isReply(int TypeId)
        {
            return ((TypeId & Reply) != 0);
        }
    }

}
