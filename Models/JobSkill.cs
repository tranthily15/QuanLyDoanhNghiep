using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class JobSkill
    {
        [Key]
        public int JobSkillID { get; set; }

        [Required]
        public int JobPositionID { get; set; }

        [Required]
        public int SkillID { get; set; }

        [Required]
        [Range(1, 5)]
        public int RequiredLevel { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Navigation properties
        [ForeignKey("JobPositionID")]
        public virtual JobPosition JobPosition { get; set; }

        [ForeignKey("SkillID")]
        public virtual Skill Skill { get; set; }
    }
} 