using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class Notification
    {
        [Key]
        public int NotificationID { get; set; }

       

        [Required]
        [StringLength(500)]
        public string Message { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        public bool IsRead { get; set; }

        public string? NotificationPath { get; set; }
        [Required]
        public string UserRole { get; set; } // "1" for employee, "2" for student, "0" for admin

        public string? UserID { get; set; } // For student notifications
        public string? EmployeeID { get; set; } // For employee notifications

        [ForeignKey("UserID")]
        public virtual User? User { get; set; }

        [ForeignKey("EmployeeID")]
        public virtual Employee? Employee { get; set; }
        
    }
} 