using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;
using QuanLyDoanhNghiep.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace QuanLyDoanhNghiep.Controllers
{
    public class CompanyController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;
        private readonly IEmailService _emailService;

        public CompanyController(QuanLyDoanhNghiepDBContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // GET: Company
        public async Task<IActionResult> Index()
        {
            ViewBag.Provinces = _context.Province.ToList();
            return View(await _context.Company
                .Include(c => c.Ward)
                .Include(c => c.District)
                .Include(c => c.Province)
                .ToListAsync());
        }

        // GET: Company/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var company = await _context.Company
                .Include(j => j.Province)
                .Include(c => c.Ward)
                .Include(c => c.District)
                .Include(c => c.Province)
                .FirstOrDefaultAsync(m => m.CompanyID == id);
            if (company == null)
            {
                return NotFound();
            }
            var companyJobs = await _context.JobPosition
                .Include(j => j.JobLocations)
                    .ThenInclude(l => l.Province)
                .Where(j => j.CompanyID == id && j.Status == true && j.EndDate >= DateTime.Now)
                .ToListAsync();
            ViewBag.CompanyJobs = companyJobs;
            return View(company);
        }

        // GET: Company/Create
        public IActionResult Create()
        {
            ViewBag.Provinces = new SelectList(_context.Province, "ProvinceID", "ProvinceName");
            return View();
        }

        // POST: Company/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Company company, IFormFile companyLogoFile)
        {
            company.Status = false;
            company.CompanyLogo = "default.png";
            //if(!ModelState.IsValid){
            //    foreach(var error in ModelState.Values.SelectMany(v => v.Errors)){
            //        Console.WriteLine(error.ErrorMessage);
            //    }
            //    return View(company);
            //}    
            if (ModelState.IsValid)
            {
                if (companyLogoFile != null && companyLogoFile.Length > 0)
                {
                    // Lưu file CV vào thư mục trên server
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "img", "CompanyLogo");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    string fileName = companyLogoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, fileName);
                    int counter = 1;

                    // Kiểm tra nếu file đã tồn tại thì thêm số đếm vào tên file
                    while (System.IO.File.Exists(filePath))
                    {
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                        string extension = Path.GetExtension(fileName);
                        fileName = $"{fileNameWithoutExtension}({counter}){extension}";
                        filePath = Path.Combine(uploadsFolder, fileName);
                        counter++;
                    }

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await companyLogoFile.CopyToAsync(stream);
                    }
                    company.CompanyLogo = companyLogoFile.FileName;

                   
                }
               
                // Lưu thông tin công ty vào CSDL
                _context.Add(company);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Bạn đã đăng ký thông tin doanh nghiệp thành công. Hãy đợi 1-2 ngày để được duyệt.";
                var notification = new Notification
                {
                    Message = $"Có doanh nghiệp mới:  {company.CompanyName}",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    UserRole = "0", // Dành cho admin
                    //EmployeeID = e.EmployeeID,
                    NotificationPath = $"/Company/Details/{company.CompanyID}"
                };
                _context.Add(notification);
                await _context.SaveChangesAsync();

                return RedirectToAction("Index", "JobPosition");
            }

            ViewBag.Provinces = new SelectList(_context.Province, "ProvinceID", "ProvinceName");
            return View(company);
        }

        // GET: Company/Approve/5
        public async Task<IActionResult> Approve(int? id)
        {
            if (!IsLogin || RoleUser == "1" || RoleUser == "2")
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                if (id == null)
                {
                    return NotFound();
                }

                var company = await _context.Company.FindAsync(id);
                if (company == null)
                {
                    return NotFound();
                }
                return View(company);
            }
        }

        // POST: Company/Approve/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var company = await _context.Company
                .Include(c => c.Ward)
                .Include(c => c.District)
                .Include(c => c.Province)
                .FirstOrDefaultAsync(c => c.CompanyID == id);

            if (company == null)
            {
                return NotFound();
            }

            company.Status = true;
            await _context.SaveChangesAsync();

            // Tạo thông báo cho công ty
            // var notification = new Notification
            // {
                
            //     Message = $"Công ty {company.CompanyName} của bạn đã được duyệt thành công. Bạn có thể đăng nhập và sử dụng hệ thống.",
            //     CreatedAt = DateTime.Now,
            //     IsRead = false,
            //     UserID = company.UserID
            // };
            // _context.Notifications.Add(notification);
            // await _context.SaveChangesAsync();

            // Gửi email thông báo
            var emailSubject = "Thông báo: Công ty của bạn đã được duyệt";
            var emailBody = $@"
                <h2>Xin chào {company.CompanyName},</h2>
                <p>Công ty của bạn đã được duyệt thành công. Dưới đây là thông tin chi tiết:</p>
                <ul>
                    <li>Mã công ty: {company.CompanyID}
                    <li>Tên công ty: {company.CompanyName}</li>
                    <li>Địa chỉ: {company.StreetAddress}, {company?.Ward?.WardName}, {company?.District?.DistrictName}, {company?.Province?.ProvinceName}</li>
                    <li>Email: {company.CompanyEmail}</li>
                </ul>
                <p>Bạn có thể đăng ký tài khoản với mã công ty là {company.CompanyID} và sử dụng.</p>
                <p>Trân trọng,<br>Ban quản trị hệ thống</p>";

            await _emailService.SendEmailAsync(company.CompanyEmail, emailSubject, emailBody);

            return RedirectToAction(nameof(Index));
        }

        // GET: Company/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Company
                .Include(c => c.Ward)
                .Include(c => c.District)
                .Include(c => c.Province)
                .FirstOrDefaultAsync(c => c.CompanyID == id);

            if (company == null)
            {
                return NotFound();
            }
            return View(company);
        }

        // POST: Company/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Company company)
        {
            if (id != company.CompanyID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(company);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CompanyExists(company.CompanyID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Provinces = new SelectList(_context.Province, "ProvinceID", "ProvinceName");
            return View(company);
        }

        // GET: Company/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var company = await _context.Company
                .Include(c => c.Ward)
                .Include(c => c.District)
                .Include(c => c.Province)
                .FirstOrDefaultAsync(m => m.CompanyID == id);

            if (company == null)
            {
                return NotFound();
            }

            return View(company);
        }

        // POST: Company/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var company = await _context.Company.FindAsync(id);
            if (company != null)
            {
                _context.Company.Remove(company);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CompanyExists(int id)
        {
            return _context.Company.Any(e => e.CompanyID == id);
        }

        public async Task<IActionResult> GetDistricts(string provinceId)
        {
            if (provinceId == null)
            {
                return Json(new { error = "Province ID is required" });
            }
            else
            {
                var districts = await _context.District
                    .Where(d => d.ProvinceID.Equals(provinceId))
                    .Select(d => new { d.DistrictID, d.DistrictName })
                    .ToListAsync(); 
                return Json(districts);

            }
                 
        }
        public async Task<IActionResult> GetWards(string districtId)
        {
            if (districtId == null)
            {
                return Json(new { error = "District ID is required" });
            }
            var wards = await _context.Ward
                .Where(w => w.DistrictID.Equals(districtId)) 
                .Select(w => new { w.WardID, w.WardName })              
                .ToListAsync();
            return Json(wards);
        }
    }
}
