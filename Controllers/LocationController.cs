using Microsoft.AspNetCore.Mvc;
using QuanLyDoanhNghiep.Models;
using QuanLyDoanhNghiep.Services;
using System.Threading.Tasks;

namespace QuanLyDoanhNghiep.Controllers
{
    public class LocationController : Controller
    {
        private readonly ILocationService _locationService;
        private readonly QuanLyDoanhNghiepDBContext _context; 

        public LocationController(ILocationService locationService, QuanLyDoanhNghiepDBContext context)
        {
            _locationService = locationService;
            _context = context;
        }

        public async Task<IActionResult> FetchAllLocations()
        {
            //await _locationService.FetchAndSaveProvincesAsync();
            //await _locationService.FetchAndSaveDistrictsAsync();
            await _locationService.FetchAndSaveWardsAsync();
            return Ok("Dữ liệu tỉnh/thành, quận/huyện, xã/phường đã được cập nhật thành công!");
        }

        public IActionResult GetProvince()
        {
            var provinces = _context.Province.ToList();
            return View(provinces);
        }

        public IActionResult GetDistricts(string provinceId)
        {
            var districts = _context.District.Where(m=> m.ProvinceID == provinceId).ToList();
            return Json(districts);
        }
    }
}
