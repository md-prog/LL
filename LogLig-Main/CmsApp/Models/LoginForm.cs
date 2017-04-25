using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace CmsApp.Models
{
    public class LoginForm
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        public string Password { get; set; }
        public string Captcha { get; set; }
        public bool IsRemember { get; set; }
        public bool IsSecure { get; set; }
    }

    public class LoggedView
    {
        public string UserName { get; set; }
        public string RoleType { get; set; }
    }
}