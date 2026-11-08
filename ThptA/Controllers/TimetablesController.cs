
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThptA.Data;
using ThptA.Models;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class TimetablesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public TimetablesController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Xem thời khóa biểu theo lớp
        [HttpGet]
        public async Task<IActionResult> Index(
            int? schoolClassId)
        {
            bool isAdmin = User.IsInRole("Admin");

            var teacherId =
                _userManager.GetUserId(User);

            if (!isAdmin &&
                string.IsNullOrEmpty(teacherId))
            {
                return Forbid();
            }

            // Lớp học được phép xem
            var classQuery = _db.SchoolClasses
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                classQuery = classQuery.Where(c =>
                    c.TeacherId == teacherId);
            }

            var classes = await classQuery
                .OrderByDescending(
                    c => c.AcademicYear!.Code)
                .ThenBy(c => c.Grade)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    YearCode = c.AcademicYear!.Code
                })
                .ToListAsync();

            // Mặc định chọn lớp đầu tiên
            int? selectedId = schoolClassId
                ?? classes.FirstOrDefault()?.Id;

            // Không được xem lớp ngoài quyền
            if (selectedId.HasValue &&
                !classes.Any(c => c.Id == selectedId.Value))
            {
                return NotFound();
            }

            var model = new TimetableIndexVm
            {
                SchoolClassId = selectedId,

                ClassOptions = classes
                    .Select(c => new SelectListItem
                    {
                        Value = c.Id.ToString(),
                        Text = $"{c.Name} - {c.YearCode}"
                    })
                    .ToList()
            };

            var selectedClass = classes
                .FirstOrDefault(c => c.Id == selectedId);

            if (selectedClass != null)
            {
                model.ClassDisplay =
                    $"{selectedClass.Name} - " +
                    $"{selectedClass.YearCode}";
            }

            // Lấy thời khóa biểu từ database
            if (selectedId.HasValue)
            {
                model.Entries = await _db.TimetableEntries
                    .AsNoTracking()
                    .Where(t =>
                        t.SchoolClassId == selectedId.Value)
                    .OrderBy(t => t.DayOfWeek)
                    .ThenBy(t => t.Period)
                    .ToListAsync();
            }

            return View(model);
        }

        // Hiển thị form thêm tiết học - chỉ Admin
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(
            int? schoolClassId)
        {
            var model = new TimetableCreateVm();

            if (schoolClassId.HasValue)
                model.SchoolClassId = schoolClassId.Value;

            await LoadClassOptionsAsync(model);

            return View(model);
        }

        // Lưu tiết học vào database
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            TimetableCreateVm model)
        {
            var schoolClass = await _db.SchoolClasses
                .AsNoTracking()
                .Include(c => c.AcademicYear)
                .FirstOrDefaultAsync(c =>
                    c.Id == model.SchoolClassId);

            if (schoolClass == null)
            {
                ModelState.AddModelError(
                    nameof(model.SchoolClassId),
                    "Lớp học không tồn tại.");
            }

            if (string.IsNullOrWhiteSpace(model.SubjectName))
            {
                ModelState.AddModelError(
                    nameof(model.SubjectName),
                    "Vui lòng nhập tên môn học.");
            }

            // Kiểm tra trùng tiết
            bool exists = await _db.TimetableEntries
                .AnyAsync(t =>
                    t.SchoolClassId == model.SchoolClassId &&
                    t.DayOfWeek == model.DayOfWeek &&
                    t.Period == model.Period);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Period),
                    "Tiết học này đã có môn học. " +
                    "Vui lòng chọn tiết khác.");
            }

            if (!ModelState.IsValid)
            {
                await LoadClassOptionsAsync(model);
                return View(model);
            }

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            // Tạo tiết học
            var entry = new TimetableEntry
            {
                SchoolClassId = model.SchoolClassId,
                DayOfWeek = model.DayOfWeek,
                Period = model.Period,
                SubjectName = model.SubjectName.Trim(),
                TeacherName = model.TeacherName?.Trim(),
                Room = model.Room?.Trim()
            };

            _db.TimetableEntries.Add(entry);

            // Ghi nhật ký
            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "CREATE_TIMETABLE",
                Target =
                    $"{schoolClass!.Name} - " +
                    $"{schoolClass.AcademicYear?.Code} - " +
                    $"Thứ {model.DayOfWeek}, tiết {model.Period}",
                AtUtc = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is
                    Microsoft.Data.SqlClient.SqlException sql &&
                (sql.Number == 2601 || sql.Number == 2627))
            {
                ModelState.AddModelError(
                    nameof(model.Period),
                    "Tiết này vừa được thêm. " +
                    "Vui lòng chọn tiết khác.");

                await LoadClassOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] =
                "Thêm tiết học thành công.";

            return RedirectToAction(
                nameof(Index),
                new { schoolClassId = model.SchoolClassId });
        }

        // Nạp các lớp cho danh sách chọn
        private async Task LoadClassOptionsAsync(
            TimetableCreateVm model)
        {
            model.ClassOptions = await _db.SchoolClasses
                .AsNoTracking()
                .OrderByDescending(c =>
                    c.AcademicYear!.Code)
                .ThenBy(c => c.Grade)
                .ThenBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name + " - " +
                           c.AcademicYear!.Code
                })
                .ToListAsync();
        }

        // Hiển thị giao diện chỉnh sửa tiết học
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            var entry = await _db.TimetableEntries
                .AsNoTracking()
                .Include(t => t.SchoolClass)
                    .ThenInclude(c => c.AcademicYear)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entry == null)
                return NotFound();

            var model = new TimetableEditVm
            {
                Id = entry.Id,
                ClassDisplay =
                    $"{entry.SchoolClass.Name} - " +
                    $"{entry.SchoolClass.AcademicYear?.Code}",
                DayOfWeek = entry.DayOfWeek,
                Period = entry.Period,
                SubjectName = entry.SubjectName,
                TeacherName = entry.TeacherName,
                Room = entry.Room
            };

            return View(model);
        }

        // Lưu thông tin tiết học đã sửa
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, TimetableEditVm model)
        {
            if (id <= 0 || id != model.Id)
                return BadRequest();

            var entry = await _db.TimetableEntries
                .Include(t => t.SchoolClass)
                    .ThenInclude(c => c.AcademicYear)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entry == null)
                return NotFound();

            // Lấy lại thông tin lớp và vị trí tiết từ DB
            model.ClassDisplay =
                $"{entry.SchoolClass.Name} - " +
                $"{entry.SchoolClass.AcademicYear?.Code}";

            model.DayOfWeek = entry.DayOfWeek;
            model.Period = entry.Period;

            if (string.IsNullOrWhiteSpace(model.SubjectName))
            {
                ModelState.AddModelError(
                    nameof(model.SubjectName),
                    "Vui lòng nhập tên môn học.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            entry.SubjectName = model.SubjectName.Trim();

            entry.TeacherName =
                string.IsNullOrWhiteSpace(model.TeacherName)
                    ? null
                    : model.TeacherName.Trim();

            entry.Room =
                string.IsNullOrWhiteSpace(model.Room)
                    ? null
                    : model.Room.Trim();

            // Ghi nhật ký chỉnh sửa
            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "UPDATE_TIMETABLE",
                Target =
                    $"{entry.SchoolClass.Name} - " +
                    $"Thứ {entry.DayOfWeek}, " +
                    $"tiết {entry.Period}",
                AtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Chỉnh sửa tiết học thành công.";

            return RedirectToAction(
                nameof(Index),
                new { schoolClassId = entry.SchoolClassId });
        }

        // Xóa tiết học - chỉ Admin
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return NotFound();

            var entry = await _db.TimetableEntries
                .Include(t => t.SchoolClass)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entry == null)
                return NotFound();

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            int schoolClassId = entry.SchoolClassId;

            // Ghi nhật ký trước khi xóa
            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "DELETE_TIMETABLE",
                Target =
                    $"{entry.SchoolClass.Name} - " +
                    $"Thứ {entry.DayOfWeek}, " +
                    $"tiết {entry.Period}: " +
                    entry.SubjectName,
                AtUtc = DateTime.UtcNow
            });

            _db.TimetableEntries.Remove(entry);

            await _db.SaveChangesAsync();

            TempData["Success"] =
                "Đã xóa tiết học khỏi thời khóa biểu.";

            return RedirectToAction(
                nameof(Index),
                new { schoolClassId });
        }

    }
}
