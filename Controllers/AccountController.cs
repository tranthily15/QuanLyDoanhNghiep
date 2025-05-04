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
                0 => RedirectToAction("Index", "Dashboard"), // Admin
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
                //if (!IsLogin)
                //{
                //    // Nếu người dùng có quyền là 2, chỉ tạo tài khoản có quyền là
                //    if (model.Quyen != 2)
                //    {
                //        ModelState.AddModelError("", "Bạn chỉ được phép tạo tại khoản cho khách hàng");
                //        return View(model);
                //    }
                //}
                //else if (RoleUser == "1")
                //{
                //    // Nếu người dùng có quyền là 1, có thể tạo tài khoản có quyền là 1 hoặc 2
                //    if (model.Role != 1 && model.Role != 2)
                //    {
                //        ModelState.AddModelError("", "Bạn không có quyền được tài khoản admin");
                //        return View(model);
                //    }
                //}

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

        //// GET: Account/Create
        //public IActionResult Create()
        //{
        //    return View();
        //}
        
        //// POST: Account/Create
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("AccountID,UserName,Password,Role")] Account account)
        //{
        //    if (ModelState.IsValid)
        //    {
        //        _context.Add(account);
        //        await _context.SaveChangesAsync();
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(account);
        //}

        //// GET: Account/Edit/5
        //public async Task<IActionResult> Edit(int? id)
        //{
        //    if (id == null)
        //    {
        //        return NotFound();
        //    }
        //    var account = await _context.Account.FindAsync(id);
        //    if (account == null)
        //    {
        //        return NotFound();
        //    }
        //    return View(account);
        //} 

        //// POST: Account/Edit/5
        //// To protect from overposting attacks, enable the specific properties you want to bind to.
        //// For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        //public async Task<IActionResult> Edit(int id, [Bind("AccountID,UserName,Password,Role")] Account account)
        //{
        //    if (id != account.AccountID)
        //    {
        //        return NotFound();
        //    }

        //    if (ModelState.IsValid)
        //    {
        //        try
        //        {
        //            _context.Update(account);
        //            await _context.SaveChangesAsync();
        //        }
        //        catch (DbUpdateConcurrencyException)
        //        {
        //            if (!AccountExists(account.AccountID))
        //            {
        //                return NotFound();
        //            }
        //            else
        //            {
        //                throw;
        //            }
        //        }
        //        return RedirectToAction(nameof(Index));
        //    }
        //    return View(account);
        //}

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
