using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Models
{
    public class JobIndexViewModel
    {
        public List<JobPosition> Internships { get; set; }
        public List<JobPosition> FullTimeJobs { get; set; }
        public List<JobPosition> AllJobs { get; set; }

        public string Keyword { get; set; }
        public string[] SelectedProvinces { get; set; }
        public int TotalInternshipPages { get; set; }
        public int TotalFullTimePages { get; set; }
        public int InternshipPage { get; set; }
        public int FullTimePage { get; set; }

        public JobIndexViewModel()
        {
            Internships = new List<JobPosition>();
            FullTimeJobs = new List<JobPosition>();
            AllJobs = new List<JobPosition>();
            Keyword = string.Empty;
            SelectedProvinces = Array.Empty<string>();
        }
    }
}
