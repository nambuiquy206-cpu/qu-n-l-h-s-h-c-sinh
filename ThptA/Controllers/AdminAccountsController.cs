
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThptA.Models;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminAccountsController : Controller
    {
        private readonly UserManager<AppUser> _userManager;

        public AdminAccountsController(
            UserManager<AppUser> userManager)
        {
            _userManager = userManager;
        }

        // 1. Danh sách tài khoản giáo viên
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var teachers = await _userManager
                .GetUsersInRoleAsync("Teacher");

            var sortedTeachers = teachers
                .OrderBy(x => x.FullName)
                .ToList();

            return View(sortedTeachers);
        }

        // 2. Hiển thị form tạo giáo viên
        [HttpGet]
        public IActionResult Create()
        {
            return View(new TeacherAccountVm());
        }

        // 3. Xử lý tạo tài khoản giáo viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TeacherAccountVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var userName = model.UserName.Trim();

            var existingUser = await _userManager
                .FindByNameAsync(userName);

            if (existingUser != null)
            {
                ModelState.AddModelError(
                    nameof(model.UserName),
                    "Tên đăng nhập đã tồn tại.");

                return View(model);
            }

            var teacher = new AppUser
            {
                UserName = userName,
                FullName = model.FullName.Trim(),
                IsActive = true
            };

            var createResult = await _userManager
                .CreateAsync(teacher, model.Password);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                {
                    ModelState.AddModelError(
                        "", error.Description);
                }

                return View(model);
            }

            var roleResult = await _userManager
                .AddToRoleAsync(teacher, "Teacher");

            if (!roleResult.Succeeded)
            {
                // Tránh để lại tài khoản vừa tạo
                // nhưng chưa được gán vai trò.
                await _userManager.DeleteAsync(teacher);

                ModelState.AddModelError(
                    "",
                    "Không thể gán vai trò Teacher. " +
                    "Vui lòng kiểm tra cấu hình Identity.");

                return View(model);
            }

            TempData["Success"] =
                "Tạo tài khoản giáo viên thành công.";

            return RedirectToAction(nameof(Index));
        }

        // Hiển thị trang sửa họ tên giáo viên
        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var teacher = await _userManager.FindByIdAsync(id);

            if (teacher == null ||
                !await _userManager.IsInRoleAsync(teacher, "Teacher"))
            {
                return NotFound();
            }

            var model = new TeacherEditVm
            {
                Id = teacher.Id,
                FullName = teacher.FullName
            };

            return View(model);
        }

        // Lưu họ tên mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TeacherEditVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var teacher = await _userManager.FindByIdAsync(model.Id);

            if (teacher == null ||
                !await _userManager.IsInRoleAsync(teacher, "Teacher"))
            {
                return NotFound();
            }

            teacher.FullName = model.FullName.Trim();

            var result = await _userManager.UpdateAsync(teacher);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);

                return View(model);
            }

            TempData["Success"] = "Cập nhật họ tên thành công.";

            return RedirectToAction(nameof(Index));
        }

        // Khóa hoặc mở khóa tài khoản giáo viên
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetStatus(
            string id, bool activate)
        {
            if (string.IsNullOrWhiteSpace(id))
                return NotFound();

            var teacher = await _userManager.FindByIdAsync(id);

            // Chỉ cho phép thay đổi tài khoản Teacher
            if (teacher == null ||
                !await _userManager.IsInRoleAsync(teacher, "Teacher"))
            {
                return NotFound();
            }

            if (teacher.IsActive == activate)
                return RedirectToAction(nameof(Index));

            teacher.IsActive = activate;

            // Lưu trạng thái và đổi SecurityStamp.
            // Cookie cũ sẽ mất hiệu lực khi được xác thực lại.
            var result = await _userManager
                .UpdateSecurityStampAsync(teacher);

            if (!result.Succeeded)
            {
                TempData["Error"] =
                    "Không thể thay đổi trạng thái tài khoản.";

                return RedirectToAction(nameof(Index));
            }

            TempData["Success"] = activate
                ? "Đã mở khóa tài khoản giáo viên."
                : "Đã khóa tài khoản giáo viên.";

            return RedirectToAction(nameof(Index));
        }

    }
}
