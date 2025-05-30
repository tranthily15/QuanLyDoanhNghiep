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

namespace QuanLyDoanhNghiep.Controllers
{
    public class AccountController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public AccountController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
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
                0 => RedirectToAction("Index", "JobPosition"), // Admin
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
    }
}
