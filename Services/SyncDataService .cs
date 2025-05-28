using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using QuanLyDoanhNghiep.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Threading.Tasks;

namespace QuanLyDoanhNghiep.Services
{
    public class SyncDataService : IHostedService, IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private Timer _timer;
        private readonly ILogger<SyncDataService> _logger;

        public SyncDataService(HttpClient httpClient, IServiceScopeFactory serviceScopeFactory, ILogger<SyncDataService> logger)
        {
            _httpClient = httpClient;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(PerformSync, null, TimeSpan.Zero, TimeSpan.FromMinutes(5)); // Đồng bộ mỗi 5 phút
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private async void PerformSync(object state)
        {
            try
            {
                // Tạo scope mới để lấy DbContext
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<QuanLyDoanhNghiepDBContext>();

                    // Lấy dữ liệu User từ API
                    var userResponse = await _httpClient.GetStringAsync("https://localhost:7000/api/Users");
                    var usersFromApi = JsonConvert.DeserializeObject<List<UserApiModel>>(userResponse);

                    foreach (var userApi in usersFromApi)
                    {
                        // Kiểm tra và thêm hoặc cập nhật Account
                        var existingAccount = await context.Account.SingleOrDefaultAsync(a => a.UserName == userApi.UserName);

                        if (existingAccount == null)
                        {
                            // Tạo tài khoản mới cho User nếu chưa có
                            var newAccount = new Account
                            {
                                UserName = userApi.UserName,
                                Password = userApi.Password,
                                Role = 2  // Gán giá trị Role là 2
                            };

                            context.Account.Add(newAccount);
                            await context.SaveChangesAsync(); // Lưu để có AccountID

                            // Gán AccountID cho User
                            userApi.AccountID = newAccount.AccountID;
                        }
                        else
                        {
                            userApi.AccountID = existingAccount.AccountID; // Dùng AccountID hiện tại nếu đã có
                        }

                        // Chuyển đổi từ UserApiModel sang User để lưu vào cơ sở dữ liệu
                        var user = new User
                        {
                            ID = userApi.ID,
                            FullName = userApi.FullName,
                            DateOfBirth = userApi.DateOfBirth,
                            Gender = userApi.Gender,
                            GraduationYear = userApi.GraduationYear,
                            GPA = userApi.GPA,
                            Honors = userApi.Honors,
                            Email = userApi.Email,
                            PhoneNumber = userApi.PhoneNumber,
                            Khoa = userApi.Khoa,
                            AccountID = (int)userApi.AccountID
                        };

                        // Kiểm tra và thêm hoặc cập nhật User
                        var existingUser = await context.User.FindAsync(user.ID);
                        if (existingUser != null)
                        {
                            // Nếu User đã tồn tại, cập nhật thông tin User
                            context.Entry(existingUser).CurrentValues.SetValues(user);
                        }
                        else
                        {
                            // Nếu User chưa tồn tại, thêm mới vào CSDL
                            context.User.Add(user);
                        }
                    }

                    // Lưu tất cả thay đổi vào cơ sở dữ liệu
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Đã xảy ra lỗi khi đồng bộ dữ liệu: {ex.Message}");
            }
        }


        // Phương thức đồng bộ dữ liệu thủ công
        public async Task SyncDataAsync()
        {
            // Tạo scope mới để lấy DbContext
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<QuanLyDoanhNghiepDBContext>();

                // Lấy dữ liệu User từ API
                var userResponse = await _httpClient.GetStringAsync("https://localhost:7000/api/Users");
                var usersFromApi = JsonConvert.DeserializeObject<List<UserApiModel>>(userResponse);

                foreach (var userApi in usersFromApi)
                {
                    // Kiểm tra xem User đã có Account chưa (dựa trên UserName)
                    var existingAccount = await context.Account.SingleOrDefaultAsync(a => a.UserName == userApi.UserName);

                    if (existingAccount == null)
                    {
                        // Nếu chưa có Account, tạo một Account mới
                        var newAccount = new Account
                        {
                            UserName = userApi.UserName,
                            Password = userApi.Password,
                            Role = 2  // Gán Role mặc định là 2
                        };

                        // Thêm Account vào CSDL
                        context.Account.Add(newAccount);
                        await context.SaveChangesAsync(); // Lưu để có AccountID

                        // Gán AccountID cho UserApiModel
                        userApi.AccountID = newAccount.AccountID;
                    }
                    else
                    {
                        // Nếu đã có Account, dùng AccountID hiện tại của Account đó
                        userApi.AccountID = existingAccount.AccountID;
                    }

                    // Chuyển đổi từ UserApiModel sang User (để lưu vào cơ sở dữ liệu)
                    var user = new User
                    {
                        ID = userApi.ID,
                        FullName = userApi.FullName,
                        DateOfBirth = userApi.DateOfBirth,
                        Gender = userApi.Gender,
                        GraduationYear = userApi.GraduationYear,
                        GPA = userApi.GPA,
                        Honors = userApi.Honors,
                        Email = userApi.Email,
                        PhoneNumber = userApi.PhoneNumber,
                        Khoa = userApi.Khoa,
                        AccountID = (int)userApi.AccountID
                    };

                    // Kiểm tra và thêm hoặc cập nhật User trong cơ sở dữ liệu
                    var existingUser = await context.User.FindAsync(user.ID);
                    if (existingUser != null)
                    {
                        // Nếu User đã tồn tại, cập nhật thông tin User
                        context.Entry(existingUser).CurrentValues.SetValues(user);
                    }
                    else
                    {
                        // Nếu User chưa tồn tại, thêm mới vào CSDL
                        context.User.Add(user);
                    }
                }

                // Lưu tất cả thay đổi vào cơ sở dữ liệu
                await context.SaveChangesAsync();
            }
        }

    }
}
