﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuanLyDoanhNghiep.Models;
using QuanLyDoanhNghiep.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace QuanLyDoanhNghiep.Controllers
{
    public class DashboardController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;
        private readonly IServiceScopeFactory _serviceScopeFactory;


        public DashboardController(QuanLyDoanhNghiepDBContext context, IServiceScopeFactory serviceScopeFactory)
        {
            _context = context;
            _serviceScopeFactory = serviceScopeFactory;
        }
        public IActionResult Index()
        {
            if (!IsLogin || RoleUser == "2" || RoleUser == "1")
            {
                return RedirectToAction("Login", "Account");
            }
            ViewData["Title"] = "Trang chủ Admin";

            // Thống kê CV
            var cvList = _context.CV.ToList();
            var summaryCV = new Dictionary<string, int>
            {
                { "TotalCVCount", cvList.Count },
                { "StatusZeroCount", cvList.Count(c => c.Status == 0) },
                { "StatusOneCount", cvList.Count(c => c.Status == 1) },
                { "StatusTwoCount", cvList.Count(c => c.Status == 2) }
            };
            ViewBag.cv = summaryCV;

            // Thống kê JobPosition
            var jobList = _context.JobPosition.ToList();
            var today = DateTime.Today;
            var summaryJob = new Dictionary<string, int>
            {
                { "TotalJobCount", jobList.Count },
                { "ActiveJobCount", jobList.Count(j => j.Status == true) },
                { "NewJobsToday", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date.Equals(today)) }
            };
            ViewBag.job = summaryJob;

            // Thống kê User
            var userList = _context.User
            .Include(u => u.Account)
            .ToList();
            var employeeList = _context.Employee
            .Include(e => e.Account)
            .ToList();

            var summaryUser = new Dictionary<string, int>
            {
                { "TotalUserCount", userList.Count+employeeList.Count },
                { "StudentCount", userList.Count(u => u.Account.Role == 2) },
                { "EmployeeCount", employeeList.Count(u => u.Account.Role == 1) }
            };
            ViewBag.user = summaryUser;

            // Thống kê Company
            var companyList = _context.Company
                .Include(c => c.Employees)
                .Include(c => c.JobPositions)
                .ToList();

            var summaryCompany = new Dictionary<string, int>
            {
                { "TotalCompanyCount", companyList.Count },
                { "ActiveCompanyCount", companyList.Count(c => c.Status == 1) }, // công ty đang hoạt động
                { "NewCompaniesToday", companyList.Count(c => c.CreatedAt.HasValue && c.CreatedAt.Value.Date.Equals(today)) },
                { "CompaniesWithJobs", companyList.Count(c => c.JobPositions.Any()) },
                { "CompaniesWithEmployees", companyList.Count(c => c.Employees.Any()) }
            };
            ViewBag.company = summaryCompany;

            // Thống kê chi tiết về công ty
            var companyDetails = companyList.Select(c => new
            {
                CompanyID = c.CompanyID,
                CompanyName = c.CompanyName,
                EmployeeCount = c.Employees.Count,
                ActiveJobCount = c.JobPositions.Count(j => j.Status == true),
                TotalJobCount = c.JobPositions.Count,
                TotalCVCount = _context.CV.Count(cv => cv.JobPosition.CompanyID == c.CompanyID),
            })
            .OrderByDescending(c => c.TotalCVCount)
            .ThenByDescending(c => c.TotalJobCount)
            .ThenByDescending(c => c.EmployeeCount)
            .Take(5)
            .ToList();
            ViewBag.topCompanies = companyDetails;

            // Thống kê theo thời gian
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);

            var timeBasedStats = new Dictionary<string, int>
            {
                { "CVsToday", cvList.Count(c => c.SubmissionDate?.Date.Equals(today) == true) },
                { "CVsThisWeek", cvList.Count(c => c.SubmissionDate >= thisWeekStart) },
                { "CVsThisMonth", cvList.Count(c => c.SubmissionDate >= thisMonthStart) },
                { "JobsPostedThisWeek", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date >= thisWeekStart) },
                { "JobsPostedThisMonth", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date >= thisMonthStart) }
            };
            ViewBag.timeStats = timeBasedStats;

            // Thống kê top vị trí có nhiều ứng viên nhất
            var topJobs = jobList
                .Select(j => new
                {
                    PositionName = j.PositionName,
                    Vacancies = j.Vacancies,
                    EndDate = j.EndDate,
                    Applicants = cvList.Count(c => c.PositionID == j.PositionID)
                })
                .OrderByDescending(j => j.Applicants)
                .Take(5)
                .ToList();
            ViewBag.TopJobs = topJobs;

            return View();
        }
        public async Task<IActionResult> Index_User(string searchString, int page = 1)
        {
            if (!IsLogin || RoleUser == "2" || RoleUser == "1")
            {
                return RedirectToAction("Login", "Account");
            }
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var syncService = scope.ServiceProvider.GetRequiredService<SyncDataService>();
                await syncService.SyncDataAsync();
            }

            int pageSize = 30;
            var users = _context.User
            .Include(u => u.CVs)
            .Include(u => u.Account)
            .AsQueryable();

            // // Lọc theo khóa
            // if (!string.IsNullOrEmpty(khoaFilter))
            // {
            //     users = users.Where(u => u.Khoa == khoaFilter);
            // }

            // Tìm kiếm theo tên, số điện thoại, mã sinh viên, email
            if (!string.IsNullOrEmpty(searchString))
            {
                users = users.Where(u =>
                    u.FullName.Contains(searchString) ||
                    u.PhoneNumber.Contains(searchString) ||
                    u.ID.ToString().Contains(searchString) ||
                    u.Khoa.Contains(searchString) ||
                    u.Email.Contains(searchString)
                );
            }

            // Lấy danh sách các khóa để hiển thị dropdown
            ViewBag.SearchString = searchString;

            int totalUsers = await users.CountAsync();
            int totalPages = (int)Math.Ceiling(totalUsers / (double)pageSize);

            var usersPaged = await users
                .OrderBy(u => u.Khoa)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(usersPaged);
        }

        public async Task<IActionResult> Index_Employee()
        {
            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            var companyId = int.Parse(CurrentCompanyID);

            // Thống kê CV cho công ty
            var cvList = await _context.CV
                .Include(c => c.JobPosition)
                .Where(c => c.JobPosition.CompanyID == companyId)
                .ToListAsync();

            var summaryCV = new Dictionary<string, int>
            {
                { "TotalCVCount", cvList.Count },
                { "PendingCount", cvList.Count(c => c.Status == 0) },
                { "AcceptedCount", cvList.Count(c => c.Status == 1) },
                { "RejectedCount", cvList.Count(c => c.Status == 2) },
                { "InternCount", cvList.Count(c => !c.JobPosition.PositionType) },
                { "EmployeeCount", cvList.Count(c => c.JobPosition.PositionType) }
            };
            ViewBag.cv = summaryCV;

            // Thống kê JobPosition của công ty
            var jobList = await _context.JobPosition
                .Where(j => j.CompanyID == companyId)
                .ToListAsync();

            var summaryJob = new Dictionary<string, int>
            {
                { "TotalJobCount", jobList.Count },
                { "ActiveJobCount", jobList.Count(j => j.Status == true) },
                { "InactiveJobCount", jobList.Count(j => j.Status == false) },
                { "NewJobsToday", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date == DateTime.Today) },
                { "InternPositions", jobList.Count(j => !j.PositionType) },
                { "EmployeePositions", jobList.Count(j => j.PositionType) },
                { "ExpiredJobs", jobList.Count(j => j.EndDate < DateTime.Today) }
            };
            ViewBag.job = summaryJob;
            // Lấy top 5 vị trí có nhiều ứng viên nhất
            var topJobPositions = jobList
                .Select(j => new
                {
                    PositionName = j.PositionName,
                    Vacancies = j.Vacancies,
                    EndDate = j.EndDate,
                    ApplicantsCount = j.ApplicantsCount
                })
                .OrderByDescending(j => j.ApplicantsCount)
                .Take(5)
                .ToList();
            ViewBag.TopJobPositions = topJobPositions;
            // Thống kê theo thời gian
            var today = DateTime.Today;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);

            var timeBasedStats = new Dictionary<string, int>
            {
                { "CVsToday", cvList.Count(c => c.SubmissionDate?.Date == today) },
                { "CVsThisWeek", cvList.Count(c => c.SubmissionDate >= thisWeekStart) },
                { "CVsThisMonth", cvList.Count(c => c.SubmissionDate >= thisMonthStart) },
                { "JobsPostedThisWeek", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date >= thisWeekStart) },
                { "JobsPostedThisMonth", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date >= thisMonthStart) }
            };
            ViewBag.timeStats = timeBasedStats;

            // --- Logic for CV Trend Chart (Last 28 Days) ---
            var startDate = today.AddDays(-27); // Start date for the 28-day period
            var dateRange = Enumerable.Range(0, 28).Select(i => startDate.AddDays(i)).ToList();

            // Query CVs submitted within the last 28 days for the specific company
            var recentCVs = _context.CV
                .Where(cv => cv.JobPosition.CompanyID == companyId && // Filter by company
                             cv.SubmissionDate.HasValue && // Ensure SubmissionDate is not null
                             cv.SubmissionDate.Value >= startDate &&
                             cv.SubmissionDate.Value <= today)
                .ToList();

            // Group CVs by submission date and count them
            var cvCountsByDay = recentCVs
                .GroupBy(cv => cv.SubmissionDate.Value.Date) // Now safe to access .Value.Date
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .ToDictionary(x => x.Date, x => x.Count);

            // Prepare data for the chart (ensure all 28 days are present, even with 0 count)
            var cvTrendLabels = dateRange.Select(d => d.ToString("dd/MM")).ToList(); // Format dates as "dd/MM"
            var cvTrendData = dateRange.Select(d => cvCountsByDay.ContainsKey(d) ? cvCountsByDay[d] : 0).ToList();

            ViewBag.CVTrendLabels = cvTrendLabels;
            ViewBag.CVTrendData = cvTrendData;

            // --- End of CV Trend Chart Logic ---


            return View();
        }

        public async Task<IActionResult> Index_Student()
        {
            if (!IsLogin || RoleUser != "2")
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.User
                 .FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));

            // Thống kê CV của sinh viên
            var cvList = await _context.CV
                .Include(c => c.JobPosition)
                .Where(c => c.ID.Equals(user.ID))
                .ToListAsync();

            var summaryCV = new Dictionary<string, int>
            {
                { "TotalCVCount", cvList.Count },
                { "PendingCount", cvList.Count(c => c.Status == 0) },
                { "AcceptedCount", cvList.Count(c => c.Status == 1) },
                { "RejectedCount", cvList.Count(c => c.Status == 2) },
                { "InternApplications", cvList.Count(c => c.JobPosition != null && !c.JobPosition.PositionType) },
                { "EmployeeApplications", cvList.Count(c => c.JobPosition != null && c.JobPosition.PositionType) }
            };
            ViewBag.cv = summaryCV;

            // Thống kê JobPosition mới và đang mở
            var jobList = await _context.JobPosition
                .Where(j => j.Status == true && j.EndDate >= DateTime.Today)
                .ToListAsync();

            var summaryJob = new Dictionary<string, int>
            {
                { "TotalActiveJobs", jobList.Count },
                { "NewJobsToday", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date == DateTime.Today) },
                { "InternJobs", jobList.Count(j => !j.PositionType) },
                { "EmployeeJobs", jobList.Count(j => j.PositionType) },
                { "JobsClosingSoon", jobList.Count(j => j.EndDate <= DateTime.Today.AddDays(7)) }
            };
            ViewBag.job = summaryJob;

            // Thống kê theo thời gian
            var today = DateTime.Today;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);

            var timeBasedStats = new Dictionary<string, int>
            {
                { "CVsSubmittedToday", cvList.Count(c => c.SubmissionDate?.Date == today) },
                { "CVsSubmittedThisWeek", cvList.Count(c => c.SubmissionDate >= thisWeekStart) },
                { "CVsSubmittedThisMonth", cvList.Count(c => c.SubmissionDate >= thisMonthStart) },
                { "NewJobsThisWeek", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date >= thisWeekStart) },
                { "NewJobsThisMonth", jobList.Count(j => j.CreatedAt.HasValue && j.CreatedAt.Value.Date >= thisMonthStart) }
            };
            ViewBag.timeStats = timeBasedStats;

            ViewBag.Student = user;

            return View();
        }
    }
}
