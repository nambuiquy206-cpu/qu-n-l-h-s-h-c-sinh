
using Microsoft.AspNetCore.Mvc.Rendering;
using ThptA.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThptA.Data;
using ThptA.Models;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin,Teacher")]
    public class StudentsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public StudentsController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Xem danh sách và tìm kiếm học sinh


        [HttpGet]
        public async Task<IActionResult> Index(
            string? q,
            int? yearId,
            int? grade,
            int? classId,
            string? status,
            bool onlyMissingContact = false,
            int page = 1)

        {
            const int pageSize = 20;

            q = q?.Trim();
            page = Math.Max(1, page);

            if (status != "Active" && status != "Inactive")
                status = null;

            bool isAdmin = User.IsInRole("Admin");
            var teacherId = _userManager.GetUserId(User);

            if (!isAdmin && string.IsNullOrEmpty(teacherId))
                return Forbid();

            // 1. Truy vấn học sinh
            var query = _db.Students
                .AsNoTracking()
                .AsQueryable();

            // 2. Phân quyền giáo viên
            if (!isAdmin)
            {
                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.TeacherId == teacherId);
            }

            // 3. Tìm theo mã hoặc họ tên
            if (!string.IsNullOrWhiteSpace(q))
            {
                query = query.Where(s =>
                    s.Code.Contains(q) ||
                    s.FullName.Contains(q));
            }

            // 4. Lọc theo năm học
            if (yearId.HasValue)
            {
                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.AcademicYearId == yearId.Value);
            }

            // 5. Lọc theo khối
            if (grade.HasValue)
            {
                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.Grade == grade.Value);
            }

            // 6. Lọc theo lớp
            if (classId.HasValue)
            {
                query = query.Where(s =>
                    s.ClassId == classId.Value);
            }

            // 7. Lọc theo trạng thái
            if (status != null)
            {
                query = query.Where(s =>
                    s.Status == status);
            }

            // 8. Chỉ hiện hồ sơ thiếu SĐT liên hệ
            if (onlyMissingContact)
            {
                query = query.Where(s =>
                    s.PrimaryContact == null ||
                    s.PrimaryContact.Phone == null ||
                    s.PrimaryContact.Phone.Trim() == "");
            }

            // 8. Lấy danh sách năm học
            var yearQuery = _db.AcademicYears
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                yearQuery = yearQuery.Where(y =>
                    _db.SchoolClasses.Any(c =>
                        c.AcademicYearId == y.Id &&
                        c.TeacherId == teacherId));
            }

            var years = await yearQuery
                .OrderByDescending(y => y.Code)
                .Select(y => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = y.Id.ToString(),
                    Text = y.Code
                })
                .ToListAsync();

            // 9. Lấy danh sách lớp học
            var classQuery = _db.SchoolClasses
                .AsNoTracking()
                .AsQueryable();

            if (!isAdmin)
            {
                classQuery = classQuery.Where(c =>
                    c.TeacherId == teacherId);
            }

            if (yearId.HasValue)
            {
                classQuery = classQuery.Where(c =>
                    c.AcademicYearId == yearId.Value);
            }

            if (grade.HasValue)
            {
                classQuery = classQuery.Where(c =>
                    c.Grade == grade.Value);
            }

            var classes = await classQuery
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Name)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name + " - " + c.AcademicYear!.Code
                })
                .ToListAsync();

            // 10. Đếm kết quả và phân trang
            int total = await query.CountAsync();

            int activeCount = await query.CountAsync(
                s => s.Status == "Active");

            int inactiveCount = await query.CountAsync(
                s => s.Status == "Inactive");

            int missingContactCount = await query.CountAsync(
                s => s.PrimaryContact == null ||
                     s.PrimaryContact.Phone == null ||
                     s.PrimaryContact.Phone.Trim() == "");


            int pageCount = Math.Max(
                1,
                (int)Math.Ceiling(total / (double)pageSize));

            page = Math.Min(page, pageCount);


            var students = await query
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .Include(s => s.PrimaryContact)
                .OrderBy(s => s.Code)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            // 11. Trả dữ liệu ra giao diện
            var model = new StudentListVm
            {
                Q = q,
                YearId = yearId,
                OnlyMissingContact = onlyMissingContact,
                Grade = grade,
                ClassId = classId,
                Status = status,
                Students = students,
                YearOptions = years,
                ClassOptions = classes,
                Page = page,
                PageCount = pageCount,

                Total = total,
                ActiveCount = activeCount,
                InactiveCount = inactiveCount,
                MissingContactCount = missingContactCount

            };

            return View(model);
        }


        // Hiển thị trang thêm học sinh
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new StudentCreateVm();

            await LoadClassOptionsAsync(model);

            return View(model);
        }

        // Xử lý thêm hồ sơ học sinh
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            StudentCreateVm model)
        {
            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            if (model.BirthDate.HasValue &&
                (model.BirthDate.Value.Date > DateTime.Today ||
                 model.BirthDate.Value.Year < 1900))
            {
                ModelState.AddModelError(
                    nameof(model.BirthDate),
                    "Ngày sinh không hợp lệ.");
            }

            // Kiểm tra quyền đối với lớp được chọn
            var isAdmin = User.IsInRole("Admin");

            var canUseClass = await _db.SchoolClasses
                .AnyAsync(c =>
                    c.Id == model.ClassId &&
                    (isAdmin || c.TeacherId == actorId));

            if (!canUseClass)
            {
                ModelState.AddModelError(
                    nameof(model.ClassId),
                    "Bạn không có quyền thêm học sinh vào lớp này.");
            }

            // Kiểm tra mã học sinh đã tồn tại chưa
            var studentCode = model.Code.Trim();

            var codeExists = await _db.Students
                .AnyAsync(s => s.Code == studentCode);

            if (codeExists)
            {
                ModelState.AddModelError(
                    nameof(model.Code),
                    "Mã học sinh đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                await LoadClassOptionsAsync(model);
                return View(model);
            }

            // Tạo hồ sơ học sinh
            var student = new Student
            {
                Code = studentCode,
                FullName = model.FullName.Trim(),
                BirthDate = model.BirthDate!.Value.Date,
                Gender = model.Gender,
                Address = model.Address.Trim(),
                ClassId = model.ClassId,
                Status = "Active",
                UpdatedAtUtc = DateTime.UtcNow,

                // Tạo người liên hệ chính cùng hồ sơ
                PrimaryContact = new PrimaryContact
                {
                    FullName = model.ContactFullName.Trim(),
                    Relationship =
                        model.ContactRelationship.Trim(),
                    Phone = model.ContactPhone.Trim()
                }
            };

            _db.Students.Add(student);

            // Ghi nhật ký người tạo
            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "CREATE_STUDENT",
                Target = studentCode,
                AtUtc = DateTime.UtcNow
            });

            // Lưu học sinh, liên hệ và nhật ký cùng lúc
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
                    nameof(model.Code),
                    "Dữ liệu bị trùng. Hãy kiểm tra mã học sinh.");

                await LoadClassOptionsAsync(model);
                return View(model);
            }

            TempData["Success"] =
                "Thêm hồ sơ học sinh thành công.";

            return RedirectToAction(nameof(Index));
        }

        // Lấy danh sách lớp mà người dùng được phép quản lý
        private async Task LoadClassOptionsAsync(
            StudentCreateVm model)
        {
            var query = _db.SchoolClasses
                .AsNoTracking()
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                var teacherId = _userManager.GetUserId(User);

                query = query.Where(
                    c => c.TeacherId == teacherId);
            }

            var classes = await query
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Name)
                .Select(c => new
                {
                    c.Id,
                    c.Name,
                    YearCode = c.AcademicYear!.Code
                })
                .ToListAsync();

            model.ClassOptions = classes
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = $"{c.Name} - {c.YearCode}"
                })
                .ToList();
        }

        // Xem chi tiết hồ sơ học sinh
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return NotFound();

            var query = _db.Students
                .AsNoTracking()
                .AsQueryable();

            // Giáo viên chỉ xem học sinh thuộc lớp mình
            if (!User.IsInRole("Admin"))
            {
                var teacherId = _userManager.GetUserId(User);

                if (string.IsNullOrEmpty(teacherId))
                    return Forbid();

                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.TeacherId == teacherId);
            }

            var student = await query
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .Include(s => s.PrimaryContact)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            return View(student);
        }

        // Hiển thị trang chỉnh sửa học sinh
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return NotFound();

            var query = _db.Students
                .AsNoTracking()
                .Include(s => s.PrimaryContact)
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .AsQueryable();

            // GVCN chỉ được sửa học sinh thuộc lớp mình
            if (!User.IsInRole("Admin"))
            {
                var teacherId = _userManager.GetUserId(User);

                if (string.IsNullOrEmpty(teacherId))
                    return Forbid();

                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.TeacherId == teacherId);
            }

            var student = await query
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            var model = new StudentEditVm
            {
                Id = student.Id,
                Code = student.Code,
                ClassDisplay =
                    $"{student.SchoolClass?.Name} - " +
                    $"{student.SchoolClass?.AcademicYear?.Code}",
                FullName = student.FullName,
                BirthDate = student.BirthDate,
                Gender = student.Gender,
                Address = student.Address,
                ContactFullName =
                    student.PrimaryContact?.FullName ?? "",
                ContactRelationship =
                    student.PrimaryContact?.Relationship ?? "",
                ContactPhone =
                    student.PrimaryContact?.Phone ?? "",
                RowVersion =
                    Convert.ToBase64String(student.RowVersion)
            };

            return View(model);
        }

        // Xử lý lưu hồ sơ đã chỉnh sửa
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id, StudentEditVm model)
        {
            if (id <= 0 || id != model.Id)
                return BadRequest();

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            var query = _db.Students
                .Include(s => s.PrimaryContact)
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .AsQueryable();

            if (!User.IsInRole("Admin"))
            {
                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.TeacherId == actorId);
            }

            var student = await query
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            model.Code = student.Code;
            model.ClassDisplay =
                $"{student.SchoolClass?.Name} - " +
                $"{student.SchoolClass?.AcademicYear?.Code}";

            if (model.BirthDate.HasValue &&
                (model.BirthDate.Value.Date > DateTime.Today ||
                 model.BirthDate.Value.Year < 1900))
            {
                ModelState.AddModelError(
                    nameof(model.BirthDate),
                    "Ngày sinh không hợp lệ.");
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
                    "Dữ liệu phiên bản không hợp lệ. " +
                    "Vui lòng tải lại hồ sơ.");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Kiểm tra thay đổi đồng thời
            _db.Entry(student)
                .Property(s => s.RowVersion)
                .OriginalValue = originalVersion;

            student.FullName = model.FullName.Trim();
            student.BirthDate = model.BirthDate!.Value.Date;
            student.Gender = model.Gender;
            student.Address = model.Address.Trim();
            student.UpdatedAtUtc = DateTime.UtcNow;

            if (student.PrimaryContact == null)
            {
                student.PrimaryContact = new PrimaryContact();
            }

            student.PrimaryContact.FullName =
                model.ContactFullName.Trim();

            student.PrimaryContact.Relationship =
                model.ContactRelationship.Trim();

            student.PrimaryContact.Phone =
                model.ContactPhone.Trim();

            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "UPDATE_STUDENT",
                Target = student.Code,
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
                    "Hồ sơ đã được người khác cập nhật. " +
                    "Vui lòng mở lại hồ sơ và thử lại.");

                return View(model);
            }

            TempData["Success"] =
                "Cập nhật hồ sơ học sinh thành công.";

            return RedirectToAction(
                nameof(Details),
                new { id = student.Id });
        }

        // Admin thay đổi trạng thái học sinh
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(
            int id, string status)
        {
            if (id <= 0)
                return NotFound();

            // Chỉ cho phép hai trạng thái hợp lệ
            if (status != "Active" && status != "Inactive")
                return BadRequest();

            var student = await _db.Students
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null)
                return NotFound();

            if (student.Status == status)
                return RedirectToAction(
                    nameof(Details), new { id });

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            // Cập nhật trạng thái
            student.Status = status;
            student.UpdatedAtUtc = DateTime.UtcNow;

            // Ghi nhật ký
            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = status == "Active"
                    ? "ACTIVATE_STUDENT"
                    : "DEACTIVATE_STUDENT",
                Target = student.Code,
                AtUtc = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                TempData["Error"] =
                    "Hồ sơ vừa được người khác thay đổi. " +
                    "Vui lòng kiểm tra lại.";

                return RedirectToAction(
                    nameof(Details), new { id });
            }

            TempData["Success"] =
                status == "Active"
                    ? "Đã chuyển học sinh sang trạng thái Đang học."
                    : "Đã chuyển học sinh sang trạng thái Ngừng học.";

            return RedirectToAction(
                nameof(Details), new { id });
        }

        // Admin mở trang chuyển lớp
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> TransferClass(int id)
        {
            if (id <= 0)
                return NotFound();

            var student = await _db.Students
                .AsNoTracking()
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null ||
                student.SchoolClass == null)
                return NotFound();

            var model = new StudentTransferVm
            {
                StudentId = student.Id,
                StudentCode = student.Code,
                StudentName = student.FullName,
                CurrentClassName = student.SchoolClass.Name,
                AcademicYearCode =
                    student.SchoolClass.AcademicYear?.Code ?? "",
                RowVersion =
                    Convert.ToBase64String(student.RowVersion)
            };

            await LoadTransferClassesAsync(
                model, student.SchoolClass);

            return View(model);
        }

        // Admin lưu việc chuyển lớp
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferClass(
            int id, StudentTransferVm model)
        {
            if (id <= 0 || id != model.StudentId)
                return BadRequest();

            var student = await _db.Students
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (student == null ||
                student.SchoolClass == null)
                return NotFound();

            var oldClass = student.SchoolClass;

            // Nạp lại thông tin từ database
            model.StudentCode = student.Code;
            model.StudentName = student.FullName;
            model.CurrentClassName = oldClass.Name;
            model.AcademicYearCode =
                oldClass.AcademicYear?.Code ?? "";

            // Chỉ cho chuyển sang lớp cùng khối, cùng năm
            var newClass = await _db.SchoolClasses
                .AsNoTracking()
                .FirstOrDefaultAsync(c =>
                    c.Id == model.NewClassId &&
                    c.Id != oldClass.Id &&
                    c.Grade == oldClass.Grade &&
                    c.AcademicYearId == oldClass.AcademicYearId);

            if (newClass == null)
            {
                ModelState.AddModelError(
                    nameof(model.NewClassId),
                    "Lớp mới không hợp lệ. " +
                    "Chỉ được chuyển trong cùng khối và năm học.");
            }

            // Kiểm tra phiên bản chống sửa đồng thời
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
                    "Phiên bản hồ sơ không hợp lệ. " +
                    "Vui lòng mở lại trang.");
            }

            if (!ModelState.IsValid)
            {
                await LoadTransferClassesAsync(model, oldClass);
                return View(model);
            }

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            // Kiểm tra thay đổi đồng thời
            _db.Entry(student)
                .Property(s => s.RowVersion)
                .OriginalValue = originalVersion;

            // Chuyển học sinh sang lớp mới
            student.ClassId = newClass!.Id;
            student.UpdatedAtUtc = DateTime.UtcNow;

            // Ghi nhật ký chuyển lớp
            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "TRANSFER_STUDENT",
                Target = $"{student.Code}: " +
                         $"{oldClass.Name} -> {newClass.Name}",
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
                    "Hồ sơ đã được thay đổi bởi người khác. " +
                    "Vui lòng tải lại trang.");

                await LoadTransferClassesAsync(model, oldClass);
                return View(model);
            }

            TempData["Success"] =
                $"Đã chuyển {student.Code} sang lớp {newClass.Name}.";

            return RedirectToAction(
                nameof(Details),
                new { id = student.Id });
        }

        // Danh sách lớp đích hợp lệ
        private async Task LoadTransferClassesAsync(
            StudentTransferVm model,
            SchoolClass currentClass)
        {
            model.ClassOptions = await _db.SchoolClasses
                .AsNoTracking()
                .Where(c =>
                    c.Id != currentClass.Id &&
                    c.Grade == currentClass.Grade &&
                    c.AcademicYearId ==
                        currentClass.AcademicYearId)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.Name
                })
                .ToListAsync();
        }

    }
}
