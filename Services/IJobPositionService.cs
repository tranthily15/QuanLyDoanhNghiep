using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Services
{
    public interface IJobPositionService
    {
        Task<IEnumerable<JobPosition>> GetAllJobPositions();
        Task<JobPosition> GetJobPositionById(int id);
        Task<bool> CreateJobPosition(JobPosition jobPosition);
        Task<bool> UpdateJobPosition(JobPosition jobPosition);
        Task<bool> DeleteJobPosition(int id);
    }
} 