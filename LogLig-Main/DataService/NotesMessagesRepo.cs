using System;
using System.Collections.Generic;
using System.Linq;
using AppModel;
using log4net;
using log4net.Config;

namespace DataService
{

    public class NotesMessagesRepo : BaseRepo
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(NotesMessagesRepo));

        public void Create(NotesMessage msg)
        {
            db.NotesMessages.Add(msg);
        }

        public IQueryable<NotesRecipient> GetByUser(int userId)
        {
            return (from n in db.NotesMessages
                    from r in n.NotesRecipients
                    where r.UserId == userId && r.IsArchive == false
                    orderby n.MsgId descending
                    select r);
        }

        public void SetRead(int userId, int[] msgsArr)
        {
            var msgList = db.NotesRecipients.Where(t => t.UserId == userId && msgsArr.Contains(t.MsgId));
            foreach (var m in msgList)
            {
                m.IsRead = true;
            }
        }

        public NotesMessage GetMessageById(int msgId)
        {
            return db.NotesMessages.Find(msgId);
        }

        public void DeleteMessage(int messageId)
        {
            var sentMessage = db.SentMessages.FirstOrDefault(x => x.MessageId == messageId);

            if (sentMessage == null) return;

            var message = db.NotesMessages.FirstOrDefault(x => x.MsgId == messageId);

            if(message == null) return;

            db.SentMessages.Remove(sentMessage);
            db.NotesMessages.Remove(message);
        }

        public void Delete(int msgId, int userId)
        {
            var msg = db.NotesRecipients.Where(t => t.MsgId == msgId && t.UserId == userId).FirstOrDefault();
            msg.IsArchive = true;
        }

        public void DeleteAll(int userId, int[] msgsArr)
        {
            var msgList = db.NotesRecipients.Where(t => t.UserId == userId && msgsArr.Contains(t.MsgId));
            foreach (var msg in msgList)
            {
                msg.IsArchive = true;
            }
        }

        public void SendToTeam(int seasonId, int teamId, string message)
        {
            log.InfoFormat("SendToTeam: seasonId={0}, teamId={1}, message={2}", seasonId, teamId, message);

            List<int> playersIds = (from teamPlayer in db.TeamsPlayers
                                    join user in db.Users on teamPlayer.UserId equals user.UserId
                                    where teamPlayer.TeamId == teamId && teamPlayer.IsActive && teamPlayer.SeasonId == seasonId &&
                                          user.IsArchive == false && user.IsActive && user.IsBlocked == false
                                    select user.UserId).ToList();
            List<int> officialsIds = (from jobs in db.UsersJobs
                                      join user in db.Users on jobs.UserId equals user.UserId
                                      where jobs.TeamId == teamId && jobs.SeasonId == seasonId &&
                                      user.IsArchive == false && user.IsActive && user.IsBlocked == false
                                      select user.UserId).ToList();

            List<int> userIds = playersIds.Union(officialsIds).ToList();

            int? messageId = SaveMessage(userIds, message);
            if (messageId.HasValue)
            {
                db.SentMessages.Add(new SentMessage()
                {
                    MessageId = messageId.Value,
                    TeamId = teamId,
                    SeasonId = seasonId
                });
            }

            Save();
        }

        public void SendToLeague(int seasonId, int leagueId, string message)
        {
            log.InfoFormat("SendToLeague: seasonId={0}, leagueId={1}, message={2}", seasonId, leagueId, message);

            List<int> playersIds = (from leagueTeam in db.LeagueTeams
                                    join teamPlayer in db.TeamsPlayers on leagueTeam.TeamId equals teamPlayer.TeamId
                                    join user in db.Users on teamPlayer.UserId equals user.UserId
                                    where leagueTeam.LeagueId == leagueId && leagueTeam.SeasonId == seasonId &&
                                          teamPlayer.SeasonId == seasonId && teamPlayer.IsActive &&
                                          user.IsActive && user.IsArchive == false && user.IsBlocked == false
                                    select user.UserId).ToList();

            List<int> officialsIds = (from jobs in db.UsersJobs
                                      join user in db.Users on jobs.UserId equals user.UserId
                                      where jobs.LeagueId == leagueId && jobs.SeasonId == seasonId &&
                                      user.IsArchive == false && user.IsActive && user.IsBlocked == false
                                      select user.UserId).ToList();

            List<int> userIds = playersIds.Union(officialsIds).ToList();

            int? messageId = SaveMessage(userIds, message);

            if (messageId.HasValue)
            {
                db.SentMessages.Add(new SentMessage()
                {
                    MessageId = messageId.Value,
                    LeagueId = leagueId,
                    SeasonId = seasonId
                });
            }

            Save();
        }

        public void SendToUnion(int seasonId, int unionId, string message)
        {
            log.InfoFormat("SendToUnion: seasonId={0}, unionId={1}, message={2}", seasonId, unionId, message);

            List<int> playersIds = (from season in db.Seasons
                                    join union in db.Unions on season.UnionId equals union.UnionId
                                    join league in db.Leagues on union.UnionId equals league.UnionId
                                    join leagueTeam in db.LeagueTeams on league.LeagueId equals leagueTeam.LeagueId
                                    join teamPlayer in db.TeamsPlayers on leagueTeam.TeamId equals teamPlayer.TeamId
                                    join user in db.Users on teamPlayer.UserId equals user.UserId
                                    where season.Id == seasonId && union.UnionId == unionId && league.UnionId == unionId &&
                                          leagueTeam.SeasonId == seasonId && teamPlayer.SeasonId == seasonId &&
                                          teamPlayer.IsActive && user.IsActive && user.IsArchive == false && user.IsBlocked == false
                                    select user.UserId).Distinct().ToList();

            List<int> officialsIds = (from jobs in db.UsersJobs
                                      join user in db.Users on jobs.UserId equals user.UserId
                                      where jobs.UnionId == unionId && jobs.SeasonId == seasonId &&
                                      user.IsArchive == false && user.IsActive && user.IsBlocked == false
                                      select user.UserId).ToList();

            List<int> userIds = playersIds.Union(officialsIds).ToList();

            int? messageId = SaveMessage(userIds, message);
            if (messageId.HasValue)
            {
                db.SentMessages.Add(new SentMessage()
                {
                    MessageId = messageId.Value,
                    UnionId = unionId,
                    SeasonId = seasonId
                });
            }

            Save();
        }

        public int? SendToUsers(IReadOnlyCollection<int> userIds, string message)
        {
            log.InfoFormat("SendToUsers: userIds={0}, message={1}", userIds, message);
            return SaveMessage(userIds, message);
        }

        public int? SendToUsers(IReadOnlyCollection<int> userIds, string message, int messageType)
        {
            log.InfoFormat("SendToUsers: userIds={0}, message={1}, type={2}", userIds, message, messageType);
            return SaveMessage(userIds, message, messageType);
        }

        private int? SaveMessage(IReadOnlyCollection<int> playersIds, string message, int messageType)
        {
            if (playersIds.Count > 0)
            {
                var msg = new NotesMessage
                {
                    Message = message,
                    SendDate = DateTime.Now,
                    TypeId = messageType
                };

                db.NotesMessages.Add(msg);

                foreach (int userId in playersIds)
                {
                    var nr = new NotesRecipient { MsgId = msg.MsgId, UserId = userId };
                    db.NotesRecipients.Add(nr);
                }

                Save();
                return msg.MsgId;
            }

            return null;
        }


        private int? SaveMessage(IReadOnlyCollection<int> playersIds, string message)
        {
            return this.SaveMessage(playersIds, message, MessageTypeEnum.Root);
        }

        public void SendToClub(int seasonId, int clubId, string message)
        {
            //List<int> clubPlayers = (from club in db.ClubTeams
            //                         join team in db.Teams on club.TeamId equals team.TeamId
            //                         join teamplayers in db.TeamsPlayers on team.TeamId equals teamplayers.TeamId
            //                         where club.ClubId == clubId && teamplayers.IsActive == false
            //                         select teamplayers.UserId)
            //                        .ToList();
            //if (clubPlayers.Any())
            //{
            //    var msg = new NotesMessage
            //    {
            //        Message = message,
            //        SendDate = DateTime.Now
            //    };

            //    db.NotesMessages.Add(msg);

            //    foreach (int userId in clubPlayers)
            //    {
            //        var nr = new NotesRecipient { MsgId = msg.MsgId, UserId = userId };
            //        db.NotesRecipients.Add(nr);
            //    }
            //}
        }

        public List<NotesMessage> GetLeagueTeamMessages(int seasonId, int teamId)
        {
            Func<SentMessage, bool> predicate = (sentMessage) => sentMessage.SeasonId == seasonId && 
                                                                 sentMessage.TeamId == teamId;
            var teamMessages = GetMessages(predicate);

            return teamMessages;
        }

        public List<NotesMessage> GetLeagueMessages(int seasonId, int leagueId)
        {
            Func<SentMessage, bool> predicate = (sentMessage) => sentMessage.SeasonId == seasonId &&
                                                                 sentMessage.LeagueId == leagueId;
            var leagueMessages = GetMessages(predicate);

            return leagueMessages;
        }

        public List<NotesMessage> GetUnionMessages(int seasonId, int unionId)
        {
            Func<SentMessage, bool> predicate = (sentMessage) => sentMessage.SeasonId == seasonId &&
                                                                 sentMessage.UnionId == unionId;
            var unionMessages = GetMessages(predicate);

            return unionMessages;
        }

        public List<NotesMessage> GetMessages(Func<SentMessage, bool> predicate)
        {
            var messages = (from sentMessage in db.SentMessages.Where(predicate)
                                join message in db.NotesMessages on sentMessage.MessageId equals message.MsgId
                                select message).ToList();
            return messages;
        }

        public void AddDeviceToUser(int userId, string deviceToken, int servToken)
        {
            var device = db.UsersDvices.FirstOrDefault(t => t.UserId == userId && 
                                                            t.DeviceToken == deviceToken && 
                                                            t.ServiceToken == servToken);

            if (device == null)
            {
                device = new UsersDvice
                {
                    UserId = userId,
                    DeviceToken = deviceToken,
                    ServiceToken = servToken
                };

                db.UsersDvices.Add(device);
                db.SaveChanges();
            }
        }

        public void DeleteDeviceToken(int userId, string token)
        {
            var device = db.UsersDvices.Where(t => t.UserId == userId && t.DeviceToken == token).FirstOrDefault();
            if (device != null)
            {
                db.UsersDvices.Remove(device);
                db.SaveChanges();
            }
        }

        public int[] GetTeamTokents(int teamId)
        {
            return (from t in db.Teams
                    from u in t.TeamsPlayers
                    from tk in u.User.UsersDvices
                    where t.TeamId == teamId
                    select (int)tk.ServiceToken).ToArray();
        }

        public IEnumerable<NotesMessage> GetClubMessages(int clubId)
        {
            return new List<NotesMessage>();
        }
    }
}
