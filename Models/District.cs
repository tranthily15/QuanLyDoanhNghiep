using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace QuanLyDoanhNghiep.Models
{
    public class District
    {
        [Key]
        [JsonPropertyName("code")]
        public string DistrictID { get; set; }
        [JsonPropertyName("name")]
        public string DistrictName { get; set;}
        
        [JsonPropertyName("parent_code")]
        [ForeignKey("Province")]
        public string ProvinceID { get; set; }
        public Province? Province { get; set; }
        public virtual ICollection<JobLocation>? JobLocations { get; set; }
        public virtual ICollection<Company>? Companies { get; set; }
    }
}
