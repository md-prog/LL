
public enum CultEnum
{
    Heb_IL,
    Eng_UK
}

public static class AppRole
{
    public const string Admins = "admins";
    public const string Editors = "editors";
    public const string Workers = "workers";
    public const string Players = "players";
    public const string Fans = "fans";
}

public static class JobRole
{
    public const string UnionManager = "unionmgr";
    public const string LeagueManager = "leaguemgr";
    public const string TeamManager = "teammgr";
    public const string Referee = "referee";
    public const string ClubManager = "clubmgr";
}

public static class GameStatus
{
    public const string Started = "started";
    public const string Ended = "ended";
    public const string Next = "next";
    public const string Closetodate = "closetodate";
}

public static class GamesAlias
{
    public const string BasketBall = "basketball";
    public const string NetBall = "netball";
    public const string WaterPolo = "waterpolo";
    public const string VolleyBall = "volleyball";
}

public static class GameType
{
    public const string Division = "Division";
    public const string Playoff = "Playoff";
    public const string Knockout = "Knockout";
}

public static class PointEditType
{
    public const int WithTheirRecords = 0;
    public const int ResetScores = 1;
    public const int SetTheScores = 2;
}

public enum LogicaName
{
    Union,
    League,
    Team,
    Club
}
