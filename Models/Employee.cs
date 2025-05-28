using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyDoanhNghiep.Models
{
    public class Employee
    {
        [Key]
        public string EmployeeID { get; set; }
        public string EmployeeName { get; set; }
        public bool Gender {  get; set; }
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public bool? Status { get; set; }
        [ForeignKey("Company")]
        public int CompanyID { get; set; }
        [ForeignKey("Account")]
        public int AccountID {  get; set; }  
        public Account? Account { get; set; }
        public Company? Company { get; set; }
        
    }
}
