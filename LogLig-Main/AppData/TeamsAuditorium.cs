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
    
    public partial class TeamsAuditorium
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public int AuditoriumId { get; set; }
        public bool IsMain { get; set; }
    
        public virtual Team Team { get; set; }
        public virtual Auditorium Auditorium { get; set; }
    }
}
