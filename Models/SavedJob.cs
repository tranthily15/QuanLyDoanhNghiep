using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class SavedJob
    {
        [Key]
        public int SavedJobID { get; set; }

        [Required]
        public string UserID { get; set; }

        [Required]
        public int PositionID { get; set; }

        [Required]
        public DateTime SavedDate { get; set; }

        [Required]
        public bool IsSaved { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; }

        [ForeignKey("PositionID")]
        public virtual JobPosition JobPosition { get; set; }
    }
} 