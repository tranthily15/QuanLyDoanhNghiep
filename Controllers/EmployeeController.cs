using System;
using System.Collections.Generic;
using System.Linq;
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
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID");
            ViewData["CompanyID"] = new SelectList(_context.Company, "CompanyID", "CompanyID");
            return View();
        }

        // POST: Employee/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmployeeID,EmployeeName,Gender,PhoneNumber,Email,CompanyID,AccountID")] Employee employee)
        {
            if (ModelState.IsValid)
            {
                _context.Add(employee);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID", employee.AccountID);
            ViewData["CompanyID"] = new SelectList(_context.Company, "CompanyID", "CompanyID", employee.CompanyID);
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
