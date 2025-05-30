﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class Company
    {
        [Key]
        public int CompanyID { get; set; }
        public string CompanyName { get; set; }
        public string StreetAddress { get; set; }

        [ForeignKey("WardID")]
        public virtual Ward? Ward { get; set; }
        public string? WardID { get; set; }

        [ForeignKey("DistrictID")]
        public virtual District? District { get; set; }
        public string? DistrictID { get; set; }

        [ForeignKey("ProvinceID")]
        public virtual Province? Province { get; set; }
        public string? ProvinceID { get; set; }

        public string CompanyEmail { get; set; }
        public string? CompanyDescription { get; set; }
        public string? CompanyLogo { get; set; }
        public int? Status { get; set; } // 0: chưa duyệt, 1 đang hoạt động , 2 đã dừng hoạt động
        public DateTime? CreatedAt { get; set; } = DateTime.Now;

        public virtual ICollection<Employee>? Employees { get; set; }
        public virtual ICollection<JobPosition>? JobPositions { get; set; }
    }
}
