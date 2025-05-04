using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class CV
    {
        [Key]
        public int CV_ID { get; set; }
        [ForeignKey("User")]
        public string ID {  get; set; }
        [ForeignKey("JobPosition")]
        public int PositionID {  get; set; }
    
        public string? Skills {  get; set; } // kỹ năng của ứng viên
        public string Resume {  get; set; }
        public int Status { get; set; }
        public DateTime? SubmissionDate { get; set; } // ngày nộp CV    
        public DateTime? EvaluationDate { get; set; } // ngày đánh giá CV
        public string? Feedback { get; set; } // ghi chú đánh giá CV
        public User? User { get; set; }
        public JobPosition? JobPosition { get; set; }
      

        // Thuộc tính chỉ đọc tính toán trực tiếp
        [NotMapped]
        public string StatusDescription
        {
            get
            {
                return Status switch
                {
                    0 => "Chờ xử lý",
                    1 => "Đã duyệt",
                    2 => "Từ chối",
                    _ => "Không xác định",
                };
            }
        }
    }
}
