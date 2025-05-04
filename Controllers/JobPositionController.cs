using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Controllers
{
    public class JobPositionController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public JobPositionController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(string keyword, string provinces, int internshipPage = 1, int fulltimePage = 1)
        {
            const int PageSize = 21;

            var baseQuery = _context.JobPosition
                .Include(j => j.Company)
                .Include(j => j.JobLocations)
                    .ThenInclude(l => l.Province)
                .Where(j => j.Status == true);

            if (!string.IsNullOrEmpty(keyword))
            {
                baseQuery = baseQuery.Where(j => EF.Functions.Like(j.PositionName, $"%{keyword}%") ||
                                                 EF.Functions.Like(j.JobDescription, $"%{keyword}%"));
            }

            if (!string.IsNullOrEmpty(provinces))
            {
                var provinceIds = provinces.Split(',').ToList();
                baseQuery = baseQuery.Where(j => j.JobLocations.Any(l => provinceIds.Contains(l.ProvinceID)));
            }

            if (IsLogin && RoleUser == "1")
            {
                baseQuery = baseQuery.Where(j => j.Company.CompanyID == int.Parse(CurrentCompanyID));
            }

            // Lấy full danh sách (để tính tổng số trang)
            var internshipQuery = baseQuery.Where(j => !j.PositionType);
            var fulltimeQuery = baseQuery.Where(j => j.PositionType);

            var totalInterns = await internshipQuery.CountAsync();
            var totalFulltime = await fulltimeQuery.CountAsync();
            Console.WriteLine($"Total FullTimeJobs: {totalFulltime}");
            var internships = await internshipQuery
                .ToListAsync();

            var fulltimeJobs = await fulltimeQuery
                .ToListAsync();

            var model = new JobIndexViewModel
            {
                Internships = internships,
                FullTimeJobs = fulltimeJobs,
                Keyword = keyword ?? string.Empty,
                SelectedProvinces = provinces?.Split(',') ?? Array.Empty<string>(),
                InternshipPage = internshipPage,
                FullTimePage = fulltimePage,
                TotalInternshipPages = (int)Math.Ceiling((double)totalInterns / PageSize),
                TotalFullTimePages = (int)Math.Ceiling((double)totalFulltime / PageSize)
            };

            ViewBag.Role = RoleUser;
            ViewBag.Provinces = await _context.Province.ToListAsync();

            return View(model);
        }
        public async Task<IActionResult> LoadFullTimeJobs(int page = 1)
        {
            const int PageSize = 21;
            var query = _context.JobPosition
                .Include(j => j.Company)
                .Include(j => j.JobLocations)
                    .ThenInclude(l => l.Province)
                .Where(j => j.Status && j.PositionType);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Đảm bảo page không vượt quá totalPages
            page = Math.Min(page, totalPages);
            page = Math.Max(1, page);

            var data = await query
                //.OrderByDescending(j => j.CreatedDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return PartialView("_JobListPartial", data);
        }

        public async Task<IActionResult> LoadInternships(int page = 1)
        {
            const int PageSize = 21;
            var query = _context.JobPosition
                .Include(j => j.Company)
                .Include(j => j.JobLocations)
                    .ThenInclude(l => l.Province)
                .Where(j => j.Status && !j.PositionType);

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)PageSize);

            // Đảm bảo page không vượt quá totalPages
            page = Math.Min(page, totalPages);
            page = Math.Max(1, page);

            var data = await query
                //.OrderByDescending(j => j.CreatedDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return PartialView("_JobListPartial", data);
        }



        // GET: JobPosition/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var jobPosition = await _context.JobPosition
                    .Include(j => j.Company)
                        .ThenInclude(c => c.Ward)
                    .Include(j => j.Company)
                        .ThenInclude(w => w.District)
                    .Include(j => j.Company)
                        .ThenInclude(d => d.Province)
                    .Include(j => j.JobLocations)
                        .ThenInclude(l => l.Ward)
                    .Include(j => j.JobLocations)
                        .ThenInclude(l => l.District)
                    .Include(j => j.JobLocations)
                        .ThenInclude(l => l.Province)
                    .FirstOrDefaultAsync(j => j.PositionID == id);
            if (jobPosition == null)
            {
                return NotFound();
            }

            if (!IsLogin)
            {
                return View(jobPosition);
            }
            else if (IsLogin && RoleUser == "2") // sinh vien
            {
                ViewBag.User = _context.User.FirstOrDefault(u => u.AccountID.ToString().Equals(CurrentID));
                return View(jobPosition);
            }
            else
            {
                return View(jobPosition);
            }
        }

        // GET: JobPosition/Create
        public IActionResult Create()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Provinces = _context.Province.ToList();
            return View();
        }

        // POST: JobPosition/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(JobPosition jobPosition, List<JobLocation> JobLocations)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    // Kiểm tra ngày tháng
                    if (jobPosition.EndDate < jobPosition.StartDate)
                    {
                        ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
                        ViewBag.Provinces = _context.Province.ToList();
                        return View(jobPosition);
                    }

                    // Xử lý dữ liệu trước khi lưu
                    jobPosition.Status = true;
                    jobPosition.CompanyID = int.Parse(CurrentCompanyID);

                    // Xử lý JobLocations
                    if (JobLocations != null && JobLocations.Count > 0)
                    {
                        // Lọc và loại bỏ các địa điểm trùng lặp và rỗng
                        var uniqueLocations = JobLocations
                            .Where(l => !string.IsNullOrEmpty(l.ProvinceID) &&
                                      !string.IsNullOrEmpty(l.DistrictID) &&
                                      !string.IsNullOrEmpty(l.Street))
                            .GroupBy(x => new
                            {
                                x.ProvinceID,
                                x.DistrictID,
                                x.WardID,
                                x.Street
                            })
                            .Select(g => g.First())
                            .ToList();

                        if (uniqueLocations.Count == 0)
                        {
                            ModelState.AddModelError("", "Vui lòng nhập ít nhất một địa điểm hợp lệ");
                            ViewBag.Provinces = _context.Province.ToList();
                            return View(jobPosition);
                        }

                        // Lưu JobPosition trước
                        _context.JobPosition.Add(jobPosition);
                        await _context.SaveChangesAsync();

                    }
                    else
                    {
                        ModelState.AddModelError("", "Vui lòng nhập ít nhất một địa điểm");
                        ViewBag.Provinces = _context.Province.ToList();
                        return View(jobPosition);
                    }

                    TempData["SuccessMessage"] = "Tạo vị trí tuyển dụng thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Có lỗi xảy ra khi lưu dữ liệu: " + ex.Message);
                }
            }

            // Nếu có lỗi, load lại dữ liệu cho view
            ViewBag.Provinces = _context.Province.ToList();
            return View(jobPosition);
        }

        // GET: JobPosition/Edit/5
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

            var companyId = int.Parse(CurrentCompanyID);
            // Load JobPosition including its current JobLocations
            var jobPosition = await _context.JobPosition
                .Include(jp => jp.JobLocations) // Include existing locations
                    .ThenInclude(jl => jl.Ward) // Include details for dropdowns
                .Include(jp => jp.JobLocations)
                    .ThenInclude(w => w.District)
                .Include(jp => jp.JobLocations)
                    .ThenInclude(d => d.Province)
                .FirstOrDefaultAsync(jp => jp.PositionID == id && jp.CompanyID == companyId);

            if (jobPosition == null)
            {
                // Ensure the user can only edit their own jobs
                return NotFound();
            }

            // Load Provinces for dropdowns
            ViewBag.Provinces = await _context.Province.ToListAsync();
            // Pass existing locations to the view (optional, view can iterate Model.JobLocations)
            // ViewBag.ExistingLocations = jobPosition.JobLocations;

            return View(jobPosition);
        }

        // POST: JobPosition/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, JobPosition jobPosition, List<JobLocation> JobLocations)
        {
            if (id != jobPosition.PositionID)
            {
                return NotFound();
            }

            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            // Ensure the company ID matches the logged-in user
            var companyId = int.Parse(CurrentCompanyID);
            if (jobPosition.CompanyID != companyId)
            {
                ModelState.AddModelError("", "Bạn không có quyền chỉnh sửa vị trí này.");
            }

            // Basic ModelState validation first
            if (ModelState.IsValid)
            {
                try
                {
                    // Handle JobLocations: Remove old, add new
                    var existingLocations = await _context.JobLocation
                        .Where(l => l.PositionID == id)
                        .ToListAsync();
                    _context.JobLocation.RemoveRange(existingLocations);

                    if (JobLocations != null && JobLocations.Any())
                    {
                        var uniqueNewLocations = JobLocations
                            .Where(l => !string.IsNullOrEmpty(l.ProvinceID) &&
                                      !string.IsNullOrEmpty(l.DistrictID) &&
                                      !string.IsNullOrEmpty(l.WardID) &&
                                      !string.IsNullOrEmpty(l.Street))
                            .GroupBy(x => new { x.ProvinceID, x.DistrictID, x.WardID, x.Street })
                            .Select(g => g.First())
                            .ToList();

                        if (!uniqueNewLocations.Any())
                        {
                            ModelState.AddModelError("", "Vui lòng cung cấp ít nhất một địa điểm hợp lệ.");
                        }
                        else
                        {
                            foreach (var location in uniqueNewLocations)
                            {
                                location.PositionID = id; // Assign the correct PositionID
                                _context.JobLocation.Add(location);
                            }
                        }

                    }
                    else
                    { // No locations submitted
                        ModelState.AddModelError("", "Vui lòng cung cấp ít nhất một địa điểm.");
                    }

                    // Re-check ModelState after location processing
                    if (!ModelState.IsValid)
                    {
                        ViewBag.Provinces = await _context.Province.ToListAsync();
                        // Reload the original job position data in case of error to avoid losing other edits
                        var originalJob = await _context.JobPosition.AsNoTracking().FirstOrDefaultAsync(j => j.PositionID == id);
                        jobPosition.JobLocations = originalJob?.JobLocations ?? new List<JobLocation>(); // Keep original locations on error
                        return View(jobPosition);
                    }

                    // If all good, update the JobPosition itself
                    _context.Update(jobPosition);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật vị trí tuyển dụng thành công!";
                    return RedirectToAction(nameof(ActiveJobs)); // Redirect to the job list
                }
                catch (DbUpdateConcurrencyException)
                {
                    // ... (concurrency handling) ...
                    ModelState.AddModelError("", "Lỗi tương tranh. Dữ liệu có thể đã bị thay đổi bởi người khác.");
                }
                catch (Exception ex)
                {
                    // Log the exception (ex)
                    ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn khi cập nhật.");
                }
            }

            // If ModelState is invalid at any point, return to the view
            ViewBag.Provinces = await _context.Province.ToListAsync();
            // Reload locations if they weren't passed back correctly or if initial load failed
            if (jobPosition.JobLocations == null || !jobPosition.JobLocations.Any())
            {
                var originalJob = await _context.JobPosition.Include(j => j.JobLocations).AsNoTracking().FirstOrDefaultAsync(j => j.PositionID == id);
                jobPosition.JobLocations = originalJob?.JobLocations ?? new List<JobLocation>();
            }
            return View(jobPosition);
        }

        // GET: JobPosition/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var jobPosition = await _context.JobPosition
                .Include(j => j.Company)
                .FirstOrDefaultAsync(m => m.PositionID == id);
            if (jobPosition == null)
            {
                return NotFound();
            }

            return View(jobPosition);
        }

        // POST: JobPosition/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var jobPosition = await _context.JobPosition.FindAsync(id);
            if (jobPosition != null)
            {
                _context.JobPosition.Remove(jobPosition);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool JobPositionExists(int id)
        {
            return _context.JobPosition.Any(e => e.PositionID == id);
        }

        // GET: JobPosition/ActiveJobs (Title updated in previous step)
        public async Task<IActionResult> ActiveJobs()
        {
            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            var companyId = int.Parse(CurrentCompanyID);
            // var today = DateTime.Today; // No longer needed for the main query

            // Fetch ALL jobs for the company, let the View handle filtering for display
            var jobs = await _context.JobPosition
                .Where(j => j.CompanyID == companyId) // Get all jobs for this company
                .OrderByDescending(j => j.Status) // Active jobs first
                .ThenByDescending(j => j.EndDate) // Then by end date (for inactive jobs)
                .ToListAsync();

            // ViewData["Title"] is already set to "Quản lý vị trí tuyển dụng"
            return View(jobs);
        }

        // POST: JobPosition/ToggleJobStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleJobStatus(int id, DateTime? newEndDate)
        {
            if (!IsLogin || RoleUser != "1")
            {
                return RedirectToAction("Login", "Account");
            }

            var companyId = int.Parse(CurrentCompanyID);
            var jobPosition = await _context.JobPosition.FindAsync(id);
            var today = DateTime.Now;

            if (jobPosition == null || jobPosition.CompanyID != companyId)
            {
                return NotFound();
            }

            bool wasActive = jobPosition.Status;

            if (wasActive)
            {
                // --- Deactivating an active job --- 
                jobPosition.Status = false;
                TempData["SuccessMessage"] = $"Đã tắt tuyển dụng cho vị trí '{jobPosition.PositionName}'.";
            }
            else // Activating an inactive job
            {
                bool isExpired = jobPosition.EndDate < today;

                // 1. Validate the provided newEndDate if it exists
                if (newEndDate.HasValue)
                {
                    if (newEndDate.Value <= today)
                    {
                        TempData["ErrorMessage"] = "Hạn nộp mới phải sau ngày hiện tại.";
                        return RedirectToAction(nameof(ActiveJobs));
                    }
                    // Update EndDate if a valid new date is provided
                    jobPosition.EndDate = newEndDate.Value;
                }
                // 2. If no newEndDate was provided, check if the job was expired (it must be extended)
                else if (isExpired)
                {
                    TempData["ErrorMessage"] = "Vui lòng nhập hạn nộp mới để bật lại vị trí đã hết hạn.";
                    return RedirectToAction(nameof(ActiveJobs));
                }
                // 3. If no newEndDate provided AND job wasn't expired, EndDate remains unchanged.

                // 4. Activate the job
                jobPosition.Status = true;
                TempData["SuccessMessage"] = $"Đã bật tuyển dụng cho vị trí '{jobPosition.PositionName}' với hạn nộp đến {jobPosition.EndDate:dd/MM/yyyy HH:mm}.";
            }

            try
            {
                _context.Update(jobPosition);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái.";
                return RedirectToAction(nameof(ActiveJobs));
            }
            catch (Exception ex)
            {
                // Log the exception ex
                TempData["ErrorMessage"] = "Đã xảy ra lỗi không mong muốn.";
                return RedirectToAction(nameof(ActiveJobs));
            }

            return RedirectToAction(nameof(ActiveJobs));
        }

        [HttpGet]
        public IActionResult GetDistricts(string provinceId)
        {

            var districts = _context.District

                .Where(d => provinceId.Contains(d.ProvinceID))
                .Select(d => new { districtID = d.DistrictID, districtName = d.DistrictName })
                .ToList();

            return Json(districts);
        }

        public IActionResult GetWards(string districtId)
        {
            var wards = _context.Ward
                        .Where(w => w.DistrictID == districtId)
                        .Select(w => new { w.WardID, w.WardName })
                        .ToList();
            return Json(wards);
        }
    }
}
