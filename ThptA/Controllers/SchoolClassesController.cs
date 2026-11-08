
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

        // Hiển thị trang phân công GVCN
        [HttpGet]
        public async Task<IActionResult> AssignTeacher(int id)
        {
            if (id <= 0)
                return NotFound();

            var schoolClass = await _db.SchoolClasses
                .AsNoTracking()
                .Include(c => c.AcademicYear)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null)
                return NotFound();

            var model = new SchoolClassAssignTeacherVm
            {
                Id = schoolClass.Id,
                ClassName = schoolClass.Name,
                AcademicYearCode =
                    schoolClass.AcademicYear?.Code ?? "",
                TeacherId = schoolClass.TeacherId,
                RowVersion = Convert.ToBase64String(
                    schoolClass.RowVersion)
            };

            await LoadTeachersAsync(model);

            return View(model);
        }

        // Lưu phân công GVCN
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeacher(
            int id, SchoolClassAssignTeacherVm model)
        {
            if (id <= 0 || id != model.Id)
                return BadRequest();

            var schoolClass = await _db.SchoolClasses
                .Include(c => c.AcademicYear)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (schoolClass == null)
                return NotFound();

            model.ClassName = schoolClass.Name;
            model.AcademicYearCode =
                schoolClass.AcademicYear?.Code ?? "";

            model.TeacherId = string.IsNullOrWhiteSpace(
                model.TeacherId)
                ? null
                : model.TeacherId.Trim();

            if (model.TeacherId != null)
            {
                var teacher = await _userManager
                    .FindByIdAsync(model.TeacherId);

                if (teacher == null ||
                    !teacher.IsActive ||
                    !await _userManager.IsInRoleAsync(
                        teacher, "Teacher"))
                {
                    ModelState.AddModelError(
                        nameof(model.TeacherId),
                        "Giáo viên không hợp lệ hoặc đã bị khóa.");
                }
                else
                {
                    // Không phân công 1 GVCN cho 2 lớp
                    // trong cùng một năm học
                    var alreadyAssigned =
                        await _db.SchoolClasses.AnyAsync(c =>
                            c.Id != schoolClass.Id &&
                            c.AcademicYearId ==
                                schoolClass.AcademicYearId &&
                            c.TeacherId == model.TeacherId);

                    if (alreadyAssigned)
                    {
                        ModelState.AddModelError(
                            nameof(model.TeacherId),
                            "Giáo viên đã chủ nhiệm một lớp " +
                            "khác trong cùng năm học.");
                    }
                }
            }

            byte[] originalVersion = Array.Empty<byte>();

            try
            {
                originalVersion =
                    Convert.FromBase64String(model.RowVersion);

                if (originalVersion.Length != 8)
                    throw new FormatException();
            }
            catch (FormatException)
            {
                ModelState.AddModelError(
                    "",
                    "Phiên bản dữ liệu không hợp lệ. " +
                    "Vui lòng tải lại trang.");
            }

            if (!ModelState.IsValid)
            {
                await LoadTeachersAsync(model);
                return View(model);
            }

            // Kiểm tra thay đổi đồng thời
            _db.Entry(schoolClass)
                .Property(c => c.RowVersion)
                .OriginalValue = originalVersion;

            if (schoolClass.TeacherId == model.TeacherId)
            {
                TempData["Success"] =
                    "Phân công giáo viên không thay đổi.";

                return RedirectToAction(nameof(Index));
            }

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            schoolClass.TeacherId = model.TeacherId;

            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = model.TeacherId == null
                    ? "UNASSIGN_TEACHER"
                    : "ASSIGN_TEACHER",
                Target = schoolClass.Name,
                AtUtc = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(
                    "",
                    "Lớp học đã được người khác cập nhật. " +
                    "Vui lòng tải lại trang.");

                await LoadTeachersAsync(model);
                return View(model);
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(
                    "",
                    "Không thể lưu phân công. " +
                    "Vui lòng kiểm tra giáo viên có đang " +
                    "chủ nhiệm lớp khác trong năm học không.");

                await LoadTeachersAsync(model);
                return View(model);
            }

            TempData["Success"] =
                model.TeacherId == null
                    ? "Đã hủy phân công GVCN."
                    : "Phân công GVCN thành công.";

            return RedirectToAction(nameof(Index));
        }

        // Lấy danh sách tài khoản giáo viên
        private async Task LoadTeachersAsync(
            SchoolClassAssignTeacherVm model)
        {
            var teachers = await _userManager
                .GetUsersInRoleAsync("Teacher");

            model.TeacherOptions = teachers
                .Where(t => t.IsActive ||
                    t.Id == model.TeacherId)
                .OrderBy(t => t.FullName)
                .Select(t => new SelectListItem
                {
                    Value = t.Id,
                    Text = $"{t.FullName} ({t.UserName})" +
                        (t.IsActive ? "" : " - Đã khóa")
                })
                .ToList();
        }

    }
}
