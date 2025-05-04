using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class JobLocation
    {
        [Key]
        public int JobLocationID { get; set; }
        [ForeignKey("JobPosition")]
        public int? PositionID { get; set; }
        public string Street { get; set; }
        [ForeignKey("Province")]
        public string ProvinceID { get; set; }
        [ForeignKey("District")]
        public string? DistrictID { get; set; }
        [ForeignKey("Ward")]
        public string? WardID { get; set; }
        public JobPosition? JobPosition { get; set; }
        public Province? Province { get; set; }
        public District? District { get; set; }
        public Ward? Ward { get; set; }

    }
}
