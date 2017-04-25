using AppModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace WebApi.Controllers
{
    public class BaseLogLigApiController : ApiController
    {
        internal DataEntities db = new DataEntities();
        private User _currentUser = null;

        public BaseLogLigApiController()
        {
            db.Database.Log = s => System.Diagnostics.Debug.WriteLine(s);
        }

        public User CurrentUser
        {
            get
            {
                if (_currentUser == null)
                {
                    _currentUser = db.Users.Find(CurrUserId);
                }
                return _currentUser;
            }
        }

        public int CurrUserId
        {
            get { return Convert.ToInt32(User.Identity.Name); }
        }
    }
}
