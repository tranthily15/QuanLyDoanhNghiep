using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Controllers
{
    public class ITPositionCategoryController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public ITPositionCategoryController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
        }

        // GET: ITPositionCategory
        public async Task<IActionResult> Index()
        {
            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            return View(await _context.ITPositionCategory.ToListAsync());
        }

        // GET: ITPositionCategory/Create
        public IActionResult Create()
        {
            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            return View();
        }

        // POST: ITPositionCategory/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ITPositionCategory category)
        {
            if (ModelState.IsValid)
            {
                _context.Add(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo nhóm vị trí thành công!";
                return RedirectToAction(nameof(Index));
            }
            return View(category);
        }

        // GET: ITPositionCategory/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.ITPositionCategory.FindAsync(id);
            if (category == null)
            {
                return NotFound();
            }
            return View(category);
        }

        // POST: ITPositionCategory/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ITPositionCategory category)
        {
            if (id != category.CategoryID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(category);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật nhóm vị trí thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CategoryExists(category.CategoryID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(category);
        }

        // GET: ITPositionCategory/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            if (id == null)
            {
                return NotFound();
            }

            var category = await _context.ITPositionCategory
                .FirstOrDefaultAsync(m => m.CategoryID == id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }

        // POST: ITPositionCategory/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var category = await _context.ITPositionCategory.FindAsync(id);
            if (category != null)
            {
                _context.ITPositionCategory.Remove(category);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Xóa nhóm vị trí thành công!";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool CategoryExists(int id)
        {
            return _context.ITPositionCategory.Any(e => e.CategoryID == id);
        }
    }
}