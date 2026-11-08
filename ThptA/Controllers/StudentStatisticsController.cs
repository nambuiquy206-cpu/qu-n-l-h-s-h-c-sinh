
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
    public class StudentStatisticsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly UserManager<AppUser> _userManager;

        public StudentStatisticsController(
            AppDbContext db,
            UserManager<AppUser> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? yearId)
        {
            bool isAdmin = User.IsInRole("Admin");
            var teacherId = _userManager.GetUserId(User);

            if (!isAdmin && string.IsNullOrEmpty(teacherId))
                return Forbid();

            // Danh sách năm học được phép xem
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

            var yearOptions = await yearQuery
                .OrderByDescending(y => y.Code)
                .Select(y => new SelectListItem
                {
                    Value = y.Id.ToString(),
                    Text = y.Code
                })
                .ToListAsync();

            // Truy vấn học sinh
            var query = _db.Students
                .AsNoTracking()
                .AsQueryable();

            // Giáo viên chỉ thống kê lớp mình phụ trách
            if (!isAdmin)
            {
                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.TeacherId == teacherId);
            }

            // Lọc theo năm học
            if (yearId.HasValue)
            {
                query = query.Where(s =>
                    s.SchoolClass != null &&
                    s.SchoolClass.AcademicYearId == yearId.Value);
            }

            // Chỉ lấy các cột cần thống kê
            var students = await query
                .Select(s => new
                {
                    s.Gender,
                    s.Status,
                    s.ClassId,
                    ClassName = s.SchoolClass!.Name,
                    Grade = s.SchoolClass!.Grade,
                    YearCode =
                        s.SchoolClass!.AcademicYear!.Code
                })
                .ToListAsync();

            var model = new StudentStatisticsVm
            {
                YearId = yearId,
                YearOptions = yearOptions,

                Total = students.Count,
                Male = students.Count(s => s.Gender == "M"),
                Female = students.Count(s => s.Gender == "F"),
                Other = students.Count(s => s.Gender == "O"),

                Active = students.Count(
                    s => s.Status == "Active"),

                Inactive = students.Count(
                    s => s.Status == "Inactive")
            };

            // Thống kê theo khối
            model.ByGrade = new[] { 10, 11, 12 }
                .Select(grade => new StudentGradeStatVm
                {
                    Grade = grade,
                    Total = students.Count(
                        s => s.Grade == grade),
                    Active = students.Count(
                        s => s.Grade == grade &&
                             s.Status == "Active")
                })
                .ToList();

            // Thống kê theo từng lớp
            model.ByClass = students
                .GroupBy(s => new
                {
                    s.ClassId,
                    s.ClassName,
                    s.Grade,
                    s.YearCode
                })
                .OrderBy(g => g.Key.YearCode)
                .ThenBy(g => g.Key.Grade)
                .ThenBy(g => g.Key.ClassName)
                .Select(g => new StudentClassStatVm
                {
                    ClassName = g.Key.ClassName,
                    YearCode = g.Key.YearCode,
                    Total = g.Count(),
                    Male = g.Count(s => s.Gender == "M"),
                    Female = g.Count(s => s.Gender == "F"),
                    Active = g.Count(
                        s => s.Status == "Active")
                })
                .ToList();

            return View(model);
        }
    }
}
