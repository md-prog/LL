using System.Runtime.Serialization;

namespace WebApi.Models
{
    [DataContract]
    public class ClubListViewModel
    {
        [DataMember(Name = "id")]
        public int Id { get; set; }
        [DataMember(Name = "name")]
        public string Name { get; set; }
        [DataMember(Name = "logo")]
        public string Logo { get; set; }
    }
}