namespace QuanLyDoanhNghiep.Services
{
    public class JobExpirationService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<JobExpirationService> _logger;

        public JobExpirationService(
            IServiceProvider serviceProvider,
            ILogger<JobExpirationService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var jobNotificationService = scope.ServiceProvider.GetRequiredService<IJobNotificationService>();
                        await jobNotificationService.CheckAndNotifyExpiredJobs();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi khi kiểm tra việc làm hết hạn");
                }

                // Chạy mỗi ngày một lần vào lúc 00:00
                var now = DateTime.Now;
                var nextRun = now.Date.AddDays(1);
                var delay = nextRun - now;

                await Task.Delay(delay, stoppingToken);
            }
        }
    }
} 