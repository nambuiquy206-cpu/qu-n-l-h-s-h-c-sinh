
using Microsoft.AspNetCore.Identity;
using ThptA.Models;

namespace ThptA.Services
{
    public static class IdentitySeeder
    {
        public static async Task SeedAsync(
            IServiceProvider services)
        {
            var roleManager = services
                .GetRequiredService<RoleManager<IdentityRole>>();

            var userManager = services
                .GetRequiredService<UserManager<AppUser>>();

            // 1. Tạo hai vai trò Admin và Teacher
            string[] roles = { "Admin", "Teacher" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var result = await roleManager.CreateAsync(
                        new IdentityRole(role));

                    if (!result.Succeeded)
                    {
                        throw new Exception(
                            $"Không thể tạo vai trò {role}: " +
                            string.Join("; ",
                                result.Errors.Select(e => e.Description)));
                    }
                }
            }

            // 2. Kiểm tra tài khoản admin01
            var admin = await userManager.FindByNameAsync("admin01");

            if (admin != null)
            {
                if (!await userManager.IsInRoleAsync(admin, "Admin"))
                {
                    throw new Exception(
                        "admin01 tồn tại nhưng không có vai trò Admin.");
                }

                return;
            }

            // 3. Lấy mật khẩu từ biến môi trường
            var password = Environment.GetEnvironmentVariable(
                "THPTA_ADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(password))
            {
                throw new Exception(
                    "Chưa cấu hình THPTA_ADMIN_PASSWORD.");
            }

            // 4. Tạo tài khoản quản trị viên
            admin = new AppUser
            {
                UserName = "admin01",
                FullName = "Quản trị viên hệ thống",
                IsActive = true
            };

            var createResult = await userManager.CreateAsync(
                admin, password);

            if (!createResult.Succeeded)
            {
                throw new Exception(
                    "Không thể tạo tài khoản Admin: " +
                    string.Join("; ",
                        createResult.Errors.Select(e => e.Description)));
            }

            // 5. Gán vai trò Admin
            var roleResult = await userManager.AddToRoleAsync(
                admin, "Admin");

            if (!roleResult.Succeeded)
            {
                throw new Exception(
                    "Không thể gán vai trò Admin: " +
                    string.Join("; ",
                        roleResult.Errors.Select(e => e.Description)));
            }

            Console.WriteLine(
                "Đã tạo tài khoản admin01 thành công.");
        }
    }
}
