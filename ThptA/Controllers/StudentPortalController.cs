
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThptA.Data;
using ThptA.Models;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentPortalController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public StudentPortalController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        // Xác định học sinh từ tài khoản đang đăng nhập
        private async Task<Student?> GetCurrentStudentAsync()
        {
            var account = await _userManager.GetUserAsync(User);

            if (account == null ||
                !account.IsActive ||
                account.StudentId == null)
            {
                return null;
            }

            return await _db.Students
                .AsNoTracking()
                .Include(s => s.SchoolClass)
                    .ThenInclude(c => c!.AcademicYear)
                .FirstOrDefaultAsync(s =>
                    s.Id == account.StudentId.Value);
        }

        // 1. TRANG CỦA TÔI
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return NotFound("Không tìm thấy hồ sơ học sinh.");

            return View(new StudentPortalVm
            {
                Student = student
            });
        }

        // 2. THỜI KHÓA BIỂU
        [HttpGet]
        public async Task<IActionResult> Timetable()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return NotFound("Không tìm thấy hồ sơ học sinh.");

            var timetable = await _db.TimetableEntries
                .AsNoTracking()
                .Where(t => t.SchoolClassId == student.ClassId)
                .OrderBy(t => t.DayOfWeek)
                .ThenBy(t => t.Period)
                .ToListAsync();

            return View(new StudentPortalVm
            {
                Student = student,
                Timetable = timetable
            });
        }

        // 3. KHOẢN THU CỦA TÔI
        [HttpGet]
        public async Task<IActionResult> Fees()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return NotFound("Không tìm thấy hồ sơ học sinh.");

            var fees = await _db.StudentFees
                .AsNoTracking()
                .Include(f => f.AcademicYear)
                .Include(f => f.Payments)
                .Where(f => f.StudentId == student.Id)
                .OrderByDescending(f => f.CreatedAtUtc)
                .ToListAsync();

            return View(new StudentPortalVm
            {
                Student = student,
                Fees = fees
            });
        }

        // 4. LỊCH SỬ ĐÓNG TIỀN
        [HttpGet]
        public async Task<IActionResult> History()
        {
            var student = await GetCurrentStudentAsync();

            if (student == null)
                return NotFound("Không tìm thấy hồ sơ học sinh.");

            var fees = await _db.StudentFees
                .AsNoTracking()
                .Include(f => f.Payments)
                .Where(f => f.StudentId == student.Id)
                .ToListAsync();

            return View(new StudentPortalVm
            {
                Student = student,
                Fees = fees
            });
        }
    }
}
