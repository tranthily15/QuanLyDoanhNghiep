using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuanLyDoanhNghiep.Models;
using QuanLyDoanhNghiep.Services;

namespace QuanLyDoanhNghiep.Controllers
{
    public class UserController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;
        private readonly SyncDataService _syncDataService;

        public UserController(QuanLyDoanhNghiepDBContext context, SyncDataService syncDataService)
        {
            _context = context;
            _syncDataService = syncDataService;
        }

        // GET: User
        public async Task<IActionResult> Index()
        {
            await _syncDataService.SyncDataAsync();

            var quanLyDoanhNghiepDBContext = _context.User.Include(u => u.Account);
            return View(await quanLyDoanhNghiepDBContext.ToListAsync());

        }

        // GET: User/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (id == null)
            {
                return NotFound();
            }
            ViewBag.Role = RoleUser;
            var user = await _context.User
                .Include(u => u.Account)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // GET: User/Create
        public IActionResult Create()
        {
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID");
            return View();
        }

        // POST: User/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,FullName,DateOfBirth,Gender,GraduationYear,GPA,Honors,Email,PhoneNumber,AccountID")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID", user.AccountID);
            return View(user);
        }

        // GET: User/Edit/5
        public async Task<IActionResult> Edit(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID", user.AccountID);
            return View(user);
        }

        // POST: User/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, [Bind("ID,FullName,DateOfBirth,Gender,GraduationYear,GPA,Honors,Email,PhoneNumber,AccountID")] User user)
        {
            if (id != user.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.ID))
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
            ViewData["AccountID"] = new SelectList(_context.Account, "AccountID", "AccountID", user.AccountID);
            return View(user);
        }

        // GET: User/Delete/5
        public async Task<IActionResult> Delete(string id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.User
                .Include(u => u.Account)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: User/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.User.FindAsync(id);
            if (user != null)
            {
                _context.User.Remove(user);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Profile()
        {
            if (!IsLogin || RoleUser != "2") // Kiểm tra quyền (RoleUser = 2 là sinh viên)
            {
                return RedirectToAction("Login", "Account");
            }

           
            var user = await _context.User
                .Include (u => u.Account)
                .FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        private bool UserExists(string id)
        {
            return _context.User.Any(e => e.ID == id);
        }
    }
}
