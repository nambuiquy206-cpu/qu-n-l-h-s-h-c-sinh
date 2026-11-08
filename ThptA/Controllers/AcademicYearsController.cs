
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
    public class AcademicYearsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public AcademicYearsController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Danh sách năm học
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var years = await _db.AcademicYears
                .AsNoTracking()
                .OrderByDescending(y => y.Code)
                .ToListAsync();

            return View(years);
        }

        // Hiển thị form thêm năm học
        [HttpGet]
        public IActionResult Create()
        {
            return View(new AcademicYearCreateVm());
        }

        // Lưu năm học mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            AcademicYearCreateVm model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var code = model.Code.Trim();

            var start = model.StartDate!.Value.Date;
            var end = model.EndDate!.Value.Date;

            var parts = code.Split('-');

            if (!int.TryParse(parts[0], out int firstYear) ||
                !int.TryParse(parts[1], out int lastYear) ||
                lastYear != firstYear + 1 ||
                start.Year != firstYear ||
                end.Year != lastYear ||
                end <= start)
            {
                ModelState.AddModelError(
                    "",
                    "Mã năm học và ngày bắt đầu, kết thúc không hợp lệ.");

                return View(model);
            }

            var exists = await _db.AcademicYears
                .AnyAsync(y => y.Code == code);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Code),
                    "Năm học này đã tồn tại.");

                return View(model);
            }

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            _db.AcademicYears.Add(new AcademicYear
            {
                Code = code,
                StartDate = start,
                EndDate = end
            });

            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "CREATE_ACADEMIC_YEAR",
                Target = code,
                AtUtc = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể lưu năm học. " +
                    "Vui lòng kiểm tra dữ liệu hoặc mã trùng.");

                return View(model);
            }

            TempData["Success"] =
                "Thêm năm học thành công.";

            return RedirectToAction(nameof(Index));
        }
    }
}
