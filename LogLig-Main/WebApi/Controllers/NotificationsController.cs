using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Threading.Tasks;
using WebApi.Services;
using DataService;
using PushServiceLib;
using WebApi.Models;
using AppModel;

namespace WebApi.Controllers
{
    [Authorize]
    [RoutePrefix("api/Notifications")]
    public class NotificationsController : BaseLogLigApiController
    {
        NotesMessagesRepo nRepo = new NotesMessagesRepo();

        [Route("list")]
        public IHttpActionResult GetList()
        {
            var resList = nRepo.GetByUser(CurrUserId)
                .Where(n => ((n.NotesMessage.TypeId & MessageTypeEnum.NoInAppMessage) == 0) )
                .Select(t => new NotificationsViewModel
                    {
                        MsgId = t.MsgId,
                        IsRead = t.IsRead,
                        Message = t.NotesMessage.Message,
                        SendDate = t.NotesMessage.SendDate
                    });
            return Ok(resList);
        }

        [Route("delete")]
        public IHttpActionResult PostDelete([FromBody]int id)
        {
            nRepo.Delete(id, base.CurrUserId);
            nRepo.Save();

            return Ok();
        }

        [Route("deleteAll")]
        public IHttpActionResult PostDeleteAll(int[] msgsArr)
        {
            var nRepo = new NotesMessagesRepo();

            nRepo.DeleteAll(base.CurrUserId, msgsArr);
            nRepo.Save();

            return Ok();
        }

        [Route("readAll")]
        public IHttpActionResult PostReadAll(int[] msgsArr)
        {
            var nRepo = new NotesMessagesRepo();

            nRepo.SetRead(base.CurrUserId, msgsArr);
            nRepo.Save();

            return Ok();
        }

        [Route("saveToken")]
        public async Task<IHttpActionResult> PostSaveToken(TokenItem item)
        {
            var notifyService = new GamesNotificationsService();
            int id = 0;

            await notifyService.SaveUserDeviceToken(CurrUserId, item.Token, id, item.IsIOS, item.Section);

            return Ok();
        }

        [Route("deleteToken")]
        public async Task<IHttpActionResult> PostDeleteToken(TokenItem item)
        {
            var notifyService = new GamesNotificationsService();

            await notifyService.UnregisterDeviceToken(base.CurrUserId, item.Token);

            return Ok();
        }

        public class TokenItem
        {
            public string Token { get; set; }
            public bool IsIOS { get; set; }
            public string Section { get; set; }
        }
    }
}
