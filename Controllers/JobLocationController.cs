using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Controllers
{
    public class JobLocationController : Controller
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public JobLocationController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
        }

        // GET: JobLocation
        public async Task<IActionResult> Index()
        {
            var jobLocations = await _context.JobLocation
                .Include(j => j.JobPosition)
                .Include(j => j.Province)
                .Include(j => j.District)
                .Include(j => j.Ward)
                .ToListAsync();

            return View(jobLocations);
        }

        // GET: JobLocation/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var jobLocation = await _context.JobLocation
                .Include(j => j.JobPosition)
                .Include(j => j.Province)
                .Include(j => j.District)
                .Include(j => j.Ward)
                .FirstOrDefaultAsync(m => m.JobLocationID == id);

            if (jobLocation == null)
                return NotFound();

            return View(jobLocation);
        }

        // GET: JobLocation/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: JobLocation/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobLocation jobLocation)
        {
            if (ModelState.IsValid)
            {
                _context.Add(jobLocation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(jobLocation);
        }

        // GET: JobLocation/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            var jobLocation = await _context.JobLocation.FindAsync(id);
            if (jobLocation == null)
                return NotFound();

            return View(jobLocation);
        }

        // POST: JobLocation/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobLocation jobLocation)
        {
            if (id != jobLocation.JobLocationID)
                return NotFound();

            if (ModelState.IsValid)
            {
                _context.Update(jobLocation);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(jobLocation);
        }

        // GET: JobLocation/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            var jobLocation = await _context.JobLocation.FindAsync(id);
            if (jobLocation == null)
                return NotFound();

            return View(jobLocation);
        }

        // POST: JobLocation/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var jobLocation = await _context.JobLocation.FindAsync(id);
            if (jobLocation != null)
            {
                _context.JobLocation.Remove(jobLocation);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
