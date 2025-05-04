using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;
using System.Linq;

namespace QuanLyDoanhNghiep.Controllers
{
    public class StatisticsController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public StatisticsController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
        }

        [Authorize(Roles = "0")]
        public async Task<IActionResult> AdminStatistics()
        {
            var newCvCount = await _context.CV.CountAsync(cv => cv.Status == 0);
            ViewBag.NewCvCount = newCvCount;
            // TODO: Thêm các thống kê khác cho Admin nếu cần
            return View();
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> EmployeeStatistics()
        {
            if (int.TryParse(CurrentCompanyID, out int companyId))
            {
                var newCvCountForCompany = await _context.CV
                    .Include(cv => cv.JobPosition)
                    .Where(cv => cv.JobPosition.CompanyID == companyId && cv.Status == 0)
                    .CountAsync();
                ViewBag.NewCvCount = newCvCountForCompany;

                var jobPositions = await _context.JobPosition
                                        .Where(jp => jp.CompanyID == companyId)
                                        .ToListAsync();
                var statistics = new
                {
                    TotalPositions = jobPositions.Count(),
                    ActivePositions = jobPositions.Count(x => x.Status),
                    ExpiredPositions = jobPositions.Count(x => !x.Status)
                };
                return View(statistics);
            }
            else
            {
                ViewBag.ErrorMessage = "Không thể xác định công ty.";
                return View();
            }
        }

        [Authorize(Roles = "2")]
        public async Task<IActionResult> StudentStatistics()
        {
            if (!string.IsNullOrEmpty(CurrentID) && int.TryParse(CurrentID, out int accountId))
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID == accountId);
                if (user != null)
                {
                    var newCvCountForStudent = await _context.CV
                        .Where(cv => cv.ID == user.ID && cv.Status == 0)
                        .CountAsync();
                    ViewBag.NewCvCount = newCvCountForStudent;
                }
                else
                {
                    ViewBag.ErrorMessage = "Không tìm thấy thông tin người dùng.";
                }
            }
            else
            {
                 ViewBag.ErrorMessage = "Không thể xác định người dùng.";
            }
            // TODO: Implement student statistics logic other than new CV count
            return View();
        }
    }
} 