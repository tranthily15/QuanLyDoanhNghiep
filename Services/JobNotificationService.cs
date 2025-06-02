using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Services
{
    public interface IJobNotificationService
    {
        Task CheckAndNotifyExpiredJobs();
    }

    public class JobNotificationService : IJobNotificationService
    {
        private readonly QuanLyDoanhNghiepDBContext _context;
        private readonly IEmailService _emailService;

        public JobNotificationService(QuanLyDoanhNghiepDBContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task CheckAndNotifyExpiredJobs()
        {
            var today = DateTime.Now.Date;
            var expiredJobs = await _context.JobPosition
                .Include(j => j.Company)
                .Include(j => j.JobLocations)
                .Where(j => j.EndDate.Date == today && j.Status)
                .ToListAsync();

            foreach (var job in expiredJobs)
            {
                var employee = await _context.Employee.Where(e => e.CompanyID == job.Company.CompanyID).ToListAsync();
                job.Status = false;
                _context.JobPosition.Update(job);
                foreach (var e in employee)
                {
                    // Tạo thông báo cho nhân viên công ty
                    var notification = new Notification
                    {
                        Message = $"Vị trí '{job.PositionName}' của công ty {job.Company.CompanyName} đã hết hạn tuyển dụng.",
                        CreatedAt = DateTime.Now,
                        UserRole = "1",
                        IsRead = false,
                        EmployeeID = e.EmployeeID, // Gửi cho tất cả nhân viên của công ty
                        NotificationPath = $"/JobPosition/Details/{job.PositionID}"
                    };
                    _context.Notification.Add(notification);
                }

                // Gửi email thông báo cho công ty
                // var emailSubject = "Thông báo: Vị trí tuyển dụng đã hết hạn";
                // var emailBody = $@"
                //     <h2>Xin chào {job.Company.CompanyName},</h2>
                //     <p>Vị trí tuyển dụng của bạn đã hết hạn. Dưới đây là thông tin chi tiết:</p>
                //     <ul>
                //         <li>Tên vị trí: {job.PositionName}</li>
                //         <li>Số lượng tuyển: {job.Vacancies}</li>
                //         <li>Ngày kết thúc: {job.EndDate:dd/MM/yyyy}</li>
                //         <li>Địa điểm làm việc: {string.Join(", ", job.JobLocations.Select(l => l.LocationName))}</li>
                //     </ul>
                //     <p>Bạn có thể đăng nhập vào hệ thống để xem chi tiết và thực hiện các thao tác cần thiết.</p>
                //     <p>Trân trọng,<br>Ban quản trị hệ thống</p>";

                // await _emailService.SendEmailAsync(job.Company.CompanyEmail, emailSubject, emailBody);
            }

            await _context.SaveChangesAsync();
        }
    }
} 