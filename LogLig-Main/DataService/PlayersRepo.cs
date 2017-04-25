using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AppModel;

namespace DataService
{
    public class TeamPlayerItem
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int ShirtNum { get; set; }
        public int? PosId { get; set; }
        public string FullName { get; set; }
        public string IdentNum { get; set; }
        public bool IsActive { get; set; }
        public int SeasonId { get; set; }
        public int TeamId { get; set; }
    }

    public class PlayersRepo : BaseRepo
    {
        public PlayersRepo() : base() { }
        public PlayersRepo(DataEntities db) : base(db) { }

        public User GetUserByIdentNum(string identNum)
        {
            return db.Users.Where(t => t.IdentNum == identNum).FirstOrDefault();
        }

        public TeamsPlayer GetTeamPlayerByIdentNum(string identNum)
        {
            return db.TeamsPlayers.FirstOrDefault(t => t.User.IdentNum == identNum);
        }

        public object GetTeamPlayerByIdentNumAndTeamId(string identNum, int teamId)
        {
            return db.TeamsPlayers.FirstOrDefault(t => t.User.IdentNum == identNum && t.TeamId == teamId);
        }


        public void AddToTeam(TeamsPlayer item)
        {
            db.TeamsPlayers.Add(item);
        }

        public TeamsPlayer GetTeamPlayer(int teamId, int userId, int? posId)
        {
            return db.TeamsPlayers.Where(t => t.TeamId == teamId && t.UserId == userId && t.PosId == posId).FirstOrDefault();
        }

        public TeamsPlayer GetTeamsPlayerById(int id)
        {
            return db.TeamsPlayers.FirstOrDefault(t => t.Id == id);
        }

        public TeamsPlayer GetTeamPlyaerBySeasonId(int id, int? seasonId)
        {
            return db.TeamsPlayers.FirstOrDefault(x => x.Id == id && x.SeasonId == seasonId);
        }

        public void ReoveFromTeam(TeamsPlayer item)
        {
            db.TeamsPlayers.Remove(item);
        }

        public IEnumerable<TeamPlayerItem> GetTeamPlayers(int teamId, int seasonId)
        {
            return (from t1 in db.Users
                    from t2 in t1.TeamsPlayers
                    where !t1.IsArchive && t2.TeamId == teamId && t2.SeasonId == seasonId
                    orderby t1.UserId
                    select new TeamPlayerItem
                    {
                        Id = t2.Id,
                        UserId = t2.UserId,
                        ShirtNum = t2.ShirtNum,
                        PosId = t2.PosId,
                        FullName = t1.FullName,
                        IdentNum = t1.IdentNum,
                        IsActive = t2.IsActive,
                        TeamId = teamId,
                        SeasonId = seasonId
                    }).ToList();
        }

        public bool ShirtNumberExists(int teamId, int shirtNum)
        {
            return db.TeamsPlayers.Any(tp => tp.TeamId == teamId && tp.ShirtNum == shirtNum);
        }

        public bool PlayerExistsInTeam(int teamId, int userId)
        {
            return db.TeamsPlayers.Any(tp => tp.TeamId == teamId && tp.UserId == userId);
        }

        public bool ShirtNumberExists(int teamId, int shirtNum, int updateId, int seasonId)
        {
            return db.TeamsPlayers
                .Where(tp => tp.TeamId == teamId
                    && tp.SeasonId == seasonId 
                    && tp.ShirtNum == shirtNum 
                    && tp.Id != updateId).Count() > 0;
        }

        public void MovePlayersToTeam(int teamId, int[] playerIds, int currentTeamId, int seasonId)
        {
            var teamPlayers = db.TeamsPlayers.Where(x => x.TeamId == currentTeamId && playerIds.Contains(x.UserId) && seasonId == x.SeasonId).ToList();
            var movedPlayers = new List<TeamsPlayer>();
            foreach (var teamPlayer in teamPlayers)
            {
                var newTeamPlayer = new TeamsPlayer();
                newTeamPlayer.TeamId = teamId;
                newTeamPlayer.UserId = teamPlayer.UserId;
                newTeamPlayer.PosId = teamPlayer.PosId;
                newTeamPlayer.ShirtNum = teamPlayer.ShirtNum;
                newTeamPlayer.IsActive = teamPlayer.IsActive;
                newTeamPlayer.SeasonId = teamPlayer.SeasonId ?? seasonId;

                db.TeamsPlayers.Add(newTeamPlayer);
                db.SaveChanges();
                movedPlayers.Add(newTeamPlayer);
            }
            db.TeamsPlayers.RemoveRange(teamPlayers);
            db.SaveChanges();

            if (movedPlayers.Count > 0)
            {
                foreach (var movedPlayer in movedPlayers)
                {
                    var history = new PlayerHistory();

                    history.SeasonId = movedPlayer.SeasonId ?? seasonId;
                    history.TeamId = movedPlayer.TeamId;
                    history.UserId = movedPlayer.UserId;
                    history.TimeStamp = DateTime.UtcNow.Ticks;

                    db.PlayerHistory.Add(history);
                }
                db.SaveChanges();

            }
        }
    }
}
