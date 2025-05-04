using System.Collections.Generic;

namespace QuanLyDoanhNghiep.Models
{
    public class JobMatchResult
    {
        public JobPosition JobPosition { get; set; }
        public double MatchPercentage { get; set; }
        public List<string> MatchingSkills { get; set; }
        public List<string> MissingSkills { get; set; }
        public string CompanyName => JobPosition?.Company?.CompanyName;
        public string PositionName => JobPosition?.PositionName;
        //public string Location => JobPosition?.Location;
        public string? Salary => JobPosition?.Salary;
        public string JobDescription => JobPosition?.JobDescription;
    }
} 