using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;
using AppModel;

namespace CmsApp.Models
{
  public class UnionDetailsForm
  {
    public int UnionId { get; set; }
    public int DocId { get; set; }

    [Required]
    [StringLength(50)]
    public string Name { get; set; }

    public string Logo { get; set; }
    public string PrimaryImage { get; set; }
    public bool IsHadicapEnabled { get; set; }

    [Required]
    [StringLength(250)]
    [DataType(DataType.MultilineText)]
    public string Description { get; set; }

    public string IndexImage { get; set; }
    public string IndexAbout { get; set; }
    public string ContactPhone { get; set; }
    public string Address { get; set; }
    public string Email { get; set; }
    public DateTime? CreateDate { get; set; }
    public string Terms { get; set; }

  }

  public class UnionsForm
  {
    public int SectionId { get; set; }

    [Required]
    public string Name { get; set; }

    public IEnumerable<Union> UnionsList { get; set; }

    public UnionsForm()
    {
      UnionsList = new List<Union>();
    }
  }

    public class EditUnionForm
    {
        public int UnionId { get; set; }
        public string UnionName { get; set; }
        public int SectionId { get; set; }
        public string SectionName { get; set; }
        public bool IsCatchBall { get; set; }
        public int? SeasonId { get; set; }

        public List<Season> Seasons { get; set; }
    }
}