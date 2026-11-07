
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ThptA.Data;
using ThptA.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký ASP.NET Core MVC
builder.Services.AddControllersWithViews();

// 2. Kết nối SQL Server LocalDB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default")
    )
);

// 3. Cấu hình hệ thống tài khoản Identity
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Quy định mật khẩu
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // Khóa tạm khi nhập sai quá nhiều
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(10);

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    options.User.RequireUniqueEmail = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// 4. Cấu hình Cookie đăng nhập
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/Denied";

    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;
});

var app = builder.Build();

// 5. Xử lý lỗi hệ thống
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// 6. Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Xác thực người dùng
app.UseAuthentication();

// Kiểm tra quyền
app.UseAuthorization();

// 7. Cấu hình đường dẫn mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"
);

// 8. Chạy website
app.Run();
