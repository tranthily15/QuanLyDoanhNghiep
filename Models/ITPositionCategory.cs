using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class ITPositionCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên nhóm vị trí")]
        [StringLength(100)]
        public string CategoryName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Navigation property
        public virtual ICollection<JobPosition> JobPositions { get; set; }
    }
}
