using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuanLyDoanhNghiep.Models;
using QuanLyDoanhNghiep.Services;


namespace QuanLyDoanhNghiep.Controllers
{
    public class AccountController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;
        private readonly IEmailService _emailService;

        public AccountController(QuanLyDoanhNghiepDBContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        // Ví dụ: Lấy danh sách tất cả các account
        public async Task<IActionResult> Index()
        {
            var accounts = await _context.Account.ToListAsync();
            return View(accounts);
        }

        // GET: Account/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Account
                .FirstOrDefaultAsync(m => m.AccountID == id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("UserName, Password")] Account model)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra tên tài khoản
            var loginUser = await _context.Account
                .FirstOrDefaultAsync(m => m.UserName == model.UserName);

            if (loginUser == null)
            {
                ModelState.AddModelError("", "Tên đăng nhập không đúng");
                return View(model);
            }
            // kiểm tra xem người dùng có là nhân viên bị đóng trạng thái không 
            var employee = await _context.Employee
                .FirstOrDefaultAsync(m => m.Account.UserName == model.UserName && m.Status == false);
            if (employee != null)
            {
                ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên.");
                return View(model);
            }

            // Kiểm tra mật khẩu
            if (!VerifyPassword(loginUser.Password, model.Password))
            {
                ModelState.AddModelError("", "Mật khẩu không đúng");
                return View(model);
            }

            // Lưu trạng thái user
            SetCurrentUser(loginUser);

            // Chuyển hướng theo vai trò
            return loginUser.Role switch
            {
                0 => RedirectToAction("Index", "DashBoard"), // Admin
                1 => await RedirectToEmployeeJobPosition(loginUser.AccountID), // Employee
                _ => RedirectToAction("Index", "JobPosition") // User
            };
        }

        private bool VerifyPassword(string storedPassword, string inputPassword)
        {
            using var hashMethod = SHA256.Create();
            return Util.Cryptography.VerifyHash(hashMethod, inputPassword, storedPassword);
        }

        private void SetCurrentUser(Account user)
        {
            CurrentUser = user.UserName;
            RoleUser = user.Role.ToString();
            CurrentID = user.AccountID.ToString();
        }

        private async Task<IActionResult> RedirectToEmployeeJobPosition(int accountId)
        {
            var employee = await _context.Employee.FirstOrDefaultAsync(m => m.AccountID == accountId);
            if (employee == null)
                return NotFound();

            CurrentCompanyID = employee.CompanyID.ToString();
            return RedirectToAction("Index", "JobPosition");
        }

        public IActionResult Create_Account_Employee()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Create_Account_Employee([FromBody] Account account)
        {
            if (ModelState.IsValid)
            {
                var existingAccount = _context.Account.FirstOrDefault(m => m.UserName.Equals(account.UserName));
                if (existingAccount != null)
                {
                    ModelState.AddModelError("", "Tên tài khoản đã tồn tại. Vui lòng chọn tên tài khoản khác.");
                    return BadRequest();
                }
                else
                {
                    account.Role = 1;
                    SHA256 hashMethod = SHA256.Create();
                    account.Password = Util.Cryptography.GetHash(hashMethod, account.Password);
                    _context.Account.Add(account);
                    await _context.SaveChangesAsync();

                    // Trả về MaTaiKhoan của tài khoản vừa tạo
                    return Json(new { accountID = account.AccountID });
                }

            }
            // Nếu có lỗi, trả về lỗi
            return BadRequest(ModelState);
        }
        public IActionResult Register()
        {
            //if (QuyenUser == "1" || !IsLogin)
            //{
            //    return RedirectToAction("Login", "TaiKhoan");
            //}
            //else
            //{
            return View();
            //}
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind(" UserName, Password, Role")] Account model)
        {
            if (ModelState.IsValid)
            {


                var existingAccount = _context.Account.FirstOrDefault(m => m.UserName.Equals(model.UserName));
                if (existingAccount != null)
                {
                    ModelState.AddModelError("", "Tên tài khoản đã tồn tại. Vui lòng chọn tên tài khoản khác.");
                    return View(model);
                }

                //Mã hóa mật khẩu 
                SHA256 hashMethod = SHA256.Create();
                model.Password = Util.Cryptography.GetHash(hashMethod, model.Password);
                _context.Add(model);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Login));
            }
            return View(model);
        }
        public IActionResult Logout()
        {
            CurrentUser = "";
            CurrentID = "";
            CurrentCompanyID = "";
            RoleUser = "";
            return RedirectToAction("Login");
        }

        public IActionResult ChangePassword()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới và xác nhận mật khẩu không khớp.");
                return View();
            }

            var account = await _context.Account.FirstOrDefaultAsync(a => a.AccountID.ToString() == CurrentID);
            if (account == null)
            {
                return NotFound();
            }

            if (!VerifyPassword(account.Password, currentPassword))
            {
                ModelState.AddModelError("", "Mật khẩu hiện tại không đúng.");
                return View();
            }

            // Mã hóa mật khẩu mới
            using var hashMethod = SHA256.Create();
            account.Password = Util.Cryptography.GetHash(hashMethod, newPassword);

            _context.Update(account);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Login", "Account");

        }



        // GET: Account/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var account = await _context.Account
                .FirstOrDefaultAsync(m => m.AccountID == id);
            if (account == null)
            {
                return NotFound();
            }

            return View(account);
        }

        // POST: Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var account = await _context.Account.FindAsync(id);
            if (account != null)
            {
                _context.Account.Remove(account);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool AccountExists(int id)
        {
            return _context.Account.Any(e => e.AccountID == id);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string username, string email)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Message = "Vui lòng nhập đầy đủ tên tài khoản và email.";
                return View();
            }

            var account = await _context.Account.FirstOrDefaultAsync(a => a.UserName == username);
            if (account == null)
            {
                ViewBag.Message = "Tên tài khoản không tồn tại trong hệ thống!";
                return View();
            }

            // if (!string.Equals(account.Email?.Trim(), email.Trim(), StringComparison.OrdinalIgnoreCase))
            // {
            //     ViewBag.Message = "Email không khớp với tài khoản đã đăng ký!";
            //     return View();
            // }

            // Tạo mật khẩu mới ngẫu nhiên
            var newPassword = Guid.NewGuid().ToString("N").Substring(0, 8);

            // Mã hóa mật khẩu mới
            using var hashMethod = System.Security.Cryptography.SHA256.Create();
            account.Password = Util.Cryptography.GetHash(hashMethod, newPassword);
            _context.Update(account);
            await _context.SaveChangesAsync();

            // Gửi email với định dạng HTML đẹp
            var emailSubject = "Mật khẩu mới từ FitJob";
            var emailBody = $@"
    <h2>Xin chào {account.UserName},</h2>
    <p>Bạn vừa yêu cầu cấp lại mật khẩu trên hệ thống FitJob.</p>
    <ul>
        <li><b>Tên đăng nhập:</b> {account.UserName}</li>
        <li><b>Email nhận mật khẩu:</b> {email}</li>
        <li><b>Mật khẩu mới:</b> <span style='color:#f57c00;font-weight:bold'>{newPassword}</span></li>
    </ul>
    <p>Vui lòng đăng nhập bằng mật khẩu mới này và đổi lại mật khẩu sau khi đăng nhập.</p>
    <p style='color:#888;font-size:13px'>Nếu bạn không yêu cầu chức năng này, hãy bỏ qua email này.</p>
    <p>Trân trọng,<br>Ban quản trị hệ thống FitJob</p>";

            try
            {
                await _emailService.SendEmailAsync(email, emailSubject, emailBody);
                ViewBag.Message = "Mật khẩu mới đã được gửi về email của bạn. Vui lòng kiểm tra hộp thư.";
            }
            catch
            {
                ViewBag.Message = "Gửi email thất bại. Vui lòng thử lại sau!";
            }
            return View();
        }
    }
}
