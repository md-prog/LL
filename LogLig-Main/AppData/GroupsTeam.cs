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
    
    public partial class GroupsTeam
    {
        public int GroupId { get; set; }
        public int TeamId { get; set; }
        public Nullable<int> Pos { get; set; }
        public Nullable<int> MaxPlayoffPos { get; set; }
        public Nullable<int> MinPlayoffPos { get; set; }
        public Nullable<int> Points { get; set; }
        public Nullable<int> SeasonId { get; set; }
    
        public virtual Group Group { get; set; }
        public virtual Team Team { get; set; }
        public virtual Season Season { get; set; }
    }
}