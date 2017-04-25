using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Resources;
using CmsApp.Helpers;
using CmsApp.Models;
using AppModel;
using DataService;
using Omu.ValueInjecter;

namespace CmsApp.Controllers
{
    public class JobsController : AdminController
    {
        //Jobs
        public ActionResult Index(int id)
        {
            var rolesList = jobsRepo.GetRoles().Select(r => new SelectListItem
            {
                Value = r.RoleId.ToString(),
                Text = LangHelper.GetJobName(r.RoleName)
            });

            var vm = new JobForm();

            vm.SectionId = id;
            vm.JobsList = jobsRepo.GetBySection(id);
            vm.Roles = new SelectList(rolesList, "Value", "Text");

            return PartialView("_List", vm);
        }

        [HttpPost]
        public ActionResult Edit(JobForm frm)
        {
            var job = new Job();

            if (frm.JobId == 0)
                jobsRepo.Add(job);
            else
                job = jobsRepo.GetById(frm.JobId);

            UpdateModel(job);

            jobsRepo.Save();

            return RedirectToAction("Index", new { id = frm.SectionId });
        }

        public ActionResult Delete(int id)
        {
            var item = jobsRepo.GetById(id);
            item.IsArchive = true;
            jobsRepo.Save();

            return RedirectToAction("Index", new { id = item.SectionId });
        }

        //Worker List
        public ActionResult WorkerList(int id, LogicaName logicalName, int seasonId, int? leagueId)
        {
            var vm = GetWorkerVMByRelevantEntity(id, logicalName, seasonId, leagueId);
            
            ViewBag.SeasonId = seasonId;

            return PartialView("_WorkerList", vm);
        }

        private Workers GetWorkerVMByRelevantEntity(int id, LogicaName logicalName, int seasonId, int? leagueId)
        {
            var frm = new Workers
            {
                RelevantEntityId = id,
                RelevantEntityLogicalName = logicalName,
                SeasonId = seasonId,
                LeagueId = (leagueId == null ? 0 : (int)leagueId)
            };

            switch (logicalName)
            {
                case LogicaName.Union:
                    frm.UsersList = base.jobsRepo.GetUnionUsersJobs(id);
                    frm.JobsList = new SelectList(base.jobsRepo.GetByUnion(id), "JobId", "JobName");
                    break;
                case LogicaName.League:
                    frm.UsersList = base.jobsRepo.GetLeagueUsersJobs(id);
                    frm.JobsList = new SelectList(base.jobsRepo.GetByLeague(id), "JobId", "JobName");
                    break;
                case LogicaName.Team:
                    frm.UsersList = base.jobsRepo.GetTeamUsersJobs(id);
                    frm.JobsList = new SelectList(base.jobsRepo.GetByTeam(id), "JobId", "JobName");
                    break;
                case LogicaName.Club:
                    frm.UsersList = base.jobsRepo.GetClubOfficials(id);
                    frm.JobsList = new SelectList(base.jobsRepo.GetClubJobs(id), "JobId", "JobName");
                    break;
            }

            return frm;
        }

        // CRUD Worker
        public ActionResult EditWorker(int id, int relevantEntityId, LogicaName logicalName, int seasonId, int? leagueId)
        {
            var frm = new CreateWorkerForm
            {
                RelevantEntityId = relevantEntityId,
                RelevantEntityLogicalName = logicalName,
            };
            switch (frm.RelevantEntityLogicalName)
            {
                case LogicaName.Union:
                    frm.JobsList = new SelectList(jobsRepo.GetByUnion(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.League:
                    frm.JobsList = new SelectList(jobsRepo.GetByLeague(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Team:
                    frm.JobsList = new SelectList(jobsRepo.GetByTeam(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Club:
                    frm.JobsList = new SelectList(jobsRepo.GetClubJobs(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                default:
                    frm.JobsList = new List<SelectListItem>();
                    break;
            }

            UsersJob userJob = jobsRepo.GetUsersJobById(id);

            frm.InjectFrom(userJob.User);
            frm.JobId = userJob.JobId;
            frm.UserJobId = userJob.Id;
            frm.SeasonId = seasonId;
            if (!string.IsNullOrEmpty(userJob.User.Password))
            {
                frm.Password = Protector.Decrypt(userJob.User.Password);
            }
            return PartialView("_EditWorker", frm);
        }

        [HttpPost]
        public ActionResult EditWorker(CreateWorkerForm frm)
        {
            User user = usersRepo.GetById(frm.UserId);
            UsersJob userJob = jobsRepo.GetUsersJobById(frm.UserJobId);

            if (user == null)
            {
                string err = Messages.UserNotExists;
                ModelState.AddModelError("FullName", err);
            }

            if (userJob == null)
            {
                string err = Messages.RoleNotExists;
                ModelState.AddModelError("UserJob", err);
            }

            if (usersRepo.GetByIdentityNumber(frm.IdentNum) != null && frm.IdentNum != user.IdentNum)
            {
                string tst = Messages.IdIsAlreadyExists;
                tst = String.Format(tst, "\"");
                ModelState.AddModelError("IdentNum", tst);
            }

            if (usersRepo.GetByEmail(frm.Email) != null && frm.Email != user.Email)
            {
                string tst = Messages.EmailAlreadyExists;
                tst = string.Format(tst, "\"");
                ModelState.AddModelError("Email", tst);
            }

            bool isUserInJob = jobsRepo.IsUserInJob(frm.RelevantEntityLogicalName, frm.RelevantEntityId, frm.JobId, userJob.UserId);
            if (isUserInJob && userJob.JobId != frm.JobId)
            {
                ModelState.AddModelError("JobId", Messages.UserAlreadyHasThisRole);
            }

            switch (frm.RelevantEntityLogicalName)
            {
                case LogicaName.Union:
                    frm.JobsList = new SelectList(jobsRepo.GetByUnion(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.League:
                    frm.JobsList = new SelectList(jobsRepo.GetByLeague(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Team:
                    frm.JobsList = new SelectList(jobsRepo.GetByTeam(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Club:
                    frm.JobsList = new SelectList(jobsRepo.GetClubJobs(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                default:
                    frm.JobsList = new List<SelectListItem>();
                    break;
            }

            if (ModelState.IsValid)
            {
                UpdateModel(user);
                user.Password = Protector.Encrypt(frm.Password);
                usersRepo.Save();

                userJob.SeasonId = frm.SeasonId;
                UpdateModel(userJob);
                userJob.LeagueId = (userJob.LeagueId == 0 ? (int?)null : userJob.LeagueId);
                jobsRepo.Save();
                 
                TempData["WorkerAddedSuccessfully"] = true;
            }

            return PartialView("_EditWorker", frm);
        }

        public ActionResult CreateWorker(int relevantEntityId, LogicaName logicalName, int seasonId)
        {
            var frm = new CreateWorkerForm
            {
                RelevantEntityId = relevantEntityId,
                RelevantEntityLogicalName = logicalName,
                SeasonId = seasonId
            };

            switch (frm.RelevantEntityLogicalName)
            {
                case LogicaName.Union:
                    frm.JobsList = new SelectList(jobsRepo.GetByUnion(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.League:
                    frm.JobsList = new SelectList(jobsRepo.GetByLeague(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Team:
                    frm.JobsList = new SelectList(jobsRepo.GetByTeam(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Club:
                    frm.JobsList = new SelectList(jobsRepo.GetClubJobs(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                default:
                    frm.JobsList = new List<SelectListItem>();
                    break;
            }
            frm.IsActive = true;
            return PartialView("_CreateWorker", frm);
        }

        [HttpPost]
        public ActionResult CreateWorker(CreateWorkerForm frm)
        {
            if (usersRepo.GetByIdentityNumber(frm.IdentNum) != null)
            {
                var tst = Messages.IdIsAlreadyExists;
                tst = string.Format(tst, "\"");
                ModelState.AddModelError("IdentNum", tst);
            }

            if (usersRepo.GetByEmail(frm.Email) != null)
            {
                var tst = Messages.EmailAlreadyExists;
                tst = string.Format(tst, "\"");
                ModelState.AddModelError("Email", tst);
            }

            switch (frm.RelevantEntityLogicalName)
            {
                case LogicaName.Union:
                    frm.JobsList = new SelectList(jobsRepo.GetByUnion(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.League:
                    frm.JobsList = new SelectList(jobsRepo.GetByLeague(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Team:
                    frm.JobsList = new SelectList(jobsRepo.GetByTeam(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                case LogicaName.Club:
                    frm.JobsList = new SelectList(jobsRepo.GetClubJobs(frm.RelevantEntityId), "JobId", "JobName");
                    break;
                default:
                    frm.JobsList = new List<SelectListItem>();
                    break;
            }

            if (ModelState.IsValid)
            {
                var user = new User();
                UpdateModel(user);
                user.Password = Protector.Encrypt(frm.Password);
                user.TypeId = 3;
                usersRepo.Create(user);

                var uJob = new UsersJob
                {
                    JobId = frm.JobId,
                    UserId = user.UserId,
                    SeasonId = frm.SeasonId
                };

                switch (frm.RelevantEntityLogicalName)
                {
                    case LogicaName.Union:
                        uJob.UnionId = frm.RelevantEntityId;
                        break;
                    case LogicaName.League:
                        uJob.LeagueId = frm.RelevantEntityId;
                        break;
                    case LogicaName.Team:
                        uJob.TeamId = frm.RelevantEntityId;
                        break;
                    case LogicaName.Club:
                        uJob.ClubId = frm.RelevantEntityId;
                        break;
                }

                var jRepo = new JobsRepo();
                jRepo.AddUsersJob(uJob);
                jRepo.Save();

                ViewBag.SeasonId = frm.SeasonId;
                TempData["WorkerAddedSuccessfully"] = true;
            }

            return PartialView("_CreateWorker", frm);
        }

        public ActionResult DeleteWorker(int id, int relevantEntityId, LogicaName logicalName, int seasonId)
        {
            jobsRepo.RemoveUsersJob(id);

            return RedirectToAction("WorkerList", new { id = relevantEntityId, logicalName = logicalName, seasonId = seasonId });
        }

        [HttpPost]
        public ActionResult Search(string term, int id, LogicaName logicalName)
        {
            int sectionId = -1;

            IEnumerable<ListItemDto> resList;

            switch (logicalName)
            {
                case LogicaName.Union:
                    sectionId = secRepo.GetByUnionId(id).SectionId;
                    break;
                case LogicaName.League:
                    sectionId = secRepo.GetByLeagueId(id).SectionId;
                    break;
                case LogicaName.Team:
                    sectionId = secRepo.GetSectionByTeamId(id).SectionId;
                    break;
                case LogicaName.Club:
                    sectionId = secRepo.GetByClubId(id).SectionId;
                    break;
            }

            resList = usersRepo.SearchSectionUser(sectionId, AppRole.Workers, term, 999);
            return Json(resList);
        }

        public ActionResult AddExistingUser(Workers frm)
        {
            var uRepo = new UsersRepo();
            var usr = uRepo.GetById(frm.UserId);

            ViewBag.SeasonId = frm.SeasonId;

            if (usr == null || usr.FullName != frm.FullName)
            {
                ModelState.AddModelError("FullName", Messages.UserNotExists);
                return PartialView("_WorkerList", GetWorkerVMByRelevantEntity(frm.RelevantEntityId, frm.RelevantEntityLogicalName, frm.SeasonId, frm.LeagueId));
            }

            var uJob = new UsersJob
            {
                JobId = frm.JobId,
                UserId = usr.UserId,
                SeasonId = frm.SeasonId,
                LeagueId = (frm.LeagueId == 0 ? (int?)null : frm.LeagueId)
            };

            switch (frm.RelevantEntityLogicalName)
            {
                case LogicaName.Union:
                    uJob.UnionId = frm.RelevantEntityId;
                    break;
                case LogicaName.League:
                    uJob.LeagueId = frm.RelevantEntityId;
                    break;
                case LogicaName.Team:
                    uJob.TeamId = frm.RelevantEntityId;
                    break;
                case LogicaName.Club:
                    uJob.ClubId = frm.RelevantEntityId;
                    break;
            }

            var jobsRepo = new JobsRepo();

            if (jobsRepo.IsUserInJob(frm.RelevantEntityLogicalName, frm.RelevantEntityId, uJob.JobId, usr.UserId))
            {
                ModelState.AddModelError("FullName", Messages.UserAlreadyHasThisRole);
                return PartialView("_WorkerList", GetWorkerVMByRelevantEntity(frm.RelevantEntityId, frm.RelevantEntityLogicalName, frm.SeasonId, frm.LeagueId));
            }

            jobsRepo.AddUsersJob(uJob);
            jobsRepo.Save();

            TempData["SavedId"] = uJob.UserId;

            return PartialView("_WorkerList", GetWorkerVMByRelevantEntity(frm.RelevantEntityId, frm.RelevantEntityLogicalName, frm.SeasonId, frm.LeagueId));
        }
    }
}