using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace QuanLyDoanhNghiep.Models
{
    public class SuggestedJobHistory
    {
        [Key]
        public int Id { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; } 
        public string CVFilePath { get; set; }
        public DateTime SuggestedAt { get; set; }
        public string JobPositionIds { get; set; } 
        public string CVSectionsJson { get; set; } 
        public User? User { get; set; } // Tham chiếu đến người dùng đã gợi ý công việc
    }
}