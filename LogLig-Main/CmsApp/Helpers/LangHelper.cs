using Resources;

namespace CmsApp.Helpers
{
    public static class LangHelper
    {
        public static string GetRoleName(string name)
        {
            switch (name)
            {
                case AppRole.Admins: return Messages.Admins;
                case AppRole.Editors: return Messages.Editors;
                case AppRole.Workers: return Messages.Workers;
                case AppRole.Players: return Messages.Players;
                case AppRole.Fans: return Messages.Fans;
                default: return "";
            }
        }

        public static string GetJobName(string name)
        {
            switch (name)
            {
                case JobRole.UnionManager: return Messages.AssociationAdmin;
                case JobRole.LeagueManager: return Messages.LeagueAdmin;
                case JobRole.TeamManager: return Messages.TeamAdmin;
                case JobRole.Referee: return Messages.Referee;
                case JobRole.ClubManager: return Messages.ClubManager;
                default: return "";
            }
        }

        public static string GetGender(string name)
        {
            switch (name)
            {
                case "Female": return Messages.Female;
                case "Women": return Messages.Women;
                case "Male": return Messages.Male;
                case "Men": return Messages.Men;
                default: return "";
            }
        }

        public static string GetGameType(string name)
        {
            switch (name)
            {
                case GameType.Division: return Messages.Division;
                case GameType.Playoff: return Messages.Playoff;
                case GameType.Knockout: return Messages.Knockout;
                default: return name;
            }
        }
    }
}