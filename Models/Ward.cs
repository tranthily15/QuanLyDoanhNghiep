using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace QuanLyDoanhNghiep.Models
{
    public class Ward
    {
        [Key]
        [JsonPropertyName("code")]
        public string WardID { get; set; }
        [JsonPropertyName("name")]
        public string WardName { get; set;}
        
        [JsonPropertyName("parent_code")]
        [ForeignKey("District")]
        public string DistrictID { get; set; }
        public District? District { get; set; }
        public virtual ICollection<JobLocation>? JobLocations { get; set; }
        public virtual ICollection<Company>? Companies { get; set; }
    }
}
