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
    
    public partial class GameSet
    {
        public int GameSetId { get; set; }
        public int HomeTeamScore { get; set; }
        public int GuestTeamScore { get; set; }
        public int GameCycleId { get; set; }
        public int SetNumber { get; set; }
        public bool IsGoldenSet { get; set; }
    
        public virtual GamesCycle GamesCycle { get; set; }
    }
}
