
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ThptA.ViewModels
{
    public class StudentFeeStatisticsVm
    {
        public int? AcademicYearId { get; set; }

        public string AcademicYearCode { get; set; } = "";

        public List<SelectListItem> AcademicYears
        { get; set; } = new();

        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }

        public int TotalFeeCount { get; set; }
        public int UnsettledFeeCount { get; set; }
        public int OverdueFeeCount { get; set; }

        public List<StudentDebtRowVm> Debtors
        { get; set; } = new();
    }

    public class StudentDebtRowVm
    {
        public int StudentId { get; set; }

        public string StudentCode { get; set; } = "";
        public string StudentName { get; set; } = "";
        public string ClassName { get; set; } = "";

        public decimal TotalDue { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalRemaining { get; set; }

        public int UnsettledFeeCount { get; set; }
        public int OverdueFeeCount { get; set; }
    }
}
