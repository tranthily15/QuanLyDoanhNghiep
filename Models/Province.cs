using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace QuanLyDoanhNghiep.Models
{
    public class Province
    {
        [Key]
        [JsonPropertyName("code")]
        public string ProvinceID { get; set; }
        [JsonPropertyName("name")]
        public string ProvinceName { get; set; }
        [JsonPropertyName("type")]
        public string Type { get; set; }
        [JsonPropertyName("name_with_type")]
        public string NameWithType { get; set; }
        public virtual ICollection<JobLocation>? JobLocations { get; set; }
        public virtual ICollection<District>? Districts { get; set; }
        public virtual ICollection<Company>? Companies { get; set; }
    }
}
