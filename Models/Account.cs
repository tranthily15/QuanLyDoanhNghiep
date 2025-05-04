using System.ComponentModel.DataAnnotations;

namespace QuanLyDoanhNghiep.Models
{
    public class Account
    {
        [Key]
        public int AccountID { get; set; }
        public string UserName {  set; get; }
        public string Password { get; set; }
        public int Role {  get; set; }
    }
}
