
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThptA.Data;
using ThptA.Models;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentAccountsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public StudentAccountsController(
            AppDbContext db,
            UserManager<AppUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // Mở giao diện cấp tài khoản học sinh
        [HttpGet]
        public async Task<IActionResult> Create(int studentId)
        {
            var student = await _db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound();

            bool hasAccount = await _db.Users
                .AnyAsync(u => u.StudentId == studentId);

            if (hasAccount)
            {
                TempData["Error"] =
                    "Học sinh này đã có tài khoản đăng nhập.";

                return RedirectToAction(
                    "Details", "Students",
                    new { id = studentId });
            }

            return View(new StudentAccountCreateVm
            {
                StudentId = student.Id,
                StudentDisplay =
                    $"{student.Code} - {student.FullName}",
                UserName = student.Code.ToLowerInvariant()
            });
        }

        // Tạo tài khoản và liên kết với học sinh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            StudentAccountCreateVm model)
        {
            var student = await _db.Students
                .FirstOrDefaultAsync(s =>
                    s.Id == model.StudentId);

            if (student == null)
                return NotFound();

            // Thông tin học sinh luôn lấy từ database
            model.StudentDisplay =
                $"{student.Code} - {student.FullName}";

            model.UserName =
                student.Code.ToLowerInvariant();

            bool hasAccount = await _db.Users
                .AnyAsync(u =>
                    u.StudentId == student.Id);

            if (hasAccount)
            {
                ModelState.AddModelError(
                    "",
                    "Học sinh đã có tài khoản.");
            }

            if (await _userManager.FindByNameAsync(
                    model.UserName) != null)
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập đã tồn tại.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Đảm bảo hệ thống có vai trò Student
            if (!await _roleManager.RoleExistsAsync("Student"))
            {
                var roleResult =
                    await _roleManager.CreateAsync(
                        new IdentityRole("Student"));

                if (!roleResult.Succeeded)
                {
                    foreach (var error in roleResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }
            }

            await using var transaction =
                await _db.Database.BeginTransactionAsync();

            var user = new AppUser
            {
                UserName = model.UserName,
                FullName = student.FullName,
                IsActive = true,
                StudentId = student.Id
            };

            // Identity tự mã hóa mật khẩu
            var createResult =
                await _userManager.CreateAsync(
                    user, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(
                        "", error.Description);

                return View(model);
            }

            // Phân quyền Student
            var addRoleResult =
                await _userManager.AddToRoleAsync(
                    user, "Student");

            if (!addRoleResult.Succeeded)
            {
                foreach (var error in addRoleResult.Errors)
                    ModelState.AddModelError(
                        "", error.Description);

                return View(model);
            }

            await transaction.CommitAsync();

            TempData["Success"] =
                $"Đã tạo tài khoản {model.UserName} " +
                $"cho học sinh {student.FullName}.";

            return RedirectToAction(
                "Details", "Students",
                new { id = student.Id });
        }
    }
}
