using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;
//using QuanLyDoanhNghiep.Services;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using Newtonsoft.Json.Linq;

namespace QuanLyDoanhNghiep.Controllers
{
    public class CVController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public CVController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
        }

        // GET: CV
        public async Task<IActionResult> Index()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.Role = RoleUser;

            if (!int.TryParse(CurrentID, out int currentID))
            {
                return RedirectToAction("Login", "Account");
            }

            // sinh viên
            if (RoleUser == "2")
            {
                var user = _context.User.FirstOrDefault(m => m.AccountID == currentID);
                if (user == null)
                {
                    return NotFound("Không tìm thấy người dùng.");
                }

                // Lấy tất cả CV của sinh viên kèm theo thông tin JobPosition và Company
                var studentCVs = await _context.CV
                    .Include(c => c.JobPosition)
                        .ThenInclude(j => j.Company)
                    .Include(c => c.User)
                    .Where(m => m.ID == user.ID)
                    .OrderByDescending(c => c.CV_ID)
                    .ToListAsync();

                ViewBag.PendingCVs = studentCVs.Where(c => c.Status == 0).ToList();
                ViewBag.AcceptedCVs = studentCVs.Where(c => c.Status == 1).ToList();
                ViewBag.RejectedCVs = studentCVs.Where(c => c.Status == 2).ToList();

                ViewBag.Student = user;

                return View(studentCVs);
            }
            // nhân viên
            else if (RoleUser == "1")
            {
                if (!int.TryParse(CurrentCompanyID, out int currentCompanyID))
                {
                    return RedirectToAction("Login", "Account");
                }

                var jobPositionIds = await _context.JobPosition
                    .Where(m => m.CompanyID == currentCompanyID)
                    .Select(m => m.PositionID)
                    .ToListAsync();

                if (!jobPositionIds.Any())
                {
                    return View(new List<CV>());
                }

                // Lấy tất cả CV đã ứng tuyển vào các vị trí của công ty
                var allCVs = await _context.CV
                    .Include(c => c.JobPosition)
                        .ThenInclude(j => j.Company)
                    .Include(c => c.User)
                    .Where(c => jobPositionIds.Contains(c.PositionID))
                    .OrderByDescending(c => c.CV_ID)
                    .ToListAsync();

                ViewBag.PendingCVs = allCVs.Where(c => c.Status == 0).ToList();
                ViewBag.AcceptedCVs = allCVs.Where(c => c.Status == 1).ToList();
                ViewBag.RejectedCVs = allCVs.Where(c => c.Status == 2).ToList();

                return View(allCVs);
            }

            return RedirectToAction("Login", "Account");
        }

        // GET: CV/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cv = await _context.CV
                .Include(c => c.JobPosition)
                    .ThenInclude(j => j.Company)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CV_ID == id);

            if (cv == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền truy cập
            if (RoleUser == "1") // Nếu là nhân viên công ty
            {
                if (!int.TryParse(CurrentCompanyID, out int currentCompanyID))
                {
                    return RedirectToAction("Login", "Account");
                }

                // Kiểm tra xem CV có thuộc về công ty của nhân viên không
                var jobPosition = await _context.JobPosition
                    .FirstOrDefaultAsync(j => j.PositionID == cv.PositionID && j.CompanyID == currentCompanyID);

                if (jobPosition == null)
                {
                    return Forbid();
                }
            }

            ViewBag.EvaluationDate = cv.JobPosition.EndDate.ToString("dd/MM/yyyy");
            ViewBag.IsQualified = true;

            return View(cv);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int PositionID, string ID, IFormFile ResumeFile)
        {
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx" };
            var extensions = Path.GetExtension(ResumeFile.FileName).ToLower();
            // Kiểm tra file CV
            if (ResumeFile == null || ResumeFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng tải lên file CV.";
                return RedirectToAction("Details", "JobPosition", new { id = PositionID });
            }
            else if (!allowedExtensions.Contains(extensions))
            {
                TempData["ErrorMessage"] = "Chỉ chấp nhận file có định dạng PDF, DOC, hoặc DOCX.";
                return RedirectToAction("Details", "JobPosition", new { id = PositionID });
            }
            else
            {
                // Lưu file CV vào thư mục trên server
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cvs");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string fileName = ResumeFile.FileName;
                string filePath = Path.Combine(uploadsFolder, fileName);

                while (System.IO.File.Exists(filePath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                    string fileExtension = Path.GetExtension(fileName);
                    string uniqueCode = Guid.NewGuid().ToString().Substring(0, 8);
                    fileName = $"{uniqueCode}_{fileNameWithoutExtension}{fileExtension}";
                    filePath = Path.Combine(uploadsFolder, fileName);
                }

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await ResumeFile.CopyToAsync(stream);
                }

                // Kiểm tra xem người dùng đã có CV nào đang chờ duyệt cho vị trí này chưa
                var pendingCV = await _context.CV
                    .FirstOrDefaultAsync(c => c.PositionID == PositionID &&
                                            c.ID == ID &&
                                            c.Status == 0); // Chỉ kiểm tra CV đang chờ duyệt

                if (pendingCV != null)
                {
                    TempData["ErrorMessage"] = "Bạn đã có CV đang chờ duyệt cho vị trí này. Vui lòng đợi CV hiện tại được duyệt hoặc từ chối trước khi nộp CV mới.";
                    return RedirectToAction("Details", "JobPosition", new { id = PositionID });
                }

                // Tạo đối tượng CV mới
                var newCV = new CV
                {
                    PositionID = PositionID,
                    ID = ID,
                    Resume = $"/uploads/cvs/{fileName}",
                    Status = 0,
                    SubmissionDate = DateTime.Now
                };

                // Lưu vào cơ sở dữ liệu
                _context.CV.Add(newCV);
                await _context.SaveChangesAsync();
                var cv_id = newCV.CV_ID;

                // Lấy thông tin vị trí và công ty
                var jobPosition = await _context.JobPosition
                    .Include(j => j.Company)
                    .FirstOrDefaultAsync(j => j.PositionID == PositionID);

                if (jobPosition != null)
                {
                    // Tạo thông báo cho nhân viên công ty
                    var employee = await _context.Employee
                        .Where(e => e.CompanyID == jobPosition.CompanyID)
                        .ToListAsync();

                    if (employee != null)
                    {
                        foreach (var e in employee)
                        {
                            var notification_e = new Notification
                            {
                                Message = $"Có sinh viên mới ứng tuyển vào vị trí {jobPosition.PositionName}",
                                CreatedAt = DateTime.Now,
                                IsRead = false,
                                UserRole = "1", // Role của nhân viên
                                EmployeeID = e.EmployeeID,
                                NotificationPath = $"/CV/Details/{cv_id}"
                            };
                            _context.Notification.Add(notification_e);
                        }
                    }
                    // tạo thông báo cho sinh viên
                    var notification_user = new Notification
                    {
                        Message = $"Bạn đã ứng tuyển thành công vào vị trí {jobPosition.PositionName} tại {jobPosition.Company.CompanyName}",
                        CreatedAt = DateTime.Now,
                        IsRead = false,
                        UserRole = "2", // Role của sinh viên
                        UserID = ID,
                        NotificationPath = $"/CV/Details/{cv_id}"
                    };
                    _context.Notification.Add(notification_user);
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã gửi CV thành công!";
                return RedirectToAction("Index", "CV");
            }
        }
        //[HttpGet]
        //public IActionResult UploadAndSuggest()
        //{
        //    return View();
        //}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAndSuggest(IFormFile ResumeFile)
        {
            if (ResumeFile == null || ResumeFile.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng tải lên file CV.";
                return RedirectToAction("Index");
            }

            // Lưu file CV vào thư mục trên server
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "cvs_suggested");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string fileName = ResumeFile.FileName;
            string filePath = Path.Combine(uploadsFolder, fileName);

            // Đảm bảo tên file không bị trùng
            while (System.IO.File.Exists(filePath))
            {
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
                string fileExtension = Path.GetExtension(fileName);
                string uniqueCode = Guid.NewGuid().ToString().Substring(0, 8);
                fileName = $"{uniqueCode}_{fileNameWithoutExtension}{fileExtension}";
                filePath = Path.Combine(uploadsFolder, fileName);
            }

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await ResumeFile.CopyToAsync(stream);
            }

            // Gửi dữ liệu đến Python API
            var apiUrl = "http://localhost:5000/api/process-cv";
            using (var client = new HttpClient())
            {
                var payload = new
                {
                    cvPath = filePath,
                    jobPositions = _context.JobPosition
                        .Select(j => new
                        {
                            id = j.PositionID,
                            description = j.PositionName + " " + j.JobDescription + " " + j.JobRequirements
                        })
                        .ToList()
                };

                var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    var responseData = JsonConvert.DeserializeObject<JObject>(result);

                    // Lấy danh sách JobPositionID phù hợp
                    var matchedJobPositionIDs = responseData["matchedJobPositionIDs"]?.ToObject<List<int>>() ?? new List<int>();
                    if (!matchedJobPositionIDs.Any())
                    {
                        TempData["ErrorMessage"] = "Không tìm thấy vị trí nào phù hợp với CV của bạn.";
                        return RedirectToAction("Index");
                    }

                    // Truy vấn danh sách JobPosition từ cơ sở dữ liệu
                    var matchedJobPositions = await _context.JobPosition
                        .Include(j => j.Company)
                        .Where(j => matchedJobPositionIDs.Contains(j.PositionID))
                        .ToListAsync();

                    ViewBag.CVSections = responseData["allSections"];

                    return View("SuggestedJobs", matchedJobPositions);
                }
                else
                {
                    TempData["ErrorMessage"] = "Có lỗi xảy ra khi xử lý CV.";
                    return RedirectToAction("Index");
                }
            }
        }

        // POST: CV/UpdateStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int status, string feedback)
        {
            try
            {
                if (!IsLogin || RoleUser != "1")
                {
                    return RedirectToAction("Login", "Account");
                }

                var cv = await _context.CV
                    .Include(c => c.JobPosition)
                        .ThenInclude(j => j.Company)
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.CV_ID == id);

                if (cv == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy CV với ID này.";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra quyền duyệt CV
                if (!int.TryParse(CurrentCompanyID, out int currentCompanyID))
                {
                    TempData["ErrorMessage"] = "Không xác định được công ty của bạn.";
                    return RedirectToAction("Login", "Account");
                }

                var jobPosition = await _context.JobPosition
                    .FirstOrDefaultAsync(j => j.PositionID == cv.PositionID && j.CompanyID == currentCompanyID);

                if (jobPosition == null)
                {
                    TempData["ErrorMessage"] = "Bạn không có quyền duyệt CV này.";
                    return RedirectToAction(nameof(Index));
                }

                // Kiểm tra trạng thái hợp lệ
                if (status != 1 && status != 2)
                {
                    TempData["ErrorMessage"] = "Trạng thái không hợp lệ.";
                    return RedirectToAction(nameof(Index));
                }

                cv.Status = status;
                cv.Feedback = feedback;
                cv.EvaluationDate = DateTime.Now;

                _context.Update(cv);

                // Tạo thông báo cho sinh viên
                var notification = new Notification
                {
                    Message = status == 1
                        ? $"CV của bạn cho vị trí {cv.JobPosition.PositionName} tại {cv.JobPosition.Company.CompanyName} đã được duyệt!"
                        : $"CV của bạn cho vị trí {cv.JobPosition.PositionName} tại {cv.JobPosition.Company.CompanyName} đã bị từ chối.",
                    CreatedAt = DateTime.Now,
                    IsRead = false,
                    UserRole = "2", // Role của sinh viên
                    UserID = cv.User.ID
                };
                _context.Notification.Add(notification);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = status == 1 ? "Đã chấp nhận CV thành công!" : "Đã từ chối CV thành công!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật trạng thái CV: " + ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CV/Create
        public IActionResult Create()
        {
            ViewData["PositionID"] = new SelectList(_context.JobPosition, "PositionID", "PositionID");
            ViewData["ID"] = new SelectList(_context.User, "ID", "ID");
            return View();
        }

        // POST: CV/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CV_ID,ID,PositionID,Skills,Resume,Status")] CV cV)
        {
            if (ModelState.IsValid)
            {
                _context.Add(cV);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PositionID"] = new SelectList(_context.JobPosition, "PositionID", "PositionID", cV.PositionID);
            ViewData["ID"] = new SelectList(_context.User, "ID", "ID", cV.ID);
            return View(cV);
        }

        // GET: CV/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cV = await _context.CV.FindAsync(id);
            if (cV == null)
            {
                return NotFound();
            }
            ViewData["PositionID"] = new SelectList(_context.JobPosition, "PositionID", "PositionID", cV.PositionID);
            ViewData["ID"] = new SelectList(_context.User, "ID", "ID", cV.ID);
            return View(cV);
        }

        // POST: CV/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("CV_ID,ID,PositionID,Skills,Resume,Status")] CV cV)
        {
            if (id != cV.CV_ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(cV);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CVExists(cV.CV_ID))
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
            ViewData["PositionID"] = new SelectList(_context.JobPosition, "PositionID", "PositionID", cV.PositionID);
            ViewData["ID"] = new SelectList(_context.User, "ID", "ID", cV.ID);
            return View(cV);
        }

        // GET: CV/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var cV = await _context.CV
                .Include(c => c.JobPosition)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.CV_ID == id);
            if (cV == null)
            {
                return NotFound();
            }

            return View(cV);
        }

        // POST: CV/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var cV = await _context.CV.FindAsync(id);
            if (cV != null)
            {
                _context.CV.Remove(cV);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CVExists(int id)
        {
            return _context.CV.Any(e => e.CV_ID == id);
        }


    }
}
