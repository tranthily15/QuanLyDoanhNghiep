using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using QuanLyDoanhNghiep.Models;
using QuanLyDoanhNghiep.Services;

namespace QuanLyDoanhNghiep.Services
{
    public class LocationService : ILocationService
    {
        private readonly QuanLyDoanhNghiepDBContext _context;
        private readonly HttpClient _httpClient;

        public LocationService(QuanLyDoanhNghiepDBContext context, HttpClient httpClient)
        {
            _context = context;
            _httpClient = httpClient;
        }

        //  Lấy danh sách Tỉnh/Thành phố
        public async Task FetchAndSaveProvincesAsync()
        {
            string apiUrl = "https://vn-public-apis.fpo.vn/provinces/getAll?limit=-1";
            try
            {
                var response = await _httpClient.GetStringAsync(apiUrl);
                Console.WriteLine($" API Response: {response}");

                var data = JsonSerializer.Deserialize<ApiResponse<Province>>(response);

                if (data?.Data?.Data != null)
                {
                    foreach (var province in data.Data.Data)
                    {
                        var existingProvince = _context.Province.FirstOrDefault(p => p.ProvinceID == province.ProvinceID);
                        if (existingProvince == null)
                        {
                            _context.Province.Add(province);
                        }
                    }
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($" Lỗi khi lấy dữ liệu Tỉnh/Thành phố: {ex.Message}");
            }
        }

        //  Lấy danh sách Quận/Huyện
        public async Task FetchAndSaveDistrictsAsync()
        {
            string apiUrl = "https://vn-public-apis.fpo.vn/districts/getAll?limit=-1";
            var response = await _httpClient.GetStringAsync(apiUrl);
            Console.WriteLine($" API Response: {response}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<ApiResponse<District>>(response, options);

            if (data == null || data.Exitcode != 1 || data.Data?.Data == null)
            {
                Console.WriteLine(" API không trả về dữ liệu District!");
                return;
            }

            foreach (var district in data.Data.Data)
            {
                Console.WriteLine($" District: {district.DistrictName}, Parent Code: {district.ProvinceID}");

                bool provinceExists = _context.Province.Any(p => p.ProvinceID == district.ProvinceID);
                if (!provinceExists)
                {
                    Console.WriteLine($" Bỏ qua {district.DistrictName} - ProvinceID {district.ProvinceID} không tồn tại");
                    continue;
                }

                var existingDistrict = _context.District.FirstOrDefault(d => d.DistrictID == district.DistrictID);
                if (existingDistrict == null)
                {
                    _context.District.Add(new District
                    {
                        DistrictID = district.DistrictID,
                        DistrictName = district.DistrictName,
                        //Type = district.Type,
                        //NameWithType = district.NameWithType,
                        ProvinceID = district.ProvinceID
                    });
                }
                else
                {
                    existingDistrict.DistrictName = district.DistrictName;
                    existingDistrict.ProvinceID = district.ProvinceID;
                }
            }
            await _context.SaveChangesAsync();
            Console.WriteLine(" Đã lưu dữ liệu District!");
        }


        //  Lấy danh sách Xã/Phường
        public async Task FetchAndSaveWardsAsync()
        {
            string apiUrl = "https://vn-public-apis.fpo.vn/wards/getAll?limit=-1";
            var response = await _httpClient.GetStringAsync(apiUrl);
            Console.WriteLine($"API Response: {response}");

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var data = JsonSerializer.Deserialize<ApiResponse<Ward>>(response, options);

            if (data == null || data.Exitcode != 1 || data.Data?.Data == null)
            {
                Console.WriteLine(" API không trả về dữ liệu District!");
                return;
            }

            foreach (var ward in data.Data.Data)
            {
                Console.WriteLine($" Ward: {ward.WardName}, Parent Code: {ward.DistrictID}");

                bool provinceExists = _context.District.Any(p => p.DistrictID == ward.DistrictID);
                if (!provinceExists)
                {
                    Console.WriteLine($" Bỏ qua {ward.WardName} - ProvinceID {ward.DistrictID} không tồn tại");
                    continue;
                }

                var existingWard = _context.Ward.FirstOrDefault(d => d.DistrictID == ward.DistrictID);
                if (existingWard == null)
                {
                    _context.Ward.Add(new Ward
                    {
                        WardID = ward.WardID,
                        WardName = ward.WardName,
                        DistrictID = ward.DistrictID
                    });
                }
                else
                {
                    existingWard.WardName = ward.WardName;
                    existingWard.DistrictID = ward.DistrictID;
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine(" Đã lưu dữ liệu Ward!");
        }

    }
}
