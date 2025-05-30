using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class JobPosition
    {
        [Key]
        public int PositionID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên vị trí")]
        public string PositionName { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại vị trí")]
        public bool PositionType { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tuyển")]
        public string Vacancies { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mô tả công việc")]
        public string JobDescription { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập yêu cầu công việc")]
        public string JobRequirements { get; set; }

        public string? Salary { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập quyền lợi")]
        public string JobBenefits { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc")]
        public DateTime EndDate { get; set; }
        public DateTime? CreatedAt { get; set; } = DateTime.Now;


        [Required(ErrorMessage = "Vui lòng nhập thời gian làm việc")]
        public string TimeWork { get; set; }

        [ForeignKey("Company")]
        public int CompanyID { get; set; }

        public bool Status { get; set; }

        [ForeignKey("ITPositionCategory")]
        public int? CategoryID { get; set; }

        public Company? Company { get; set; }
        public ITPositionCategory? ITPositionCategory { get; set; }

        public List<JobLocation> JobLocations { get; set; } = new List<JobLocation>();
        public virtual ICollection<CV>? CVs { get; set; }

        [NotMapped]
        public int ApplicantsCount
        {
            get
            {
                return CVs?.Count ?? 0;
            }
        }

    }
}
