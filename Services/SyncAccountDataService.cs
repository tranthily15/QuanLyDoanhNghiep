//using Microsoft.EntityFrameworkCore;
//using Newtonsoft.Json;
//using QuanLyDoanhNghiep.Models;

//namespace QuanLyDoanhNghiep.Service
//{
//    public class SyncAccountDataService : IHostedService, IDisposable
//    {
//        private readonly HttpClient _httpClient;
//        private readonly QuanLyDoanhNghiepDBContext _context;
//        private Timer _timer;

//        public SyncAccountDataService(HttpClient httpClient, QuanLyDoanhNghiepDBContext context)
//        {
//            _httpClient = httpClient;
//            _context = context;
//        }

//        public Task StartAsync(CancellationToken cancellationToken)
//        {
//            // Set timer để chạy mỗi 5 phút (hoặc thời gian khác mà bạn muốn)
//            _timer = new Timer(PerformSync, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
//            return Task.CompletedTask;
//        }

//        public Task StopAsync(CancellationToken cancellationToken)
//        {
//            _timer?.Change(Timeout.Infinite, 0);
//            return Task.CompletedTask;
//        }

//        public void Dispose()
//        {
//            _timer?.Dispose();
//        }

//        private async void PerformSync(object state)
//        {
//            // Lấy dữ liệu Account từ API
//            var accountResponse = await _httpClient.GetStringAsync("https://localhost:7000/api/Accounts");
//            var accountsFromApi = JsonConvert.DeserializeObject<List<Account>>(accountResponse);

//            foreach (var account in accountsFromApi)
//            {
//                // Kiểm tra xem account đã tồn tại trong CSDL chưa
//                var existingAccount = await _context.Account
//                                                     .FirstOrDefaultAsync(a => a.AccountID == account.AccountID);

//                if (existingAccount == null)
//                {
//                    // Nếu chưa tồn tại, thêm mới
//                    _context.Account.Add(account);
//                }
//                else
//                {
//                    // Nếu đã tồn tại, kiểm tra và cập nhật nếu có thay đổi
//                    if (existingAccount.Username != account.Username ||
//                        existingAccount.Password != account.Password )
                       
//                    {
//                        // Cập nhật dữ liệu nếu có sự thay đổi
//                        existingAccount.Username = account.Username;
//                        existingAccount.Password = account.Password;
//                    }
//                }
//            }

//            // Lưu các thay đổi vào cơ sở dữ liệu
//            await _context.SaveChangesAsync();
//        }
//    }

//}
