using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class Skill
    {
        [Key]
        public int SkillID { get; set; }

        [Required]
        [StringLength(100)]
        public string SkillName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        // Navigation properties
        public virtual ICollection<JobSkill> JobSkills { get; set; }
        public virtual ICollection<CVSkill> CVSkills { get; set; }
    }
} 