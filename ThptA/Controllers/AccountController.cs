
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ThptA.Models;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;

        public AccountController(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // 1. Hiển thị trang đăng nhập
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new LoginVm());
        }

        // 2. Xử lý thông tin đăng nhập
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByNameAsync(
                model.UserName.Trim());

            // Từ chối tài khoản không tồn tại hoặc bị khóa
            if (user == null || !user.IsActive)
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập hoặc mật khẩu không hợp lệ."
                );

                return View(model);
            }

            // Kiểm tra mật khẩu bằng ASP.NET Core Identity
            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                isPersistent: false,
                lockoutOnFailure: true
            );

            if (result.Succeeded)
            {
                TempData["Success"] = "Đăng nhập thành công!";

                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(
                    "",
                    "Tài khoản đang bị khóa tạm thời do đăng nhập sai nhiều lần."
                );
            }
            else
            {
                ModelState.AddModelError(
                    "",
                    "Tên đăng nhập hoặc mật khẩu không hợp lệ."
                );
            }

            return View(model);
        }

        // 3. Đăng xuất
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();

            return RedirectToAction("Login", "Account");
        }

        // 4. Trang thông báo không có quyền truy cập
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Denied()
        {
            return View();
        }
    }
}
