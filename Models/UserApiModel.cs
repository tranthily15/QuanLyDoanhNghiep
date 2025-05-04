namespace QuanLyDoanhNghiep.Models
{
    public class UserApiModel
    {
        public string ID { get; set; }
        public string FullName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool Gender { get; set; }
        public DateTime? GraduationYear { get; set; }
        public float? GPA { get; set; }
        public int? Honors { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public int? AccountID { get; set; }
        public string UserName { get; set; }  // Dữ liệu có thể chứa UserName trong API
        public string Password { get; set; }  // Dữ liệu có thể chứa Password trong API
    }
}

