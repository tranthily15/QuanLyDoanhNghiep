using Microsoft.EntityFrameworkCore;
using QuanLyDoanhNghiep.Models;
using QuanLyDoanhNghiep.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//------------------
// Thiết lập dịch vụ kết nối vs database
var connectionString = builder.Configuration.GetConnectionString("QLDoanhNghiepDB");
builder.Services.AddDbContext<QuanLyDoanhNghiepDBContext>(options =>
    options.UseSqlServer(connectionString));
//---------------------
// Thiết lập sử dụng API

builder.Services.AddHttpClient();
//builder.Services.AddHostedService<SyncDataService>();
builder.Services.AddScoped<SyncDataService>();
builder.Services.AddScoped<ILocationService, LocationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJobNotificationService, JobNotificationService>();
builder.Services.AddHostedService<JobExpirationService>();

// Khai báo sử dụng session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromSeconds(18000); // Thời gian session
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

// Đăng ký dịch vụ đồng bộ dữ liệu người dùng và tài khoản từ API

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthorization();

// Thiết lập định tuyến mặc định cho controller
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=JobPosition}/{action=Index}/{id?}");
app.Run();
