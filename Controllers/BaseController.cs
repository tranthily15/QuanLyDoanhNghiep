using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace QuanLyDoanhNghiep.Controllers
{
    public class BaseController : Controller
    {
        public string CurrentUser
        {
            get
            {
                // Đọc từ sesion 
                return HttpContext.Session.GetString("USERNAME");
            }
            set
            {
                // Gán dữ liệu cho session
                HttpContext.Session.SetString("USERNAME", value);
            }
        }
        public string CurrentID
        {
            get
            {
                // Đọc từ sesion 
                return HttpContext.Session.GetString("ACCOUNTID");
            }
            set
            {
                // Gán dữ liệu cho session
                HttpContext.Session.SetString("ACCOUNTID", value);
            }
        }
        public string CurrentCompanyID
        {
            get
            {
                // Đọc từ sesion 
                return HttpContext.Session.GetString("COMPANYID");
            }
            set
            {
                // Gán dữ liệu cho session
                HttpContext.Session.SetString("COMPANYID", value);
            }
        }
        public string RoleUser
        {
            get
            {
                // Doc tu session
                return HttpContext.Session.GetString("ROLE");
            }
            set
            {
                HttpContext.Session.SetString("ROLE", value);
            }
        }
        public bool IsLogin
        {
            get
            {
                return !string.IsNullOrEmpty(CurrentUser);
            }
        }
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            ViewBag.IsLogin = IsLogin;
            ViewBag.Role = RoleUser;
            ViewBag.CurrentUser = CurrentUser;
            ViewBag.CurrentCompanyID = CurrentCompanyID;
            base.OnActionExecuted(filterContext);
        }
    }
}
