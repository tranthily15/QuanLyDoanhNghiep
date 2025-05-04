namespace QuanLyDoanhNghiep.Services
{
    public interface ILocationService
    {
        Task FetchAndSaveProvincesAsync();
        Task FetchAndSaveDistrictsAsync();
        Task FetchAndSaveWardsAsync();
    }
}
