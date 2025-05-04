using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class CVSkill
    {
        [Key]
        public int CVSkillID { get; set; }

        [Required]
        public int CVID { get; set; }

        [Required]
        public int SkillID { get; set; }

        [Required]
        [Range(1, 5)]
        public int SkillLevel { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Column(TypeName = "nvarchar(100)")]
        public string SkillName { get; set; }

        [Column(TypeName = "nvarchar(50)")]
        public string SkillCategory { get; set; }

        public double? ConfidenceScore { get; set; }

        public bool IsVerified { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        [ForeignKey("CVID")]
        public virtual CV CV { get; set; }

        [ForeignKey("SkillID")]
        public virtual Skill Skill { get; set; }

        // Helper methods
        public string GetSkillLevelText()
        {
            return SkillLevel switch
            {
                1 => "Cơ bản",
                2 => "Trung bình",
                3 => "Khá",
                4 => "Giỏi",
                5 => "Chuyên gia",
                _ => "Không xác định"
            };
        }

        public string GetSkillLevelClass()
        {
            return SkillLevel switch
            {
                1 => "bg-secondary",
                2 => "bg-info",
                3 => "bg-primary",
                4 => "bg-success",
                5 => "bg-warning",
                _ => "bg-secondary"
            };
        }
    }
} 