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
    
    public partial class UsersJob
    {
        public int Id { get; set; }
        public int JobId { get; set; }
        public int UserId { get; set; }
        public Nullable<int> UnionId { get; set; }
        public Nullable<int> LeagueId { get; set; }
        public Nullable<int> TeamId { get; set; }
        public Nullable<int> ClubId { get; set; }
        public Nullable<int> SeasonId { get; set; }
    
        public virtual Job Job { get; set; }
        public virtual League League { get; set; }
        public virtual Team Team { get; set; }
        public virtual Union Union { get; set; }
        public virtual User User { get; set; }
        public virtual Season Season { get; set; }
    }
}