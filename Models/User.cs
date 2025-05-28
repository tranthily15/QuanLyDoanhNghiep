using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class User
    {
        [Key]
        public string ID { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool Gender { get; set; }
        public DateTime? GraduationYear { get; set; }
        public float? GPA { get; set; }
        public int? Honors { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string? Khoa { get; set; }

        [ForeignKey("Account")]
        public int AccountID { get; set; }
        public Account? Account { get; set; }
        public string? avt { get; set; } = "/img/avt/default-avatar.png";

        public virtual ICollection<SavedJob> SavedJobs { get; set; }
    }
}
