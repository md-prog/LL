//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AppModel
{
    using System;
    using System.Collections.Generic;
    
    public partial class Gender
    {
        public Gender()
        {
            this.Users = new HashSet<User>();
            this.Leagues = new HashSet<League>();
        }
    
        public int GenderId { get; set; }
        public string Title { get; set; }
        public string TitleMany { get; set; }
    
        public virtual ICollection<User> Users { get; set; }
        public virtual ICollection<League> Leagues { get; set; }
    }
}