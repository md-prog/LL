using AppModel;
using System.Collections.Generic;

namespace CmsApp.Models
{
    public class ClubDetailsForm
    {
        public int ClubId { get; set; }
        public int? UnionId { get; set; }
        public int? SectionId { get; set; }
        public int? NGO_Number { get; set; }
        public int? SportCenterId { get; set; }
        public List<SportCenter> SportCenterList { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public string Address { get; set; }
        public string ContactPhone { get; set; }
        public string Email { get; set; }
        public string PrimaryImage { get; set; }
        public string Description { get; set; }
        public string IndexImage { get; set; }
        public string IndexAbout { get; set; }
        public string TermsCondition { get; set; }
        public CultEnum Culture { get; set; }
    }
}