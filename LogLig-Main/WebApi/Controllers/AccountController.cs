using AppModel;
using Microsoft.AspNet.Identity;
using System;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using WebApi.Models;
using WebApi.Photo;
using Resources;
using WebApi.Services.Email;
using WebApi.Services;
using System.Web.Http.Description;
using DataService;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;

namespace WebApi.Controllers
{
    [Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : BaseLogLigApiController
    {
        /// <summary>
        /// הרשמת אוהד עם שם משתמש וסיסמה
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // POST api/Account/Register
        [AllowAnonymous]
        [Route("RegisterFan")]
        public async Task<IHttpActionResult> PostRegisterFan(FanRegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (db.Users.Where(u => u.UserName == model.UserName).Count() > 0)
            {
                return BadRequest(Messages.UsernameExists);
            }


            if (db.Users.Where(u => u.Email == model.Email).Count() > 0)
            {
                return BadRequest(Messages.EmailExists);
            }
            var lang = db.Languages.FirstOrDefault(x => x.Code == model.Language);

            var user = new User()
            {
                UserName = model.UserName,
                Email = model.Email,
                Password = Protector.Encrypt(model.Password),
                UsersType = db.UsersTypes.FirstOrDefault(t => t.TypeRole == AppRole.Fans),
                IsActive = true,
                LangId = lang != null ? lang.LangId : 1
            };

            foreach (var item in model.Teams)
            {
                user.TeamsFans.Add(new TeamsFan
                {
                    TeamId = item.TeamId,
                    UserId = user.UserId,
                    LeageId = item.LeagueId
                });
            }


            var newUser = db.Users.Add(user);
            await db.SaveChangesAsync();

            if (newUser != null)
            {
                return Ok();
            }
            else
            {
                return InternalServerError();
            }
        }

        public static Image Crop(Image image, Rectangle selection)
        {
            Bitmap bmp = image as Bitmap;

            // Check if it is a bitmap:
            if (bmp == null)
                throw new ArgumentException("No valid bitmap");

            // Crop the image:
            Bitmap cropBmp = bmp.Clone(selection, bmp.PixelFormat);

            // Release the resources:
            image.Dispose();

            return cropBmp;
        }

        public static Image ScaleImage(Image image, int maxWidth, int maxHeight)
        {
            var ratioX = (double)maxWidth / image.Width;
            var ratioY = (double)maxHeight / image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            var newImage = new Bitmap(newWidth, newHeight);

            using (var graphics = Graphics.FromImage(newImage))
                graphics.DrawImage(image, 0, 0, newWidth, newHeight);

            return newImage;
        }


        /// <summary>
        /// העלאת תמונת פרופיל 
        /// </summary>
        /// <returns></returns>
        [Route("UploadProfilePicture")]
        [AllowAnonymous]
        public async Task<IHttpActionResult> PostUploadProfilePicture()
        {

            if (Request.Content.IsMimeMultipartContent("form-data"))
            {
                try
                {
                    User user = base.CurrentUser;
                    if (user != null)
                    {
                        string appPath = ConfigurationManager.AppSettings["ImageUrl"];
                        string fileName = DateTime.Now.Ticks.ToString() + ".jpeg";
                        string absoluteFolderPath = appPath + user.UserId;
                        string pathToFile = absoluteFolderPath + "\\" + fileName;

                        if (!Directory.Exists(absoluteFolderPath))
                        {
                            Directory.CreateDirectory(absoluteFolderPath);
                        }

                        var provider = new PhotoMultipartFormDataStreamProvider(absoluteFolderPath, fileName);
                        await Request.Content.ReadAsMultipartAsync(provider);

                        using (var image = Image.FromFile(pathToFile))
                        {
                            var size = image.Height >= image.Width ? image.Width : image.Height;
                            var paddingW = (image.Width - size) / 2;
                            var padding = (image.Height - size) / 2;
                            using (var newImage = Crop(image, new Rectangle(paddingW, padding, image.Width - paddingW*2, image.Height - padding*2)))
                            {
                                newImage.Save(absoluteFolderPath + "\\thumb_" + fileName, ImageFormat.Jpeg);
                            }
                        }

                        user.Image = user.UserId + "/thumb_" + fileName;
                        db.Entry(user).State = EntityState.Modified;
                        await db.SaveChangesAsync();

                        return Ok();
                    }
                    else
                    {
                        return BadRequest(Messages.UserNotFound);
                    }
                }
                catch (Exception ex)
                {
                    return BadRequest(Messages.ErrorUploadingFile);
                }
            }
            else
            {
                return BadRequest(Messages.NoFileContent);
            }
        }



        /// <summary>
        /// הרשמת משתמש דרך פייסבוק
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // POST api/Account/Register
        [AllowAnonymous]
        [Route("RegisterFanFB")]
        public IHttpActionResult RegisterFanFB(FBFanRegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (db.Users.Where(u => u.FbId == model.FbId).Count() > 0)
            {
                return BadRequest(Messages.UsernameExists);
            }

            if (db.Users.Where(u => u.Email == model.Email).Count() > 0)
            {
                return BadRequest(Messages.EmailExists);
            }

            var lang = db.Languages.FirstOrDefault(x => x.Code == model.Language);

            var user = new User()
            {
                UserName = model.UserName,
                FullName = model.FullName,
                Email = model.Email,
                FbId = model.FbId,
                UsersType = db.UsersTypes.FirstOrDefault(t => t.TypeRole == AppRole.Fans),
                IsActive = true,
                Image = CreateFacebookProfilePictureUrl(model.FbId),
                LangId = lang != null ? lang.LangId : 1
            };

            foreach (var item in model.Teams)
            {
                user.TeamsFans.Add(new TeamsFan
                {
                    TeamId = item.TeamId,
                    UserId = user.UserId,
                    LeageId = item.LeagueId
                });
            }

            var newUser = db.Users.Add(user);
            db.SaveChanges();
            if (newUser != null)
            {
                return Ok();
            }
            else
            {
                return InternalServerError();
            }
        }

        /// <summary>
        /// הרשמת בעל תפקיד
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        // POST api/Account/Register
        [AllowAnonymous]
        [Route("RegisterWorker")]
        public async Task<IHttpActionResult> RegisterWorker(WorkerRegisterBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var usr = db.Users.FirstOrDefault(u => u.IdentNum == model.PersonalId);

            if (usr == null)
            {
                return BadRequest(Messages.UserNotExists);
            }

            usr.IsActive = true;

            db.Entry(usr).State = EntityState.Modified;

            await db.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// החזרת מידע על המשתמש הנוכחי
        /// </summary>
        /// <param name="unionId"></param>
        /// <returns></returns>
        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        [ResponseType(typeof(UserInfoViewModel))]
        public IHttpActionResult GetUserInfo(int? unionId = null)
        {
            var usr = CurrentUser;
            if (usr == null)
            {
                return NotFound();
            }

            SeasonsRepo seasonsRepo = new SeasonsRepo();
            int? seasonId = unionId != null ? seasonsRepo.GetLastSeasonByCurrentUnionId(unionId.Value) :
                                              (int?) null;

            var vm = new UserInfoViewModel
            {
                Id = usr.UserId,
                UserName = usr.UserName,
                Email = usr.Email,
                FullName = usr.FullName,
                Role = usr.UsersType.TypeRole,
                Image = usr.Image,
            };

            if (usr.UsersJobs != null && usr.UsersJobs.Count > 0)
            {
                //vm.UserJobs = usr.UsersJobs.Select(x => x.JobId).ToList();
                vm.UserJobs = usr.UsersJobs.Select(x => new UserJobDetail()
                {
                    UserId = x.Id,
                    JobId = x.JobId,
                    JobName = (x.Job == null ? null : x.Job.JobName),
                    JobRoleId = (x.Job == null || x.Job.RoleId == null ? 0 : (int)x.Job.RoleId),
                    JobRoleName = (x.Job == null || x.Job.JobsRole == null ? null : x.Job.JobsRole.RoleName),
                    JobRolePriority = (x.Job == null || x.Job.JobsRole == null ? 0 : x.Job.JobsRole.Priority),
                    LeagueId = x.LeagueId,
                    LeagueName = (x.League == null ? null : x.League.Name),
                    TeamId = x.TeamId,
                    TeamName = (x.Team == null ? null : x.Team.Title)
                }).ToList();
            }

            var teamsRepo = new TeamsRepo();
            if (User.IsInRole(AppRole.Fans))
            {
                vm.Teams = teamsRepo.GetFanTeams(usr.UserId, seasonId);
            }
            else if (User.IsInRole(AppRole.Players))
            {
                vm.Teams = teamsRepo.GetPlayerTeams(usr.UserId, seasonId);
            }
            else if (User.IsInRole(AppRole.Workers))
            {
                vm.Teams = usr.UsersJobs.Select(x => new DataService.DTO.TeamDto()
                {
                    TeamId = (x.Team == null ? 0 : x.Team.TeamId),
                    Title = (x.Team == null ? null : x.Team.Title),
                    LeagueId = (x.LeagueId == null ? 0 : (int)x.LeagueId),
                    Logo = (x.Team == null ? null : x.Team.Logo),
                    SeasonId = (x.SeasonId == null ? 0 : x.SeasonId)
                }).ToList();
            }

            return Ok(vm);
        }


        /// <summary>
        /// מחזיר אם חשבון פייסבוק קיים במערכת
        /// </summary>
        /// <param name="facebookId"></param>
        /// <returns></returns>
        // GET api/Account/FBAccounExists/{facebookId}
        [Route("FBAccountExists/{facebookId}")]
        [AllowAnonymous]
        public ExistsResponse GetFBAccountExists(string facebookId)
        {
            return new ExistsResponse { Exists = db.Users.Any(u => u.FbId.Trim().ToLower() == facebookId.Trim().ToLower()) };
        }

        public class UserMail
        {
            public string Mail { get; set; }
        }

        /// <summary>
        /// שולח את הסיסימה למשתמש דרך אימייל 
        /// </summary>
        /// <returns></returns>
        [Route("ForgotPassword")]
        [AllowAnonymous]
        [HttpPost]
        public IHttpActionResult GetForgotPassword(UserMail userMail)
        {
            if (userMail == null || string.IsNullOrEmpty(userMail.Mail) || string.IsNullOrWhiteSpace(userMail.Mail))
            {
                return NotFound();
            }

            var user = db.Users.FirstOrDefault(u => u.Email.Trim().ToLower() == userMail.Mail.Trim().ToLower());
            if (user == null)
            {
                return NotFound();
            }
            EmailService emailService = new EmailService();
            ForgotPasswordEmailModel model = new ForgotPasswordEmailModel
            {
                Name = user.UserName,
                Password = Protector.Decrypt(user.Password)
            };

            var templateService = new RazorEngine.Templating.TemplateService();
            var templateFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Services", "Email", "ForgotPasswordEmailTemplate.cshtml");
            var emailHtmlBody = templateService.Parse(File.ReadAllText(templateFilePath), model, null, null);

            try
            {
                IdentityMessage msg = new IdentityMessage();
                msg.Subject = "Loglig";
                msg.Body = emailHtmlBody;
                msg.Destination = user.Email;
                emailService.SendAsync(msg);
            }
            catch (Exception ex)
            {
                return InternalServerError();
            }
            return Ok();
        }

        /// <summary>
        /// עדכון פרטי משתמש 
        /// StartAlert תזכורת יום לפני המשחק
        /// TimeChange שינוי מועד משחק
        /// GameScores תוצאות משחק עם סיומו
        /// GameRecords שיאים לאחר משחק
        /// </summary>
        /// <returns></returns>
        [Route("Update")]
        public IHttpActionResult PostUpdateUser(UserDetails frm)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("נתון לא תקין");
            }

            var user = base.CurrentUser;

            if (!string.IsNullOrEmpty(frm.OldPassword) && !string.IsNullOrEmpty(frm.NewPassword))
            {
                string pass = Protector.Encrypt(frm.OldPassword);

                if (user.Password != pass)
                {
                    return BadRequest("סיסמה שגוייה");
                }

                user.Password = Protector.Encrypt(frm.NewPassword);
            }
            var lang = db.Languages.FirstOrDefault(x => x.Code == frm.Language);
            if (!string.IsNullOrEmpty(frm.Email))
                user.Email = frm.Email;
            user.FullName = frm.FullName != null ? frm.FullName : user.FullName;
            user.UserName = frm.UserName != null ? frm.UserName : user.UserName;
            user.LangId = lang != null ? lang.LangId : user.LangId;
            if (frm.Teams != null && frm.Teams.Any())
            {
                db.TeamsFans.RemoveRange(user.TeamsFans);
                user.TeamsFans.Clear();

                foreach (var t in frm.Teams)
                {
                    var team = db.Teams.FirstOrDefault(x => x.TeamId == t.TeamId);
                    var league = db.Leagues.FirstOrDefault(x => x.LeagueId == t.LeagueId);
                    if (team != null && league != null)
                        user.TeamsFans.Add(new TeamsFan
                        {
                            TeamId = t.TeamId,
                            UserId = user.UserId,
                            LeageId = t.LeagueId,
                            Team = team,
                            League = league,
                            User = user
                        });
                }
            }
            user.Notifications.ToList().ForEach(t => user.Notifications.Remove(t));

            db.SaveChanges();

            var notesList = db.Notifications.ToList();

            if (!frm.IsStartAlert)
            {
                var nItem = notesList.FirstOrDefault(t => t.Type == "StartAlert");
                user.Notifications.Add(nItem);
            }

            if (!frm.IsTimeChange)
            {
                var nItem = notesList.FirstOrDefault(t => t.Type == "TimeChange");
                user.Notifications.Add(nItem);
            }

            if (!frm.IsGameRecords)
            {
                var nItem = notesList.FirstOrDefault(t => t.Type == "GameRecords");
                user.Notifications.Add(nItem);
            }
            if (!frm.IsGameScores)
            {
                var nItem = notesList.FirstOrDefault(t => t.Type == "GameScores");
                user.Notifications.Add(nItem);
            }

            db.SaveChanges();

            return Ok("saved");
        }

        /// <summary>
        /// קבלת פרטי משתמש 
        /// </summary>
        /// <returns></returns>
        [Route("Details")]
        public IHttpActionResult GetEditUser()
        {
            var vm = new UserDetails();

            var user = base.CurrentUser;
            vm.Email = user.Email;
            vm.UserName = user.UserName;
            vm.FullName = user.FullName;

            var lang = user.LangId != null ? db.Languages.FirstOrDefault(x => x.LangId == user.LangId) : null;

            vm.Language = lang != null ? lang.Code : "he";

            var userNotes = user.Notifications.ToList();

            vm.IsStartAlert = !userNotes.Any(t => t.Type == "StartAlert");
            vm.IsTimeChange = !userNotes.Any(t => t.Type == "TimeChange");
            vm.IsGameRecords = false;//!userNotes.Any(t => t.Type == "GameRecords");
            vm.IsGameScores = !userNotes.Any(t => t.Type == "GameScores");

            return Ok(vm);
        }

        [NonAction]
        private string CreateFacebookProfilePictureUrl(string fbid)
        {
            return string.Format("https://graph.facebook.com/{0}/picture?type=large", fbid);
        }

    }
}
