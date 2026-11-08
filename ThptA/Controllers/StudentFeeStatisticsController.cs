
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ThptA.Data;
using ThptA.ViewModels;

namespace ThptA.Controllers
{
    [Authorize(Roles = "Admin")]
    public class StudentFeeStatisticsController : Controller
    {
        private readonly AppDbContext _db;

        public StudentFeeStatisticsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            int? academicYearId)
        {
            var years = await _db.AcademicYears
                .AsNoTracking()
                .OrderByDescending(y => y.StartDate)
                .Select(y => new
                {
                    y.Id,
                    y.Code
                })
                .ToListAsync();

            var model = new StudentFeeStatisticsVm
            {
                AcademicYears = years.Select(y =>
                    new SelectListItem
                    {
                        Value = y.Id.ToString(),
                        Text = y.Code
                    }).ToList()
            };

            // Chưa có năm học
            if (years.Count == 0)
                return View(model);

            // Mặc định chọn năm học mới nhất
            int selectedYearId =
                academicYearId ?? years[0].Id;

            var selectedYear = years.FirstOrDefault(
                y => y.Id == selectedYearId);

            if (selectedYear == null)
                return NotFound("Năm học không tồn tại.");

            model.AcademicYearId = selectedYearId;
            model.AcademicYearCode = selectedYear.Code;

            // Lấy các khoản thu thuộc năm học đã chọn
            var fees = await _db.StudentFees
                .AsNoTracking()
                .Where(f =>
                    f.AcademicYearId == selectedYearId)
                .Select(f => new
                {
                    f.StudentId,
                    StudentCode = f.Student.Code,
                    StudentName = f.Student.FullName,
                    ClassName =
                        f.Student.SchoolClass.Name,

                    f.AmountDue,
                    f.DueDate,

                    Paid = f.Payments
                        .Sum(p => (decimal?)p.Amount) ?? 0m
                })
                .ToListAsync();

            var today = DateTime.Today;

            // Thống kê toàn trường trong năm học
            model.TotalFeeCount = fees.Count;

            model.TotalDue =
                fees.Sum(f => f.AmountDue);

            model.TotalPaid =
                fees.Sum(f => f.Paid);

            model.TotalRemaining =
                model.TotalDue - model.TotalPaid;

            model.UnsettledFeeCount = fees.Count(
                f => f.Paid < f.AmountDue);

            model.OverdueFeeCount = fees.Count(
                f => f.Paid < f.AmountDue &&
                     f.DueDate.Date < today);

            // Gộp nợ theo từng học sinh
            model.Debtors = fees
                .GroupBy(f => f.StudentId)
                .Select(g => new StudentDebtRowVm
                {
                    StudentId = g.Key,

                    StudentCode =
                        g.First().StudentCode,

                    StudentName =
                        g.First().StudentName,

                    ClassName =
                        g.First().ClassName,

                    TotalDue =
                        g.Sum(f => f.AmountDue),

                    TotalPaid =
                        g.Sum(f => f.Paid),

                    TotalRemaining =
                        g.Sum(f => f.AmountDue - f.Paid),

                    UnsettledFeeCount =
                        g.Count(f => f.Paid < f.AmountDue),

                    OverdueFeeCount =
                        g.Count(f =>
                            f.Paid < f.AmountDue &&
                            f.DueDate.Date < today)
                })
                .Where(s => s.TotalRemaining > 0)
                .OrderByDescending(
                    s => s.OverdueFeeCount)
                .ThenByDescending(
                    s => s.TotalRemaining)
                .ToList();

            return View(model);
        }
    }
}
