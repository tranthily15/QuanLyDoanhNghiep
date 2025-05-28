using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Controllers
{
    public class EmployeeController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public EmployeeController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
        }

        // GET: Employee
        public async Task<IActionResult> Index()
        {
            var quanLyDoanhNghiepDBContext = _context.Employee.Include(e => e.Account).Include(e => e.Company);
            return View(await quanLyDoanhNghiepDBContext.ToListAsync());
        }

        // GET: Employee/CompanyEmployees
        public async Task<IActionResult> CompanyEmployees()
        {
            if (!IsLogin || RoleUser != "1") // Kiểm tra quyền nhân viên
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin công ty của nhân viên hiện tại
            var currentEmployee = await _context.Employee
                .FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));

            if (currentEmployee == null)
            {
                return NotFound();
            }

            // Lấy danh sách nhân viên của công ty
            var employees = await _context.Employee
                .Include(e => e.Account)
                .Include(e => e.Company)
                .Where(e => e.CompanyID == currentEmployee.CompanyID)
                .ToListAsync();

            ViewBag.CompanyId = currentEmployee.CompanyID;
            return View(employees);
        }

        // GET: Employee/Details/5
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee
                .Include(e => e.Account)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(m => m.EmployeeID == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // GET: Employee/Create
        public IActionResult Create()
        {
            if (!IsLogin || RoleUser != "1") // Kiểm tra quyền nhân viên
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy thông tin công ty của nhân viên hiện tại
            var currentEmployee = _context.Employee
                .FirstOrDefault(e => e.AccountID.ToString().Equals(CurrentID));

            if (currentEmployee == null)
            {
                return NotFound();
            }

            ViewBag.CompanyId = currentEmployee.CompanyID;
            return View();
        }

        // POST: Employee/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeID,EmployeeName,Gender,PhoneNumber,Email,CompanyID")] Employee employee, string username, string password)
        {
            if (!IsLogin || RoleUser != "1") // Kiểm tra quyền nhân viên
            {
                return RedirectToAction("Login", "Account");
            }

            if (ModelState.IsValid)
            {
                // Lấy thông tin công ty của nhân viên hiện tại
                var currentEmployee = await _context.Employee
                    .FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));

                if (currentEmployee == null)
                {
                    return NotFound();
                }

                // Kiểm tra username đã tồn tại chưa
                var existingAccount = await _context.Account
                    .FirstOrDefaultAsync(a => a.UserName == username);
                if (existingAccount != null)
                {
                    ModelState.AddModelError("", "Tên đăng nhập đã tồn tại. Vui lòng chọn tên khác.");
                    return View(employee);
                }

                Account account = null;
                try
                {
                    // Mã hóa mật khẩu
                    SHA256 hashMethod = SHA256.Create();
                    password = Util.Cryptography.GetHash(hashMethod, password);

                    // Tạo tài khoản mới
                    account = new Account
                    {
                        UserName = username,
                        Password = password,
                        Role = 1 // Role 1 là nhân viên
                    };
                    _context.Account.Add(account);
                    await _context.SaveChangesAsync(); // Lưu để lấy AccountID

                    // Tạo nhân viên mới
                    employee.EmployeeID = Guid.NewGuid().ToString();
                    employee.AccountID = account.AccountID; // Sử dụng AccountID đã được tạo
                    employee.CompanyID = currentEmployee.CompanyID;

                    _context.Add(employee);
                    await _context.SaveChangesAsync();

                    // Tạo thông báo cho tất cả nhân viên trong công ty
                    var companyEmployees = await _context.Employee
                        .Where(e => e.CompanyID == currentEmployee.CompanyID && e.EmployeeID != employee.EmployeeID)
                        .ToListAsync();

                    foreach (var emp in companyEmployees)
                    {
                        var notification = new Notification
                        {
                            Message = $"Nhân viên mới {employee.EmployeeName} đã được thêm vào công ty",
                            CreatedAt = DateTime.Now,
                            IsRead = false,
                            UserRole = "1", // Role 1 là nhân viên
                            EmployeeID = emp.EmployeeID,
                            NotificationPath = $"/Employee/Details/{employee.EmployeeID}"
                        };
                        _context.Notification.Add(notification);
                    }
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(CompanyEmployees));
                }
                catch (DbUpdateException ex)
                {
                    // Nếu có lỗi, xóa tài khoản đã tạo (nếu có)
                    if (account != null)
                    {
                        _context.Account.Remove(account);
                        await _context.SaveChangesAsync();
                    }
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu. Vui lòng kiểm tra lại thông tin.");
                }
                catch (Exception ex)
                {
                    // Nếu có lỗi, xóa tài khoản đã tạo (nếu có)
                    if (account != null)
                    {
                        _context.Account.Remove(account);
                        await _context.SaveChangesAsync();
                    }
                    ModelState.AddModelError("", "Có lỗi xảy ra khi tạo tài khoản. Vui lòng thử lại sau.");
                }
            }
            return View(employee);
        }

        [HttpPost]
        public async Task<IActionResult> Create1([FromBody] Employee employee)
        {
            if (ModelState.IsValid)
            {
                var existingEmployee = _context.Employee.FirstOrDefault(m => m.EmployeeID == employee.EmployeeID);
                if (existingEmployee != null)
                {
                    ModelState.AddModelError("", "Tài khoản đã được gắn với một người dùng khác, xin vui lòng chọn tài khoản khác");
                    return BadRequest(ModelState);
                }
                employee.Status = true;
                _context.Employee.Add(employee);
                await _context.SaveChangesAsync();
                return Ok();
            }
            return BadRequest(ModelState);
        }

        // GET: Employee/Edit/5
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee.FindAsync(id);
            if (employee == null)
            {
                return NotFound();
            }
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID", employee.AccountID);
            ViewData["CompanyID"] = new SelectList(_context.Company, "CompanyID", "CompanyID", employee.CompanyID);
            return View(employee);
        }

        // POST: Employee/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("EmployeeID,EmployeeName,Gender,PhoneNumber,Email,CompanyID,AccountID")] Employee employee)
        {
            if (id != employee.EmployeeID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(employee);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmployeeExists(employee.EmployeeID))
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
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID", employee.AccountID);
            ViewData["CompanyID"] = new SelectList(_context.Company, "CompanyID", "CompanyID", employee.CompanyID);
            return View(employee);
        }

        // GET: Employee/Delete/5
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var employee = await _context.Employee
                .Include(e => e.Account)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(m => m.EmployeeID == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Employee/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var employee = await _context.Employee.FindAsync(id);
            if (employee != null)
            {
                _context.Employee.Remove(employee);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Profile()
        {
            if (!IsLogin || RoleUser != "1") // Kiểm tra quyền (RoleUser = 1 là nhân viên)
            {
                return RedirectToAction("Login", "Account");
            }

            var employeeId = CurrentID;
            var employee = await _context.Employee
                .Include(e => e.Account)
                .Include(e => e.Company)
                .FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(employeeId));

            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        private bool EmployeeExists(string id)
        {
            return _context.Employee.Any(e => e.EmployeeID == id);
        }
    }
}
