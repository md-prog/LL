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
    
    public partial class LeagueTeams
    {
        public int LeagueId { get; set; }
        public int TeamId { get; set; }
        public Nullable<int> SeasonId { get; set; }
    
        public virtual Team Teams { get; set; }
        public virtual League Leagues { get; set; }
    }
}
