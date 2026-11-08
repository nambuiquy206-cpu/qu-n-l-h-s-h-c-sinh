
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ThptA.ViewModels
{
    public class StudentStatisticsVm
    {
        public int? YearId { get; set; }

        public List<SelectListItem> YearOptions { get; set; }
            = new();

        public int Total { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public int Other { get; set; }
        public int Active { get; set; }
        public int Inactive { get; set; }

        public List<StudentGradeStatVm> ByGrade { get; set; }
            = new();

        public List<StudentClassStatVm> ByClass { get; set; }
            = new();
    }

    public class StudentGradeStatVm
    {
        public int Grade { get; set; }
        public int Total { get; set; }
        public int Active { get; set; }
    }

    public class StudentClassStatVm
    {
        public string ClassName { get; set; } = "";
        public string YearCode { get; set; } = "";
        public int Total { get; set; }
        public int Male { get; set; }
        public int Female { get; set; }
        public int Active { get; set; }
    }
}
