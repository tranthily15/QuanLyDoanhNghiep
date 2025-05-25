using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;
using System.Linq;

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
            try
            {
                const int PageSize = 18;

                var baseQuery = _context.JobPosition
                    .Include(j => j.Company)
                    .Include(j => j.JobLocations)
                        .ThenInclude(l => l.Province)
                    .Where(j => j.Status == true);

                if (!string.IsNullOrEmpty(keyword))
                {
                    baseQuery = baseQuery.Where(j => j.PositionName.Contains(keyword) ||
                                                   j.JobDescription.Contains(keyword) ||
                                                   j.Company.CompanyName.Contains(keyword));
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

                var internships = await internshipQuery
                    .OrderByDescending(j => j.StartDate)
                    .Skip((internshipPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToListAsync();

                var fulltimeJobs = await fulltimeQuery
                    .OrderByDescending(j => j.StartDate)
                    .Skip((fulltimePage - 1) * PageSize)
                    .Take(PageSize)
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
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in Index action: {ex.Message}");
                // Return a default model if there's an error
                var model = new JobIndexViewModel
                {
                    Internships = new List<JobPosition>(),
                    FullTimeJobs = new List<JobPosition>(),
                    Keyword = keyword ?? string.Empty,
                    SelectedProvinces = Array.Empty<string>(),
                    InternshipPage = 1,
                    FullTimePage = 1,
                    TotalInternshipPages = 0,
                    TotalFullTimePages = 0
                };
                ViewBag.Role = RoleUser;
                ViewBag.Provinces = await _context.Province.ToListAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetFulltime(string keyword, string provinces, string jobType, string salaryRange, int page = 1)
        {
            const int PageSize = 6;
            
            // Build base query
            var query = _context.JobPosition
                .Include(j => j.Company)
                .Include(j => j.JobLocations)
                    .ThenInclude(l => l.Province)
                .Where(j => j.Status == true && j.PositionType == true);

            // Apply keyword search if provided
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(j => j.PositionName.Contains(keyword) ||
                                        j.JobDescription.Contains(keyword) ||
                                        j.Company.CompanyName.Contains(keyword));
            }

            // Apply province filter if provided
            if (!string.IsNullOrEmpty(provinces))
            {
                var provinceIds = provinces.Split(',').ToList();
                query = query.Where(j => j.JobLocations.Any(l => provinceIds.Contains(l.ProvinceID)));
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply pagination
            var jobs = await query
                .OrderByDescending(j => j.StartDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(j => new
                {
                    j.PositionID,
                    j.PositionName,
                    j.Salary,
                    j.Company.CompanyName,
                    CompanyLogo = j.Company.CompanyLogo ?? "default.png",
                    ProvinceName = j.JobLocations.Select(l => l.Province.ProvinceName).FirstOrDefault() ?? "Không rõ"
                })
                .ToListAsync();

            return Json(new
            {
                Jobs = jobs,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / PageSize)
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetIntern(string keyword, string provinces, string jobType, string salaryRange, int page = 1)
        {
            const int PageSize = 6;
            
            // Build base query
            var query = _context.JobPosition
                .Include(j => j.Company)
                .Include(j => j.JobLocations)
                    .ThenInclude(l => l.Province)
                .Where(j => j.Status == true && j.PositionType == false);

            // Apply keyword search if provided
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(j => j.PositionName.Contains(keyword) ||
                                        j.JobDescription.Contains(keyword) ||
                                        j.Company.CompanyName.Contains(keyword));
            }

            // Apply province filter if provided
            if (!string.IsNullOrEmpty(provinces))
            {
                var provinceIds = provinces.Split(',').ToList();
                query = query.Where(j => j.JobLocations.Any(l => provinceIds.Contains(l.ProvinceID)));
            }

            // Get total count before pagination
            var total = await query.CountAsync();

            // Apply pagination
            var jobs = await query
                .OrderByDescending(j => j.StartDate)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .Select(j => new
                {
                    j.PositionID,
                    j.PositionName,
                    j.Salary,
                    j.Company.CompanyName,
                    CompanyLogo = j.Company.CompanyLogo ?? "default.png",
                    ProvinceName = j.JobLocations.Select(l => l.Province.ProvinceName).FirstOrDefault() ?? "Không rõ"
                })
                .ToListAsync();

            return Json(new
            {
                Jobs = jobs,
                CurrentPage = page,
                TotalPages = (int)Math.Ceiling((double)total / PageSize)
            });
        }

        //[HttpGet]
        //public async Task<IActionResult> GetInternships(string keyword, string provinces, int page = 1)
        //{
        //    const int PageSize = 18;

        //    var query = _context.JobPosition
        //        .Include(j => j.Company)
        //        .Include(j => j.JobLocations)
        //            .ThenInclude(l => l.Province)
        //        .Where(j => j.Status == true && !j.PositionType); // Internship

        //    if (!string.IsNullOrEmpty(keyword))
        //    {
        //        query = query.Where(j => EF.Functions.Like(j.PositionName, $"%{keyword}%") ||
        //                                 EF.Functions.Like(j.JobDescription, $"%{keyword}%"));
        //    }

        //    if (!string.IsNullOrEmpty(provinces))
        //    {
        //        var provinceIds = provinces.Split(',').ToList();
        //        query = query.Where(j => j.JobLocations.Any(l => provinceIds.Contains(l.ProvinceID)));
        //    }

        //    var total = await query.CountAsync();
        //    var data = await query
        //        .OrderByDescending(j => j.StartDate)
        //        .Skip((page - 1) * PageSize)
        //        .Take(PageSize)
        //        .Select(j => new {
        //            j.PositionID,
        //            j.PositionName,
        //            j.Salary,
        //            CompanyName = j.Company.CompanyName,
        //            CompanyLogo = j.Company.CompanyLogo,
        //            ProvinceName = j.JobLocations.Select(l => l.Province.ProvinceName).FirstOrDefault()
        //        }).ToListAsync();

        //    return Json(new
        //    {
        //        TotalPages = (int)Math.Ceiling((double)total / PageSize),
        //        CurrentPage = page,
        //        Jobs = data
        //    });
        //}

        // GET: JobPosition/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }
            var jobPosition = await _context.JobPosition
                    .Include(j => j.Company.Ward)
                    .Include(j => j.Company.District)
                    .Include(j => j.Company.Province)
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
                ViewBag.IsLogin = IsLogin;
                ViewBag.RoleUser = RoleUser;
                return View(jobPosition);
            }
            else if (IsLogin && RoleUser == "2") // sinh viên
            {
                ViewBag.IsLogin = IsLogin;
                ViewBag.RoleUser = RoleUser;
                ViewBag.User = _context.User.FirstOrDefault(u => u.AccountID.ToString().Equals(CurrentID));
                // Kiểm tra xem vị trí đã được lưu chưa
                ViewBag.IsSaved = await _context.SavedJob
                    .AnyAsync(sj => sj.UserID == CurrentID && sj.PositionID == id && sj.IsSaved);
                return View(jobPosition);
            }
            else
            {
                return View(jobPosition);
            }
        }

        [HttpGet]
        [Route("api/jobpositions")]
        public async Task<IActionResult> GetJobPositions()
        {
            var jobPositions = await _context.JobPosition
                .Where(j => j.Status == true) // Chỉ lấy các vị trí đang hoạt động
                .Select(j => new
                {
                    j.PositionID,
                    j.PositionName,
                    j.JobDescription,
                    j.JobRequirements
                })
                .ToListAsync();

            return Ok(jobPositions);
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
            var jobPosition = await _context.JobPosition
                .Include(jp => jp.JobLocations)
                    .ThenInclude(jl => jl.Province)
                .Include(jp => jp.JobLocations)
                    .ThenInclude(jl => jl.District)
                .Include(jp => jp.JobLocations)
                    .ThenInclude(jl => jl.Ward)
                .FirstOrDefaultAsync(jp => jp.PositionID == id && jp.CompanyID == companyId);

            if (jobPosition == null)
            {
                return NotFound();
            }

            // Load danh sách tỉnh/thành phố cho dropdown
            ViewBag.Provinces = await _context.Province.ToListAsync();

            return View(jobPosition);
        }

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

            var companyId = int.Parse(CurrentCompanyID);
            if (jobPosition.CompanyID != companyId)
            {
                ModelState.AddModelError("", "Bạn không có quyền chỉnh sửa vị trí này.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy danh sách địa điểm hiện tại trong DB
                    var existingLocations = await _context.JobLocation
                        .Where(l => l.PositionID == id)
                        .ToListAsync();

                    // Lọc danh sách mới (loại bỏ bản ghi không hợp lệ)
                    var validNewLocations = JobLocations
                        .Where(l => !string.IsNullOrEmpty(l.ProvinceID) &&
                                    !string.IsNullOrEmpty(l.DistrictID) &&
                                    !string.IsNullOrEmpty(l.Street))
                        .GroupBy(x => new { x.ProvinceID, x.DistrictID, Ward = x.WardID ?? "", x.Street })
                        .Select(g => g.First())
                        .ToList();

                    if (!validNewLocations.Any())
                    {
                        ModelState.AddModelError("", "Vui lòng cung cấp ít nhất một địa điểm hợp lệ.");
                        ViewBag.Provinces = await _context.Province.ToListAsync();
                        return View(jobPosition);
                    }

                    // Tìm location đã bị xoá (có trong DB nhưng không có trong danh sách mới)
                    var toDelete = existingLocations.Where(e =>
                        !validNewLocations.Any(n =>
                            n.ProvinceID == e.ProvinceID &&
                            n.DistrictID == e.DistrictID &&
                            n.WardID == e.WardID &&
                            n.Street.Trim().Equals(e.Street.Trim(), StringComparison.OrdinalIgnoreCase)
                        )
                    ).ToList();

                    // Tìm location mới (có trong danh sách mới nhưng không tồn tại trong DB)
                    var toAdd = validNewLocations.Where(n =>
                        !existingLocations.Any(e =>
                            n.ProvinceID == e.ProvinceID &&
                            n.DistrictID == e.DistrictID &&
                            n.WardID == e.WardID &&
                            n.Street.Trim().Equals(e.Street.Trim(), StringComparison.OrdinalIgnoreCase)
                        )
                    ).ToList();

                    // Thực hiện thao tác xóa và thêm mới
                    if (toDelete.Any())
                    {
                        _context.JobLocation.RemoveRange(toDelete);
                    }

                    foreach (var location in toAdd)
                    {
                        location.PositionID = id;
                        _context.JobLocation.Add(location);
                    }

                    // Cập nhật thông tin JobPosition
                    _context.Entry(jobPosition).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cập nhật vị trí tuyển dụng thành công!";
                    return RedirectToAction(nameof(ActiveJobs));
                }
                catch (DbUpdateConcurrencyException)
                {
                    ModelState.AddModelError("", "Lỗi tương tranh. Dữ liệu có thể đã bị thay đổi bởi người khác.");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", "Đã xảy ra lỗi không mong muốn: " + ex.Message);
                }
            }

            // Nếu có lỗi thì trả lại view
            ViewBag.Provinces = await _context.Province.ToListAsync();
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

        [HttpPost]
        public async Task<IActionResult> DeleteJobLocation(int locationId)
        {
            var jobLocation = await _context.JobLocation.FindAsync(locationId);
            if (jobLocation == null)
            {
                return Json(new { success = false, message = "Địa điểm không tồn tại." });
            }

            try
            {
                _context.JobLocation.Remove(jobLocation);
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã xóa địa điểm thành công." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<IActionResult> List(string type = "all")
        {
            var baseQuery = _context.JobPosition
                .Include(j => j.Company)
                .Include(j => j.JobLocations)
                    .ThenInclude(l => l.Province)
                .Where(j => j.Status == true);

            if (type == "intern")
            {
                baseQuery = baseQuery.Where(j => !j.PositionType);
            }
            else if (type == "fulltime")
            {
                baseQuery = baseQuery.Where(j => j.PositionType);
            }
            // else: all

            var jobs = await baseQuery.ToListAsync();
            return View(jobs);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveJob(int positionId)
        {
            if (!IsLogin || RoleUser != "2") // Chỉ cho phép sinh viên lưu vị trí
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập với tài khoản sinh viên để lưu vị trí." });
            }

            try
            {
                var userId = CurrentID;
                var existingSavedJob = await _context.SavedJob
                    .FirstOrDefaultAsync(sj => sj.UserID == userId && sj.PositionID == positionId);

                if (existingSavedJob != null)
                {
                    if (existingSavedJob.IsSaved)
                    {
                        return Json(new { success = false, message = "Bạn đã lưu vị trí này rồi." });
                    }
                    else
                    {
                        existingSavedJob.IsSaved = true;
                        existingSavedJob.SavedDate = DateTime.Now;
                        _context.Update(existingSavedJob);
                    }
                }
                else
                {
                    var savedJob = new SavedJob
                    {
                        UserID = userId,
                        PositionID = positionId,
                        SavedDate = DateTime.Now,
                        IsSaved = true
                    };
                    _context.SavedJob.Add(savedJob);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Đã lưu vị trí thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnsaveJob(int positionId)
        {
            if (!IsLogin || RoleUser != "2")
            {
                return Json(new { success = false, message = "Bạn cần đăng nhập với tài khoản sinh viên để thực hiện thao tác này." });
            }

            try
            {
                var userId = CurrentID;
                var savedJob = await _context.SavedJob
                    .FirstOrDefaultAsync(sj => sj.UserID == userId && sj.PositionID == positionId);

                if (savedJob == null || !savedJob.IsSaved)
                {
                    return Json(new { success = false, message = "Vị trí này chưa được lưu." });
                }

                savedJob.IsSaved = false;
                _context.Update(savedJob);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Đã hủy lưu vị trí thành công!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        public async Task<IActionResult> SavedJobs()
        {
            if (!IsLogin || RoleUser != "2")
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = CurrentID;
            var savedJobs = await _context.SavedJob
                .Include(sj => sj.JobPosition)
                    .ThenInclude(jp => jp.Company)
                .Include(sj => sj.JobPosition)
                    .ThenInclude(jp => jp.JobLocations)
                        .ThenInclude(jl => jl.Province)
                .Where(sj => sj.UserID == userId)
                .OrderByDescending(sj => sj.SavedDate)
                .ToListAsync();

            return View(savedJobs);
        }
    }
}
