
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using ThptA.Data;
using ThptA.Models;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentFeesController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public StudentFeesController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Danh sách khoản thu của một học sinh
        [HttpGet]
        public async Task<IActionResult> Index(int studentId)
        {
            var student = await _db.Students
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null)
                return NotFound();

            var fees = await _db.StudentFees
                .AsNoTracking()
                .Include(f => f.AcademicYear)
                .Include(f => f.Payments)
                .Where(f => f.StudentId == studentId)
                .OrderByDescending(f => f.CreatedAtUtc)
                .ToListAsync();

            ViewBag.StudentId = student.Id;
            ViewBag.StudentCode = student.Code;
            ViewBag.StudentName = student.FullName;

            return View(fees);
        }

        // Mở trang tạo khoản thu
        [HttpGet]
        public async Task<IActionResult> Create(int studentId)
        {
            var student = await _db.Students
                .AsNoTracking()
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .FirstOrDefaultAsync(s => s.Id == studentId);

            if (student == null ||
                student.SchoolClass == null)
                return NotFound();

            var model = new StudentFeeCreateVm
            {
                StudentId = student.Id,
                StudentDisplay =
                    $"{student.Code} - {student.FullName}",
                AcademicYearCode =
                    student.SchoolClass.AcademicYear?.Code ?? "",
                DueDate = DateTime.Today.AddDays(30)
            };

            return View(model);
        }

        // Lưu khoản thu mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            StudentFeeCreateVm model)
        {
            var student = await _db.Students
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .FirstOrDefaultAsync(s =>
                    s.Id == model.StudentId);

            if (student == null ||
                student.SchoolClass == null)
                return NotFound();

            // Dữ liệu hiển thị luôn lấy lại từ database
            model.StudentDisplay =
                $"{student.Code} - {student.FullName}";

            model.AcademicYearCode =
                student.SchoolClass.AcademicYear?.Code ?? "";

            var title = model.Title?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(title))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Vui lòng nhập tên khoản thu.");
            }

            if (model.DueDate.HasValue &&
                (model.DueDate.Value.Year < 2000 ||
                 model.DueDate.Value.Year > 2100))
            {
                ModelState.AddModelError(
                    nameof(model.DueDate),
                    "Hạn đóng tiền không hợp lệ.");
            }

            int yearId = student.SchoolClass.AcademicYearId;

            // Không tạo trùng khoản thu
            var exists = await _db.StudentFees
                .AnyAsync(f =>
                    f.StudentId == student.Id &&
                    f.AcademicYearId == yearId &&
                    f.Title == title);

            if (exists)
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Khoản thu này đã tồn tại trong năm học.");
            }

            if (!ModelState.IsValid)
                return View(model);

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            var fee = new StudentFee
            {
                StudentId = student.Id,
                AcademicYearId = yearId,
                Title = title,
                AmountDue = model.AmountDue,
                DueDate = model.DueDate!.Value.Date,
                CreatedAtUtc = DateTime.UtcNow
            };

            _db.StudentFees.Add(fee);

            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "CREATE_STUDENT_FEE",
                Target = $"{student.Code} - {title}",
                AtUtc = DateTime.UtcNow
            });

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex) when (
                ex.InnerException is SqlException sql &&
                (sql.Number == 2601 || sql.Number == 2627))
            {
                ModelState.AddModelError(
                    nameof(model.Title),
                    "Khoản thu đã tồn tại. Vui lòng kiểm tra lại.");

                return View(model);
            }

            TempData["Success"] =
                "Tạo khoản thu thành công.";

            return RedirectToAction(
                nameof(Index),
                new { studentId = student.Id });
        }

        // Hiển thị form ghi nhận đóng tiền
        [HttpGet]
        public async Task<IActionResult> RecordPayment(
            int studentFeeId)
        {
            var fee = await _db.StudentFees
                .AsNoTracking()
                .Include(f => f.Student)
                .Include(f => f.Payments)
                .FirstOrDefaultAsync(f =>
                    f.Id == studentFeeId);

            if (fee == null)
                return NotFound();

            var paid = fee.Payments.Sum(p => p.Amount);
            var remaining = fee.AmountDue - paid;

            if (remaining <= 0)
            {
                TempData["Success"] =
                    "Khoản thu này đã được đóng đủ.";

                return RedirectToAction(
                    nameof(Index),
                    new { studentId = fee.StudentId });
            }

            var model = new FeePaymentCreateVm
            {
                StudentFeeId = fee.Id,
                StudentId = fee.StudentId,
                StudentDisplay =
                    $"{fee.Student.Code} - {fee.Student.FullName}",
                FeeTitle = fee.Title,
                AmountDue = fee.AmountDue,
                PaidAmount = paid,
                RemainingAmount = remaining
            };

            return View(model);
        }

        // Lưu số tiền học sinh đã đóng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(
            FeePaymentCreateVm model)
        {
            if (model.StudentFeeId <= 0)
                return BadRequest();

            var actorId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(actorId))
                return Challenge();

            if (model.Amount != decimal.Truncate(model.Amount))
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    "Số tiền VNĐ phải là số nguyên.");
            }

            // Giao dịch để kiểm tra số dư và ghi nhận
            // trên dữ liệu hiện tại của database
            await using var transaction =
                await _db.Database.BeginTransactionAsync(
                    System.Data.IsolationLevel.Serializable);

            var fee = await _db.StudentFees
                .Include(f => f.Student)
                .FirstOrDefaultAsync(f =>
                    f.Id == model.StudentFeeId);

            if (fee == null)
                return NotFound();

            var paid = await _db.FeePayments
                .Where(p => p.StudentFeeId == fee.Id)
                .SumAsync(p => (decimal?)p.Amount) ?? 0m;

            var remaining = fee.AmountDue - paid;

            // Luôn lấy lại thông tin thật từ database
            model.StudentId = fee.StudentId;
            model.StudentDisplay =
                $"{fee.Student.Code} - {fee.Student.FullName}";
            model.FeeTitle = fee.Title;
            model.AmountDue = fee.AmountDue;
            model.PaidAmount = paid;
            model.RemainingAmount = remaining;

            if (remaining <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Khoản thu đã được đóng đủ.");
            }
            else if (model.Amount > remaining)
            {
                ModelState.AddModelError(
                    nameof(model.Amount),
                    "Số tiền đóng không được vượt quá " +
                    $"số tiền còn thiếu ({remaining:N0} đ).");
            }

            if (!ModelState.IsValid)
                return View(model);

            // Ghi nhận một lần đóng tiền mới
            var payment = new FeePayment
            {
                StudentFeeId = fee.Id,
                Amount = model.Amount,
                PaidAtUtc = DateTime.UtcNow,
                RecordedById = actorId,
                Note = model.Note?.Trim()
            };

            _db.FeePayments.Add(payment);

            // Ghi nhật ký thao tác
            _db.AuditLogs.Add(new AuditLog
            {
                ActorId = actorId,
                Action = "RECORD_FEE_PAYMENT",
                Target =
                    $"{fee.Student.Code} - {fee.Title}: " +
                    $"{model.Amount:N0} VND",
                AtUtc = DateTime.UtcNow
            });

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            TempData["Success"] =
                $"Đã ghi nhận đóng {model.Amount:N0} đ.";

            return RedirectToAction(
                nameof(Index),
                new { studentId = fee.StudentId });
        }

        // Xem lịch sử đóng tiền của một khoản thu
        [HttpGet]
        public async Task<IActionResult> History(int studentFeeId)
        {
            if (studentFeeId <= 0)
                return NotFound();

            var fee = await _db.StudentFees
                .AsNoTracking()
                .Include(f => f.Student)
                .Include(f => f.AcademicYear)
                .FirstOrDefaultAsync(f => f.Id == studentFeeId);

            if (fee == null)
                return NotFound();

            var payments = await _db.FeePayments
                .AsNoTracking()
                .Where(p => p.StudentFeeId == studentFeeId)
                .OrderByDescending(p => p.PaidAtUtc)
                .ThenByDescending(p => p.Id)
                .ToListAsync();

            // Lấy tài khoản đã ghi nhận tiền
            var actorIds = payments
                .Select(p => p.RecordedById)
                .Distinct()
                .ToList();

            var actors = await _db.Users
                .AsNoTracking()
                .Where(u => actorIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.UserName
                })
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrWhiteSpace(u.FullName)
                        ? u.FullName
                        : u.UserName ?? u.Id);

            var paid = payments.Sum(p => p.Amount);

            var model = new FeePaymentHistoryVm
            {
                StudentId = fee.StudentId,

                StudentDisplay =
                    $"{fee.Student.Code} - {fee.Student.FullName}",

                FeeTitle = fee.Title,
                YearCode = fee.AcademicYear.Code,
                AmountDue = fee.AmountDue,
                PaidAmount = paid,
                RemainingAmount = fee.AmountDue - paid,

                Payments = payments.Select(p =>
                    new FeePaymentHistoryRowVm
                    {
                        Id = p.Id,
                        Amount = p.Amount,
                        PaidAtUtc = p.PaidAtUtc,

                        RecordedBy = actors.TryGetValue(
                            p.RecordedById, out var actor)
                            ? actor
                            : "Không tìm thấy tài khoản",

                        Note = p.Note
                    })
                    .ToList()
            };

            return View(model);
        }

    }
}
