using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;

namespace QuanLyDoanhNghiep.Controllers
{
    public class NotificationController : BaseController
    {
        private readonly QuanLyDoanhNghiepDBContext _context;

        public NotificationController(QuanLyDoanhNghiepDBContext context)
        {
            _context = context;
        }

        // GET: Notification
        public async Task<IActionResult> Index()
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                if (RoleUser == "2")
                {
                    var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                    var notifications = await _context.Notification
                        .Where(n => 
                                (n.UserRole == "2" && n.UserID.Equals(user.ID))) // Thông báo cho sinh viên                           
                        .OrderByDescending(n => n.CreatedAt)
                        .ToListAsync();
                    return View(notifications);
                }
                if (RoleUser == "1")
                {
                   
                    var employee = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                    var notifications = await _context.Notification
                        .Where(n => n.UserRole == "1" && n.EmployeeID.Equals(employee.EmployeeID)) // Thông báo cho nhân viên
                        .OrderByDescending(n => n.CreatedAt)
                        .ToListAsync();
                    return View(notifications);
                }
                if (RoleUser == "0")
                {
                    var notifications = await _context.Notification
                        .Where(n => (n.UserRole == "0")) // Thông báo cho admin
                        .OrderByDescending(n => n.CreatedAt)
                        .ToListAsync();
                    return View(notifications);
                }
            }
            return View();
        }

        // GET: Notification/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            if (!IsLogin)
                return Json(new { count = 0 });

            string realId = null;
            if (RoleUser == "2")
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                if (user != null) realId = user.ID;
                else return Json(new { count = 0 });
            }
            else if (RoleUser == "1")
            {
                var emp = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                if (emp != null) realId = emp.EmployeeID;
                else return Json(new { count = 0 });
            }

            var countNotification = await _context.Notification
                .CountAsync(n => n.UserRole == RoleUser &&
                                 !n.IsRead &&
                                 ((n.UserRole == "2" && n.UserID == realId) ||
                                  (n.UserRole == "1" && n.EmployeeID == realId) ||
                                  (n.UserRole == "0")));
            return Json(new { count = countNotification });
        }


        // GET: Notification/GetLatestNotifications
        [HttpGet]
        public async Task<IActionResult> GetLatestNotifications()
        {
            if (!IsLogin)
                return Json(new List<object>());

            string realId = null;
            if (RoleUser == "2")
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                if (user != null) realId = user.ID;
                else return Json(new List<object>());
            }
            else if (RoleUser == "1")
            {
                var emp = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                if (emp != null) realId = emp.EmployeeID;
                else return Json(new List<object>());
            }

            var notifications = await _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                            !n.IsRead &&
                            ((n.UserRole == "2" && n.UserID == realId) ||
                             (n.UserRole == "1" && n.EmployeeID == realId) ||
                             (n.UserRole == "0")))
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    notificationID = n.NotificationID,
                    message = n.Message,
                    createdAt = n.CreatedAt,
                    notificationPath = n.NotificationPath
                })
                .ToListAsync();

            return Json(notifications);
        }

        // POST: Notification/MarkAsRead/{id}
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!IsLogin)
            {
                return Json(new { success = false });
            }

            if (!int.TryParse(CurrentID, out int currentID))
            {
                return Json(new { success = false });
            }

            var notification = await _context.Notification
                .FirstOrDefaultAsync(n => n.NotificationID == id &&
                                        n.UserRole == RoleUser &&
                                        ((n.UserRole == "2" && n.UserID.Equals(currentID)) ||
                                         (n.UserRole == "1" && n.EmployeeID.Equals(currentID)) ||
                                         (n.UserRole == "0")));

            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // POST: Notification/MarkAllAsRead
        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            if (!IsLogin)
            {
                return Json(new { success = false });
            }

            if (!int.TryParse(CurrentID, out int currentID))
            {
                return Json(new { success = false });
            }

            var notifications = await _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                           !n.IsRead &&
                           ((n.UserRole == "2" && n.UserID.Equals(currentID)) ||
                            (n.UserRole == "1" && n.EmployeeID.Equals(currentID)) ||
                            (n.UserRole == "0")))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: Notification/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            if (!IsLogin)
            {
                return Json(new { success = false });
            }

            if (!int.TryParse(CurrentID, out int currentID))
            {
                return Json(new { success = false });
            }

            var notification = await _context.Notification
                .FirstOrDefaultAsync(n => n.NotificationID == id &&
                                        n.UserRole == RoleUser &&
                                        ((n.UserRole == "2" && n.UserID.Equals(currentID)) ||
                                         (n.UserRole == "1" && n.EmployeeID.Equals(currentID)) ||
                                         (n.UserRole == "0")));

            if (notification != null)
            {
                _context.Notification.Remove(notification);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // POST: Notification/DeleteAll
        [HttpPost]
        public async Task<IActionResult> DeleteAll()
        {
            if (!IsLogin)
            {
                return Json(new { success = false });
            }

            if (!int.TryParse(CurrentID, out int currentID))
            {
                return Json(new { success = false });
            }

            var notifications = await _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                           ((n.UserRole == "2" && n.UserID.Equals(currentID)) ||
                            (n.UserRole == "1" && n.EmployeeID.Equals(currentID)) ||
                            (n.UserRole == "0")))
                .ToListAsync();

            _context.Notification.RemoveRange(notifications);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // GET: Notification/GetNewNotifications
        public async Task<IActionResult> GetNewNotifications()
        {
            if (!IsLogin)
            {
                return Json(new List<object>());
            }

            if (!int.TryParse(CurrentID, out int currentID))
            {
                return Json(new List<object>());
            }

            // Lấy thông báo mới (chưa đọc) và thông báo mới nhất đã đọc
            var notifications = await _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                           ((n.UserRole == "2" && n.UserID.Equals(currentID)) ||
                            (n.UserRole == "1" && n.EmployeeID.Equals(currentID)) ||
                            (n.UserRole == "0")))
                .OrderByDescending(n => n.CreatedAt)
                .Take(10) // Lấy 10 thông báo mới nhất
                .Select(n => new
                {
                    notificationID = n.NotificationID,
                    message = n.Message,
                    createdAt = n.CreatedAt,
                    isRead = n.IsRead,
                    notificationPath = n.NotificationPath
                })
                .ToListAsync();

            return Json(notifications);
        }

        // GET: Notification/GetUnreadNotifications
        [HttpGet]
        public async Task<IActionResult> GetUnreadNotifications()
        {
            if (!IsLogin || !int.TryParse(CurrentID, out int currentID))
                return Json(new List<object>());

            var notifications = await _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                            !n.IsRead &&
                            ((n.UserRole == "2" && n.UserID.Equals(currentID)) ||
                            (n.UserRole == "1" && n.EmployeeID.Equals(currentID)) ||
                            (n.UserRole == "0")))
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new
                {
                    notificationID = n.NotificationID,
                    message = n.Message,
                    createdAt = n.CreatedAt,
                    notificationPath = n.NotificationPath
                })
                .ToListAsync();

            return Json(notifications);
        }

        public async Task<IActionResult> ViewAndMarkAsRead(int id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }

            if (!int.TryParse(CurrentID, out int currentID))
            {
                return RedirectToAction("Login", "Account");
            }

            // Tìm thông báo dựa trên ID và quyền truy cập
            var notification = await _context.Notification
                .FirstOrDefaultAsync(n => n.NotificationID == id);

            // Kiểm tra quyền truy cập
            if (notification == null || notification.UserRole != RoleUser)
            {
                TempData["ErrorMessage"] = "Thông báo không tồn tại hoặc bạn không có quyền truy cập.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra quyền truy cập chi tiết dựa trên loại người dùng
            bool hasAccess = false;
            if (RoleUser == "2") // Sinh viên
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID == currentID);
                hasAccess = user != null && notification.UserID == user.ID;
            }
            else if (RoleUser == "1") // Nhân viên
            {
                var employee = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID == currentID);
                hasAccess = employee != null && notification.EmployeeID == employee.EmployeeID;
            }
            else if (RoleUser == "0") // Admin
            {
                hasAccess = true;
            }

            if (!hasAccess)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền truy cập thông báo này.";
                return RedirectToAction(nameof(Index));
            }

            // Đánh dấu thông báo đã đọc
            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // Chuyển hướng đến đường dẫn trong thông báo
            if (!string.IsNullOrEmpty(notification.NotificationPath))
            {
                // Kiểm tra xem đường dẫn có hợp lệ không
                if (notification.NotificationPath.StartsWith("/"))
                {
                    return Redirect(notification.NotificationPath);
                }
                else
                {
                    // Nếu đường dẫn không bắt đầu bằng /, thêm / vào đầu
                    return Redirect("/" + notification.NotificationPath);
                }
            }

            // Nếu không có đường dẫn, quay về trang thông báo
            TempData["InfoMessage"] = "Thông báo đã được đánh dấu là đã đọc.";
            return RedirectToAction(nameof(Index));
        }
    }
}