
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ThptA.Data;
using ThptA.Models;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SchoolClassesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public SchoolClassesController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // 1. Danh sách lớp học
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var classes = await _db.SchoolClasses
                .AsNoTracking()
                .Include(c => c.AcademicYear)
                .Include(c => c.Teacher)
                .OrderByDescending(c => c.AcademicYear!.Code)
                .ThenBy(c => c.Grade)
                .ThenBy(c => c.Name)
                .ToListAsync();

            return View(classes);
        }

        // 2. Hiển thị trang thêm lớp
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new SchoolClassCreateVm();

            await LoadYearsAsync(model);

            return View(model);
        }

        // 3. Xử lý thêm lớp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            SchoolClassCreateVm model)
        {
            if (!ModelState.IsValid)
            {
                await LoadYearsAsync(model);
                return View(model);
            }

            var className = model.Name.Trim().ToUpperInvariant();

            // Kiểm tra tên lớp phù hợp với khối
            if (!className.StartsWith(
                model.Grade.ToString()))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Tên lớp phải bắt đầu bằng số khối. " +
                    "Ví dụ: 10A2.");

                await LoadYearsAsync(model);
                return View(model);
            }

            // Kiểm tra năm học tồn tại
            var yearExists = await _db.AcademicYears
                .AnyAsync(y => y.Id == model.AcademicYearId);

            if (!yearExists)
            {
                ModelState.AddModelError(
                    nameof(model.AcademicYearId),
                    "Năm học không tồn tại.");

                await LoadYearsAsync(model);
                return View(model);
            }

            // Không cho trùng tên lớp trong cùng năm học
            var exists = await _db.SchoolClasses
                .AnyAsync(c =>
                    c.Name == className &&
                    c.AcademicYearId == model.AcademicYearId);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Lớp này đã tồn tại trong năm học.");

                await LoadYearsAsync(model);
                return View(model);
            }

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            var schoolClass = new SchoolClass
            {
                Name = className,
                Grade = model.Grade,
                AcademicYearId = model.AcademicYearId,
                TeacherId = null
            };

            _db.SchoolClasses.Add(schoolClass);

            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "CREATE_CLASS",
                Target = className,
                AtUtc = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql &&
                      (sql.Number == 2601 || sql.Number == 2627))
            {
                ModelState.AddModelError(
                    nameof(model.Name),
                    "Lớp đã tồn tại. Vui lòng chọn tên khác.");

                await LoadYearsAsync(model);
                return View(model);
            }

            TempData["Success"] =
                "Thêm lớp học thành công.";

            return RedirectToAction(nameof(Index));
        }

        // 4. Lấy danh sách năm học
        private async Task LoadYearsAsync(
            SchoolClassCreateVm model)
        {
            model.YearOptions = await _db.AcademicYears
                .AsNoTracking()
                .OrderByDescending(y => y.Code)
                .Select(y => new SelectListItem
                {
                    Value = y.Id.ToString(),
                    Text = y.Code
                })
                .ToListAsync();
        }
    }
}
