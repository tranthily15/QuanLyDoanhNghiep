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
                        .Where(n => (n.UserRole == "2" && n.UserID.Equals(user.ID))) // Thông báo cho sinh viên                           
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

            string realId = null;
            if (RoleUser == "2")
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                if (user != null) realId = user.ID;
                else return Json(new { success = false });
            }
            else if (RoleUser == "1")
            {
                var emp = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                if (emp != null) realId = emp.EmployeeID;
                else return Json(new { success = false });
            }

            var notification = await _context.Notification
                .FirstOrDefaultAsync(n => n.NotificationID == id &&
                                        n.UserRole == RoleUser &&
                                        ((n.UserRole == "2" && n.UserID == realId) ||
                                         (n.UserRole == "1" && n.EmployeeID == realId) ||
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

            string realId = null;
            if (RoleUser == "2")
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                if (user != null) realId = user.ID;
                else return Json(new { success = false });
            }
            else if (RoleUser == "1")
            {
                var emp = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                if (emp != null) realId = emp.EmployeeID;
                else return Json(new { success = false });
            }

            var notifications = await _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                           !n.IsRead &&
                           ((n.UserRole == "2" && n.UserID == realId) ||
                            (n.UserRole == "1" && n.EmployeeID == realId) ||
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
            var notification = await _context.Notification.FindAsync(id);
            if (notification == null)
                return Json(new { success = false });

            _context.Notification.Remove(notification);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // POST: Notification/DeleteAll
        [HttpPost]
        public async Task<IActionResult> DeleteAll()
        {
            if (!IsLogin)
            {
                return Json(new { success = false });
            }

            string realId = null;
            if (RoleUser == "2")
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                if (user != null) realId = user.ID;
                else return Json(new { success = false });
            }
            else if (RoleUser == "1")
            {
                var emp = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                if (emp != null) realId = emp.EmployeeID;
                else return Json(new { success = false });
            }

            var notifications = await _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                   ((n.UserRole == "2" && n.UserID == realId) ||
                    (n.UserRole == "1" && n.EmployeeID == realId) ||
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

        // GET: Notification/ViewAndMarkAsRead/{id}
        public async Task<IActionResult> ViewAndMarkAsRead(int id)
        {
            if (!IsLogin)
            {
                return RedirectToAction("Login", "Account");
            }
            var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
            var employee = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
            var notification = await _context.Notification
                .FirstOrDefaultAsync(n => n.NotificationID == id &&
                                        n.UserRole == RoleUser &&
                                        ((n.UserRole == "2" && n.UserID.Equals(user.ID)) ||
                                         (n.UserRole == "1" && n.EmployeeID.Equals(employee.EmployeeID)) ||
                                         (n.UserRole == "0")));

            if (notification == null)
            {
                return NotFound();
            }

            // Đánh dấu thông báo đã đọc
            notification.IsRead = true;
            await _context.SaveChangesAsync();

            // Chuyển hướng đến đường dẫn trong thông báo
            if (!string.IsNullOrEmpty(notification.NotificationPath))
            {
                // Kiểm tra xem đường dẫn có bắt đầu bằng / không
                string path = notification.NotificationPath;
                if (!path.StartsWith("/"))
                {
                    path = "/" + path;
                }
                return Redirect(path);
            }

            // Nếu không có đường dẫn, quay về trang thông báo
            return RedirectToAction(nameof(Index));
        }

        // GET: Notification/GetPagedNotifications
        [HttpGet]
        public async Task<IActionResult> GetPagedNotifications(int skip = 0, int take = 5)
        {
            if (!IsLogin)
                return Json(new { notifications = new object[0], hasMore = false });

            string realId = null;
            if (RoleUser == "2")
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                if (user != null) realId = user.ID;
                else return Json(new { notifications = new object[0], hasMore = false });
            }
            else if (RoleUser == "1")
            {
                var emp = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                if (emp != null) realId = emp.EmployeeID;
                else return Json(new { notifications = new object[0], hasMore = false });
            }

            var query = _context.Notification
                .Where(n => n.UserRole == RoleUser &&
                            ((n.UserRole == "2" && n.UserID == realId) ||
                             (n.UserRole == "1" && n.EmployeeID == realId) ||
                             (n.UserRole == "0")))
                .OrderBy(n => n.IsRead).ThenByDescending(n => n.CreatedAt);

            var notifications = await query
                .Skip(skip)
                .Take(take)
                .Select(n => new
                {
                    notificationID = n.NotificationID,
                    message = n.Message,
                    createdAt = n.CreatedAt,
                    isRead = n.IsRead,
                    notificationPath = n.NotificationPath
                })
                .ToListAsync();

            var totalCount = await query.CountAsync();
            var hasMore = skip + take < totalCount;

            return Json(new { notifications, hasMore });
        }

        // GET: Notification/GetAllNotifications
        [HttpGet]
        public async Task<IActionResult> GetAllNotifications()
        {
            if (!IsLogin)
                return Json(new { notifications = new object[0] });

            IQueryable<Notification> query = _context.Notification.AsQueryable();

            if (RoleUser == "2")
            {
                var user = await _context.User.FirstOrDefaultAsync(u => u.AccountID.ToString().Equals(CurrentID));
                if (user == null) return Json(new { notifications = new object[0] });
                query = query.Where(n => n.UserRole == "2" && n.UserID == user.ID);
            }
            else if (RoleUser == "1")
            {
                var emp = await _context.Employee.FirstOrDefaultAsync(e => e.AccountID.ToString().Equals(CurrentID));
                if (emp == null) return Json(new { notifications = new object[0] });
                query = query.Where(n => n.UserRole == "1" && n.EmployeeID == emp.EmployeeID);
            }
            else if (RoleUser == "0")
            {
                // Admin: lấy tất cả thông báo dành cho admin
                query = query.Where(n => n.UserRole == "0");
            }

            var notifications = await query
                .OrderBy(n => n.IsRead)
                .ThenByDescending(n => n.CreatedAt)
                .Select(n => new
                {
                    notificationID = n.NotificationID,
                    message = n.Message,
                    createdAt = n.CreatedAt,
                    isRead = n.IsRead,
                    notificationPath = n.NotificationPath
                })
                .ToListAsync();

            return Json(new { notifications });
        }
    }
}